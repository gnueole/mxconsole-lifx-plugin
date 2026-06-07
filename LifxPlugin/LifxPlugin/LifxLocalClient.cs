namespace Loupedeck.LifxPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    public class LifxLocalClient : IDisposable
    {
        private const int LifxPort = 56700;
        private readonly UdpClient _udpClient;
        private readonly Dictionary<string, string> _deviceIps = new Dictionary<string, string>(); // MAC (12 chars hex) -> IPAddress (string)
        private readonly Dictionary<byte, TaskCompletionSource<bool>> _pendingAcks = new Dictionary<byte, TaskCompletionSource<bool>>();
        private readonly object _lock = new object();
        private byte _sequence = 0;
        private bool _isDisposed = false;

        public LifxLocalClient()
        {
            try
            {
                // Create UDP client configured to reuse port and enable broadcast
                this._udpClient = new UdpClient();
                this._udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                this._udpClient.EnableBroadcast = true;

                // Bind to LIFX port 56700 so we receive broadcasts and responses correctly
                this._udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, LifxPort));

                // Start background listening task for UDP replies
                Task.Run(this.ListenForRepliesAsync);

                // Start background discovery loop
                Task.Run(this.DiscoveryLoopAsync);

                PluginLog.Info("LIFX Local Client started. Discovery and reply loops active.");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to initialize LIFX local UDP client.");
            }
        }

        private byte GetNextSequence()
        {
            lock (this._lock)
            {
                this._sequence++;
                // 0 is usually reserved or standard, so we cycle 1-255
                if (this._sequence == 0)
                {
                    this._sequence = 1;
                }
                return this._sequence;
            }
        }

        public async Task DiscoverAsync()
        {
            if (this._isDisposed || this._udpClient == null)
            {
                return;
            }

            try
            {
                var seq = this.GetNextSequence();
                // GetService packet (Type 2)
                var packet = BuildPacket(2, seq, null, 0, null);
                var broadcastEP = new IPEndPoint(IPAddress.Broadcast, LifxPort);

                await this._udpClient.SendAsync(packet, packet.Length, broadcastEP);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to broadcast discovery GetService packet");
            }
        }

        private async Task DiscoveryLoopAsync()
        {
            while (!this._isDisposed)
            {
                await this.DiscoverAsync();
                // Run discovery every 30 seconds
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }

        private async Task ListenForRepliesAsync()
        {
            while (!this._isDisposed && this._udpClient != null)
            {
                try
                {
                    var receiveResult = await this._udpClient.ReceiveAsync();
                    byte[] buffer = receiveResult.Buffer;

                    if (buffer.Length >= 36)
                    {
                        // Target MAC address is located at bytes 8-13 (6 bytes)
                        string mac = BitConverter.ToString(buffer, 8, 6).Replace("-", "").ToLower();
                        string ip = receiveResult.RemoteEndPoint.Address.ToString();

                        if (mac != "000000000000")
                        {
                            lock (this._lock)
                            {
                                if (!this._deviceIps.ContainsKey(mac) || this._deviceIps[mac] != ip)
                                {
                                    this._deviceIps[mac] = ip;
                                    PluginLog.Info($"LIFX Local Client: Discovered device {mac} at IP {ip}");
                                }
                            }
                        }

                        ushort type = (ushort)(buffer[32] | (buffer[33] << 8));
                        byte seq = buffer[22];

                        if (type == 45) // Acknowledgement packet
                        {
                            lock (this._lock)
                            {
                                if (this._pendingAcks.TryGetValue(seq, out var tcs))
                                {
                                    tcs.TrySetResult(true);
                                    this._pendingAcks.Remove(seq);
                                }
                            }
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!this._isDisposed)
                    {
                        PluginLog.Error(ex, "Exception in LIFX local UDP listener loop.");
                        await Task.Delay(1000); // Backoff briefly on unexpected errors
                    }
                }
            }
        }

        public async Task<bool> SetPowerAsync(string macAddress, bool on, int durationMs = 250)
        {
            string ip;
            lock (this._lock)
            {
                if (!this._deviceIps.TryGetValue(macAddress, out ip))
                {
                    return false; // Bulbs IP address is not discovered locally yet
                }
            }

            var targetMac = ParseMacAddress(macAddress);
            var seq = this.GetNextSequence();

            // SetLightPower payload (Type 117): Level (uint16) + Duration (uint32)
            byte[] payload = new byte[6];
            ushort level = on ? (ushort)65535 : (ushort)0;
            payload[0] = (byte)(level & 0xFF);
            payload[1] = (byte)((level >> 8) & 0xFF);

            payload[2] = (byte)(durationMs & 0xFF);
            payload[3] = (byte)((durationMs >> 8) & 0xFF);
            payload[4] = (byte)((durationMs >> 16) & 0xFF);
            payload[5] = (byte)((durationMs >> 24) & 0xFF);

            // Flag 2 is AckRequired
            byte[] packet = BuildPacket(117, seq, targetMac, 2, payload);
            return await this.SendCommandWithAckAsync(seq, ip, packet);
        }

        public async Task<bool> SetColorAsync(string macAddress, double hue, double saturation, double brightness, int kelvin, int durationMs = 250)
        {
            string ip;
            lock (this._lock)
            {
                if (!this._deviceIps.TryGetValue(macAddress, out ip))
                {
                    return false;
                }
            }

            var targetMac = ParseMacAddress(macAddress);
            var seq = this.GetNextSequence();

            // SetColor payload (Type 102): Reserved (1 byte) + HSBK (8 bytes) + Duration (4 bytes)
            byte[] payload = new byte[13];
            payload[0] = 0; // Reserved

            // Hue: 0-360 mapped to 0-65535
            ushort h = (ushort)Math.Round((hue % 360.0) / 360.0 * 65535.0);
            payload[1] = (byte)(h & 0xFF);
            payload[2] = (byte)((h >> 8) & 0xFF);

            // Saturation: 0-1 mapped to 0-65535
            ushort s = (ushort)Math.Round(saturation * 65535.0);
            payload[3] = (byte)(s & 0xFF);
            payload[4] = (byte)((s >> 8) & 0xFF);

            // Brightness: 0-1 mapped to 0-65535
            ushort b = (ushort)Math.Round(brightness * 65535.0);
            payload[5] = (byte)(b & 0xFF);
            payload[6] = (byte)((b >> 8) & 0xFF);

            // Kelvin: 2500-9000
            ushort k = (ushort)kelvin;
            payload[7] = (byte)(k & 0xFF);
            payload[8] = (byte)((k >> 8) & 0xFF);

            // Duration (uint32)
            payload[9] = (byte)(durationMs & 0xFF);
            payload[10] = (byte)((durationMs >> 8) & 0xFF);
            payload[11] = (byte)((durationMs >> 16) & 0xFF);
            payload[12] = (byte)((durationMs >> 24) & 0xFF);

            // Flag 2 is AckRequired
            byte[] packet = BuildPacket(102, seq, targetMac, 2, payload);
            return await this.SendCommandWithAckAsync(seq, ip, packet);
        }

        private async Task<bool> SendCommandWithAckAsync(byte seq, string ipAddress, byte[] packet, int timeoutMs = 250)
        {
            var tcs = new TaskCompletionSource<bool>();
            lock (this._lock)
            {
                this._pendingAcks[seq] = tcs;
            }

            try
            {
                var destinationEP = new IPEndPoint(IPAddress.Parse(ipAddress), LifxPort);
                await this._udpClient.SendAsync(packet, packet.Length, destinationEP);

                // Wait for either the ack callback or a timeout
                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
                if (completedTask == tcs.Task)
                {
                    return await tcs.Task;
                }
                else
                {
                    // Timeout occurred, remove the pending promise
                    lock (this._lock)
                    {
                        this._pendingAcks.Remove(seq);
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Local UDP Send error to IP: {ipAddress}");
                lock (this._lock)
                {
                    this._pendingAcks.Remove(seq);
                }
                return false;
            }
        }

        private static byte[] BuildPacket(ushort type, byte sequence, byte[] targetMac, byte flags, byte[] payload)
        {
            int payloadLength = payload?.Length ?? 0;
            byte[] packet = new byte[36 + payloadLength];

            // Size (uint16)
            ushort size = (ushort)(36 + payloadLength);
            packet[0] = (byte)(size & 0xFF);
            packet[1] = (byte)((size >> 8) & 0xFF);

            // Protocol flags (uint16)
            // Addressable (bit 12) = 1, Protocol = 1024 (0x0400).
            // Tagged (bit 13) is 1 for broadcast, 0 for unicast.
            // If targetMac is null -> Tagged = 1 -> 0x3400 (broadcast)
            // If targetMac is not null -> Tagged = 0 -> 0x1400 (unicast)
            ushort protocol = (ushort)(targetMac == null ? 0x3400 : 0x1400);
            packet[2] = (byte)(protocol & 0xFF);
            packet[3] = (byte)((protocol >> 8) & 0xFF);

            // Source (uint32)
            uint source = 0x54474f4c; // "LOGT" (Logitech)
            packet[4] = (byte)(source & 0xFF);
            packet[5] = (byte)((source >> 8) & 0xFF);
            packet[6] = (byte)((source >> 16) & 0xFF);
            packet[7] = (byte)((source >> 24) & 0xFF);

            // Target MAC (8 bytes)
            if (targetMac != null)
            {
                Array.Copy(targetMac, 0, packet, 8, Math.Min(targetMac.Length, 8));
            }

            // Reserved (6 bytes) at bytes 16-21 default to 0s

            // Ack/Res flags (1 byte)
            packet[22] = flags;

            // Sequence (1 byte)
            packet[23] = sequence;

            // Reserved (8 bytes) at bytes 24-31 default to 0s

            // Type (uint16)
            packet[32] = (byte)(type & 0xFF);
            packet[33] = (byte)((type >> 8) & 0xFF);

            // Reserved (2 bytes) at bytes 34-35 default to 0s

            // Payload
            if (payload != null)
            {
                Array.Copy(payload, 0, packet, 36, payloadLength);
            }

            return packet;
        }

        private static byte[] ParseMacAddress(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex.Length != 12)
            {
                return new byte[8];
            }
            byte[] mac = new byte[8];
            for (int i = 0; i < 6; i++)
            {
                mac[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return mac;
        }

        public void Dispose()
        {
            this._isDisposed = true;
            try
            {
                this._udpClient?.Dispose();
            }
            catch
            {
                // Ignore socket dispose issues
            }

            lock (this._lock)
            {
                foreach (var pending in this._pendingAcks.Values)
                {
                    pending.TrySetResult(false);
                }
                this._pendingAcks.Clear();
            }
        }
    }
}
