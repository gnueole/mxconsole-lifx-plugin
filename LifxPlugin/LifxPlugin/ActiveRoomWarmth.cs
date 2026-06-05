namespace Loupedeck.LifxPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class ActiveRoomWarmth : PluginDynamicAdjustment
    {
        private int _globalTemperature = 3500;
        private bool _globalInitialized = false;

        private readonly Dictionary<string, int> _groupTemperatures = new Dictionary<string, int>();
        private readonly HashSet<string> _initializedGroups = new HashSet<string>();

        // Coalescers prevent overlapping HTTP calls when dial spins fast
        private RequestCoalescer _globalCoalescer;
        private readonly Dictionary<string, RequestCoalescer> _groupCoalescers = new Dictionary<string, RequestCoalescer>();

        private double _localGlobalBrightness = 0.5;
        private readonly Dictionary<string, double> _localGroupBrightnesses = new Dictionary<string, double>();
        private RequestCoalescer _localGlobalBrightnessCoalescer;
        private readonly Dictionary<string, RequestCoalescer> _localGroupBrightnessCoalescers = new Dictionary<string, RequestCoalescer>();

        public ActiveRoomWarmth()
            : base(displayName: "Warmth", description: "Adjust color temperature of active room", groupName: "LIFX", hasReset: true)
        {
        }

        protected override bool OnLoad()
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin != null)
            {
                plugin.SelectionUpdated += this.OnSelectionUpdated;
            }
            return true;
        }

        protected override bool OnUnload()
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin != null)
            {
                plugin.SelectionUpdated -= this.OnSelectionUpdated;
            }
            return true;
        }

        private void OnSelectionUpdated(object sender, EventArgs e)
        {
            try
            {
                var plugin = (LifxPlugin)this.Plugin;
                if (plugin != null)
                {
                    var roomId = plugin.ActiveSelector;
                    lock (this._initializedGroups)
                    {
                        if (!string.IsNullOrEmpty(roomId))
                        {
                            this._initializedGroups.Remove(roomId);
                        }
                        else
                        {
                            this._globalInitialized = false;
                        }
                    }

                    // Fetch active room/global brightness for local cache
                    Task.Run(async () =>
                    {
                        if (plugin.Client != null)
                        {
                            if (string.IsNullOrEmpty(roomId))
                            {
                                this._localGlobalBrightness = await plugin.Client.GetBrightnessAsync();
                            }
                            else
                            {
                                var brightness = await plugin.Client.GetGroupBrightnessAsync(roomId);
                                lock (this._localGroupBrightnesses)
                                {
                                    this._localGroupBrightnesses[roomId] = brightness;
                                }
                            }
                        }
                    });
                }
                this.AdjustmentValueChanged();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Error in ActiveRoomWarmth.OnSelectionUpdated");
            }
        }

        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin == null)
            {
                return;
            }

            var roomId = plugin.ActiveSelector;

            if (string.IsNullOrEmpty(roomId))
            {
                // Global temperature adjustment (clockwise = colder = higher Kelvin)
                this._globalTemperature += diff * 200;
                this._globalTemperature = Math.Max(1500, Math.Min(9000, this._globalTemperature));
                var targetTemp = this._globalTemperature;

                PluginLog.Info($"[Warmth] Global: diff={diff:+0;-0}, target={targetTemp}K");
                this.AdjustmentValueChanged();

                if (this._globalCoalescer == null)
                {
                    this._globalCoalescer = new RequestCoalescer(async () =>
                    {
                        await plugin.Client.SetTemperatureAsync(this._globalTemperature);
                    });
                }
                this._globalCoalescer.Trigger();
            }
            else
            {
                // Group-specific temperature adjustment (clockwise = warmer = lower Kelvin)
                int currentVal = 3500;
                lock (this._groupTemperatures)
                {
                    if (this._groupTemperatures.TryGetValue(roomId, out int cachedVal))
                    {
                        currentVal = cachedVal;
                    }
                }

                currentVal += diff * 200;
                currentVal = Math.Max(1500, Math.Min(9000, currentVal));

                lock (this._groupTemperatures)
                {
                    this._groupTemperatures[roomId] = currentVal;
                }

                PluginLog.Info($"[Warmth] Group {roomId}: diff={diff:+0;-0}, target={currentVal}K");
                this.AdjustmentValueChanged();

                RequestCoalescer coalescer;
                lock (this._groupCoalescers)
                {
                    if (!this._groupCoalescers.TryGetValue(roomId, out coalescer))
                    {
                        var capturedRoomId = roomId;
                        coalescer = new RequestCoalescer(async () =>
                        {
                            int val;
                            lock (this._groupTemperatures)
                            {
                                this._groupTemperatures.TryGetValue(capturedRoomId, out val);
                            }
                            await plugin.Client.SetGroupTemperatureAsync(capturedRoomId, val);
                        });
                        this._groupCoalescers[roomId] = coalescer;
                    }
                }
                coalescer.Trigger();
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            // Reset temperature to standard warm-white (3500K)
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin == null)
            {
                return;
            }

            var roomId = plugin.ActiveSelector;

            if (string.IsNullOrEmpty(roomId))
            {
                this._globalTemperature = 3500;
                PluginLog.Info("[Warmth] Reset global temperature to 3500K");
                this.AdjustmentValueChanged();

                Task.Run(async () => await plugin.Client.SetTemperatureAsync(3500));
            }
            else
            {
                lock (this._groupTemperatures)
                {
                    this._groupTemperatures[roomId] = 3500;
                }
                PluginLog.Info($"[Warmth] Reset group {roomId} temperature to 3500K");
                this.AdjustmentValueChanged();

                Task.Run(async () => await plugin.Client.SetGroupTemperatureAsync(roomId, 3500));
            }
        }

        protected override String GetAdjustmentValue(String actionParameter)
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin == null)
            {
                return "3500K";
            }

            var roomId = plugin.ActiveSelector;

            if (string.IsNullOrEmpty(roomId))
            {
                if (!this._globalInitialized)
                {
                    this._globalInitialized = true;
                    Task.Run(async () =>
                    {
                        if (plugin?.Client != null)
                        {
                            this._globalTemperature = await plugin.Client.GetTemperatureAsync();
                            this.AdjustmentValueChanged();
                        }
                    });
                }
                return $"{this._globalTemperature}K";
            }
            else
            {
                bool shouldInit = false;
                lock (this._initializedGroups)
                {
                    if (!this._initializedGroups.Contains(roomId))
                    {
                        this._initializedGroups.Add(roomId);
                        shouldInit = true;
                    }
                }

                if (shouldInit)
                {
                    Task.Run(async () =>
                    {
                        if (plugin?.Client != null)
                        {
                            int temp = await plugin.Client.GetGroupTemperatureAsync(roomId);
                            lock (this._groupTemperatures)
                            {
                                this._groupTemperatures[roomId] = temp;
                            }
                            this.AdjustmentValueChanged();
                        }
                    });
                }

                int val = 3500;
                lock (this._groupTemperatures)
                {
                    if (this._groupTemperatures.TryGetValue(roomId, out int cachedVal))
                    {
                        val = cachedVal;
                    }
                }
                return $"{val}K";
            }
        }

        protected override String GetAdjustmentDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            if (imageSize == PluginImageSize.None)
            {
                var plugin = (LifxPlugin)this.Plugin;
                if (plugin != null && !string.IsNullOrEmpty(plugin.ActiveSelector))
                {
                    if (plugin.ActiveSelector.StartsWith("group_id:"))
                    {
                        var groupId = plugin.ActiveSelector.Substring("group_id:".Length);
                        var group = plugin.Groups.Find(g => g.Id == groupId);
                        if (group != null)
                        {
                            return $"{group.Name} Warmth";
                        }
                    }
                    else if (plugin.ActiveSelector.StartsWith("id:"))
                    {
                        var lightId = plugin.ActiveSelector.Substring("id:".Length);
                        var light = plugin.Lights.Find(l => l.Id == lightId);
                        if (light != null)
                        {
                            return $"{light.Name} Warmth";
                        }
                    }
                }
                return "Warmth";
            }
            return "";
        }

        protected override BitmapImage GetAdjustmentImage(String actionParameter, PluginImageSize imageSize)
        {
            return PluginImages.CreateWarmthWheelImage(imageSize);
        }

        protected override Boolean ProcessEncoderEvent(String actionParameter, DeviceEncoderEvent encoderEvent)
        {
            if (encoderEvent.ControlId == 41 || encoderEvent.ControlId == 0)
            {
                // Roller (vertical scroll) turned: adjust active group's brightness
                this.AdjustBrightness(actionParameter, encoderEvent.Clicks);
                return true;
            }
            else
            {
                // Main Dial turned: adjust warmth
                this.ApplyAdjustment(actionParameter, encoderEvent.Clicks);
                return true;
            }
        }

        private void AdjustBrightness(string actionParameter, int diff)
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin == null)
            {
                return;
            }

            var roomId = plugin.ActiveSelector;

            if (string.IsNullOrEmpty(roomId))
            {
                this._localGlobalBrightness += diff * 0.02;
                this._localGlobalBrightness = Math.Max(0.0, Math.Min(1.0, this._localGlobalBrightness));
                
                PluginLog.Info($"[Warmth/Brightness] Global Scroll: diff={diff:+0;-0}, target={this._localGlobalBrightness * 100:0}%");

                if (this._localGlobalBrightnessCoalescer == null)
                {
                    this._localGlobalBrightnessCoalescer = new RequestCoalescer(async () =>
                    {
                        await plugin.Client.SetBrightnessAsync(this._localGlobalBrightness);
                    });
                }
                this._localGlobalBrightnessCoalescer.Trigger();
            }
            else
            {
                double currentVal = 0.5;
                lock (this._localGroupBrightnesses)
                {
                    if (!this._localGroupBrightnesses.TryGetValue(roomId, out currentVal))
                    {
                        currentVal = 0.5;
                    }
                }

                currentVal += diff * 0.02;
                currentVal = Math.Max(0.0, Math.Min(1.0, currentVal));

                lock (this._localGroupBrightnesses)
                {
                    this._localGroupBrightnesses[roomId] = currentVal;
                }

                PluginLog.Info($"[Warmth/Brightness] Group {roomId} Scroll: diff={diff:+0;-0}, target={currentVal * 100:0}%");

                RequestCoalescer coalescer;
                lock (this._localGroupBrightnessCoalescers)
                {
                    if (!this._localGroupBrightnessCoalescers.TryGetValue(roomId, out coalescer))
                    {
                        var capturedRoomId = roomId;
                        coalescer = new RequestCoalescer(async () =>
                        {
                            double val;
                            lock (this._localGroupBrightnesses)
                            {
                                this._localGroupBrightnesses.TryGetValue(capturedRoomId, out val);
                            }
                            await plugin.Client.SetGroupBrightnessAsync(capturedRoomId, val);
                        });
                        this._localGroupBrightnessCoalescers[roomId] = coalescer;
                    }
                }
                coalescer.Trigger();
            }
        }
    }
}
