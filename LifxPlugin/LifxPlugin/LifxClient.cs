namespace Loupedeck.LifxPlugin
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class LifxGroup
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class LifxClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _token;

        public LifxClient()
        {
            try
            {
                // First try Documents/LIFX_Token.txt (user-friendly location)
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var documentsTokenPath = Path.Combine(documentsPath, "LIFX_Token.txt");

                if (File.Exists(documentsTokenPath))
                {
                    this._token = File.ReadAllText(documentsTokenPath).Trim();
                }
                else
                {
                    // Fallback to UserProfile/.lifx_token (developer/power-user location)
                    var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    var userProfileTokenPath = Path.Combine(userProfilePath, ".lifx_token");

                    if (File.Exists(userProfileTokenPath))
                    {
                        this._token = File.ReadAllText(userProfileTokenPath).Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to load LIFX token from Documents/LIFX_Token.txt or ~/.lifx_token");
            }

            this._httpClient = new HttpClient();
            if (!string.IsNullOrEmpty(this._token))
            {
                this._httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this._token);
            }
        }

        public bool HasToken => !string.IsNullOrEmpty(this._token);

        public async Task<bool> ToggleLightsAsync()
        {
            if (!this.HasToken)
            {
                PluginLog.Warning("Cannot toggle lights: LIFX token is not configured.");
                return false;
            }

            try
            {
                var response = await this._httpClient.PostAsync("https://api.lifx.com/v1/lights/all/toggle", null);
                if (response.IsSuccessStatusCode)
                {
                    PluginLog.Info("Successfully toggled LIFX lights.");
                    return true;
                }

                PluginLog.Warning($"Failed to toggle lights. API returned status code: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "HTTP request to toggle LIFX lights failed.");
                return false;
            }
        }

        public async Task<double> GetBrightnessAsync()
        {
            if (!this.HasToken)
            {
                return 0.5;
            }

            try
            {
                var responseJson = await this._httpClient.GetStringAsync("https://api.lifx.com/v1/lights/all");
                using var document = JsonDocument.Parse(responseJson);

                if (document.RootElement.ValueKind == JsonValueKind.Array && document.RootElement.GetArrayLength() > 0)
                {
                    foreach (var element in document.RootElement.EnumerateArray())
                    {
                        if (element.TryGetProperty("connected", out var connectedProp) && connectedProp.GetBoolean())
                        {
                            if (element.TryGetProperty("brightness", out var brightnessProp))
                            {
                                return brightnessProp.GetDouble();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to retrieve current LIFX brightness.");
            }

            return 0.5;
        }

        public async Task<bool> SetBrightnessAsync(double brightness)
        {
            if (!this.HasToken)
            {
                PluginLog.Warning("Cannot set brightness: LIFX token is not configured.");
                return false;
            }

            try
            {
                brightness = Math.Max(0.0, Math.Min(1.0, brightness));

                var payload = new { brightness = brightness, power = brightness > 0.001 ? "on" : "off" };
                var payloadString = JsonSerializer.Serialize(payload);
                var content = new StringContent(payloadString, System.Text.Encoding.UTF8, "application/json");

                var startTime = DateTime.UtcNow;
                PluginLog.Info($"LIFX Client: Sending SetBrightnessAsync({brightness * 100:0}%) to all lights...");
                var response = await this._httpClient.PutAsync("https://api.lifx.com/v1/lights/all/state", content);
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

                if (response.IsSuccessStatusCode)
                {
                    PluginLog.Info($"LIFX Client: SetBrightnessAsync succeeded in {elapsed:0}ms.");
                    return true;
                }

                var contentString = await response.Content.ReadAsStringAsync();
                PluginLog.Warning($"LIFX Client: SetBrightnessAsync failed in {elapsed:0}ms. Status: {response.StatusCode} ({(int)response.StatusCode}). Response: {contentString}");
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    PluginLog.Warning("LIFX Client: Rate limit reached! Please slow down adjustments.");
                }
                return false;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "HTTP request to set LIFX brightness failed.");
                return false;
            }
        }

        public async Task<List<LifxGroup>> GetGroupsAsync()
        {
            var groups = new List<LifxGroup>();
            if (!this.HasToken)
            {
                return groups;
            }

            try
            {
                var responseJson = await this._httpClient.GetStringAsync("https://api.lifx.com/v1/lights/all");
                using var document = JsonDocument.Parse(responseJson);

                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var seenIds = new HashSet<string>();
                    foreach (var element in document.RootElement.EnumerateArray())
                    {
                        if (element.TryGetProperty("group", out var groupProp) && groupProp.ValueKind == JsonValueKind.Object)
                        {
                            if (groupProp.TryGetProperty("id", out var idProp) && groupProp.TryGetProperty("name", out var nameProp))
                            {
                                var id = idProp.GetString();
                                var name = nameProp.GetString();
                                if (!string.IsNullOrEmpty(id) && !seenIds.Contains(id))
                                {
                                    seenIds.Add(id);
                                    groups.Add(new LifxGroup { Id = id, Name = name });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to retrieve LIFX groups.");
            }

            return groups;
        }

        public async Task<bool> ToggleGroupAsync(string groupId)
        {
            if (!this.HasToken)
            {
                return false;
            }

            try
            {
                var response = await this._httpClient.PostAsync($"https://api.lifx.com/v1/lights/group_id:{groupId}/toggle", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed to toggle LIFX group {groupId}.");
                return false;
            }
        }

        public async Task<double> GetGroupBrightnessAsync(string groupId)
        {
            if (!this.HasToken)
            {
                return 0.5;
            }

            try
            {
                var responseJson = await this._httpClient.GetStringAsync("https://api.lifx.com/v1/lights/all");
                using var document = JsonDocument.Parse(responseJson);

                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    double sum = 0;
                    int count = 0;
                    foreach (var element in document.RootElement.EnumerateArray())
                    {
                        if (element.TryGetProperty("group", out var groupProp) && groupProp.ValueKind == JsonValueKind.Object)
                        {
                            if (groupProp.TryGetProperty("id", out var idProp) && idProp.GetString() == groupId)
                            {
                                if (element.TryGetProperty("connected", out var connectedProp) && connectedProp.GetBoolean())
                                {
                                    if (element.TryGetProperty("brightness", out var brightnessProp))
                                    {
                                        sum += brightnessProp.GetDouble();
                                        count++;
                                    }
                                }
                            }
                        }
                    }
                    if (count > 0)
                    {
                        return sum / count;
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed to retrieve brightness for group {groupId}.");
            }

            return 0.5;
        }

        public async Task<bool> SetGroupBrightnessAsync(string groupId, double brightness)
        {
            if (!this.HasToken)
            {
                return false;
            }

            try
            {
                brightness = Math.Max(0.0, Math.Min(1.0, brightness));
                var payload = new { brightness = brightness, power = brightness > 0.001 ? "on" : "off" };
                var payloadString = JsonSerializer.Serialize(payload);
                var content = new StringContent(payloadString, System.Text.Encoding.UTF8, "application/json");

                var startTime = DateTime.UtcNow;
                PluginLog.Info($"LIFX Client: Sending SetGroupBrightnessAsync({brightness * 100:0}%) for group {groupId}...");
                var response = await this._httpClient.PutAsync($"https://api.lifx.com/v1/lights/group_id:{groupId}/state", content);
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

                if (response.IsSuccessStatusCode)
                {
                    PluginLog.Info($"LIFX Client: SetGroupBrightnessAsync succeeded in {elapsed:0}ms.");
                    return true;
                }

                var contentString = await response.Content.ReadAsStringAsync();
                PluginLog.Warning($"LIFX Client: SetGroupBrightnessAsync failed in {elapsed:0}ms. Status: {response.StatusCode} ({(int)response.StatusCode}). Response: {contentString}");
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    PluginLog.Warning("LIFX Client: Rate limit reached! Please slow down adjustments.");
                }
                return false;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed to set brightness for group {groupId}.");
                return false;
            }
        }

        public async Task<bool> SetColorToWhiteAsync(string groupId = null)
        {
            if (!this.HasToken)
            {
                PluginLog.Warning("Cannot set color to white: LIFX token is not configured.");
                return false;
            }

            try
            {
                var selector = string.IsNullOrEmpty(groupId) ? "all" : $"group_id:{groupId}";
                var payload = new { color = "white" };
                var payloadString = JsonSerializer.Serialize(payload);
                var content = new StringContent(payloadString, System.Text.Encoding.UTF8, "application/json");

                var response = await this._httpClient.PutAsync($"https://api.lifx.com/v1/lights/{selector}/state", content);
                if (response.IsSuccessStatusCode)
                {
                    PluginLog.Info($"Successfully reset color to standard white for selector: {selector}.");
                    return true;
                }

                PluginLog.Warning($"Failed to reset color. API returned: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed to reset color for group/selector {groupId ?? "all"}.");
                return false;
            }
        }

        public async Task<double> GetHueAsync()
        {
            if (!this.HasToken)
            {
                return 0.0;
            }

            try
            {
                var responseJson = await this._httpClient.GetStringAsync("https://api.lifx.com/v1/lights/all");
                using var document = JsonDocument.Parse(responseJson);

                if (document.RootElement.ValueKind == JsonValueKind.Array && document.RootElement.GetArrayLength() > 0)
                {
                    foreach (var element in document.RootElement.EnumerateArray())
                    {
                        if (element.TryGetProperty("connected", out var connectedProp) && connectedProp.GetBoolean())
                        {
                            if (element.TryGetProperty("color", out var colorProp) && colorProp.ValueKind == JsonValueKind.Object)
                            {
                                if (colorProp.TryGetProperty("hue", out var hueProp))
                                {
                                    return hueProp.GetDouble();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to retrieve current LIFX hue.");
            }

            return 0.0;
        }

        public async Task<bool> SetHueAsync(double hue)
        {
            if (!this.HasToken)
            {
                PluginLog.Warning("Cannot set hue: LIFX token is not configured.");
                return false;
            }

            try
            {
                hue = Math.Max(0.0, Math.Min(360.0, hue));

                var payload = new { color = $"hue:{hue:0.0} saturation:1.0", power = "on" };
                var payloadString = JsonSerializer.Serialize(payload);
                var content = new StringContent(payloadString, System.Text.Encoding.UTF8, "application/json");

                var startTime = DateTime.UtcNow;
                PluginLog.Info($"LIFX Client: Sending SetHueAsync({hue:0.0}) to all lights...");
                var response = await this._httpClient.PutAsync("https://api.lifx.com/v1/lights/all/state", content);
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

                if (response.IsSuccessStatusCode)
                {
                    PluginLog.Info($"LIFX Client: SetHueAsync succeeded in {elapsed:0}ms.");
                    return true;
                }

                var contentString = await response.Content.ReadAsStringAsync();
                PluginLog.Warning($"LIFX Client: SetHueAsync failed in {elapsed:0}ms. Status: {response.StatusCode} ({(int)response.StatusCode}). Response: {contentString}");
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    PluginLog.Warning("LIFX Client: Rate limit reached! Please slow down adjustments.");
                }
                return false;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "HTTP request to set LIFX hue failed.");
                return false;
            }
        }

        public async Task<double> GetGroupHueAsync(string groupId)
        {
            if (!this.HasToken)
            {
                return 0.0;
            }

            try
            {
                var responseJson = await this._httpClient.GetStringAsync("https://api.lifx.com/v1/lights/all");
                using var document = JsonDocument.Parse(responseJson);

                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    double sum = 0;
                    int count = 0;
                    foreach (var element in document.RootElement.EnumerateArray())
                    {
                        if (element.TryGetProperty("group", out var groupProp) && groupProp.ValueKind == JsonValueKind.Object)
                        {
                            if (groupProp.TryGetProperty("id", out var idProp) && idProp.GetString() == groupId)
                            {
                                if (element.TryGetProperty("connected", out var connectedProp) && connectedProp.GetBoolean())
                                {
                                    if (element.TryGetProperty("color", out var colorProp) && colorProp.ValueKind == JsonValueKind.Object)
                                    {
                                        if (colorProp.TryGetProperty("hue", out var hueProp))
                                        {
                                            sum += hueProp.GetDouble();
                                            count++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (count > 0)
                    {
                        return sum / count;
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed to retrieve hue for group {groupId}.");
            }

            return 0.0;
        }

        public async Task<bool> SetGroupHueAsync(string groupId, double hue)
        {
            if (!this.HasToken)
            {
                return false;
            }

            try
            {
                hue = Math.Max(0.0, Math.Min(360.0, hue));
                var payload = new { color = $"hue:{hue:0.0} saturation:1.0", power = "on" };
                var payloadString = JsonSerializer.Serialize(payload);
                var content = new StringContent(payloadString, System.Text.Encoding.UTF8, "application/json");

                var startTime = DateTime.UtcNow;
                PluginLog.Info($"LIFX Client: Sending SetGroupHueAsync({hue:0.0}) for group {groupId}...");
                var response = await this._httpClient.PutAsync($"https://api.lifx.com/v1/lights/group_id:{groupId}/state", content);
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

                if (response.IsSuccessStatusCode)
                {
                    PluginLog.Info($"LIFX Client: SetGroupHueAsync succeeded in {elapsed:0}ms.");
                    return true;
                }

                var contentString = await response.Content.ReadAsStringAsync();
                PluginLog.Warning($"LIFX Client: SetGroupHueAsync failed in {elapsed:0}ms. Status: {response.StatusCode} ({(int)response.StatusCode}). Response: {contentString}");
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    PluginLog.Warning("LIFX Client: Rate limit reached! Please slow down adjustments.");
                }
                return false;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed to set hue for group {groupId}.");
                return false;
            }
        }

        public async Task<int> GetTemperatureAsync()
        {
            if (!this.HasToken)
            {
                return 3500;
            }

            try
            {
                var responseJson = await this._httpClient.GetStringAsync("https://api.lifx.com/v1/lights/all");
                using var document = JsonDocument.Parse(responseJson);

                if (document.RootElement.ValueKind == JsonValueKind.Array && document.RootElement.GetArrayLength() > 0)
                {
                    foreach (var element in document.RootElement.EnumerateArray())
                    {
                        if (element.TryGetProperty("connected", out var connectedProp) && connectedProp.GetBoolean())
                        {
                            if (element.TryGetProperty("color", out var colorProp) && colorProp.ValueKind == JsonValueKind.Object)
                            {
                                if (colorProp.TryGetProperty("kelvin", out var kelvinProp))
                                {
                                    return kelvinProp.GetInt32();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to retrieve current LIFX temperature.");
            }

            return 3500;
        }

        public async Task<bool> SetTemperatureAsync(int kelvin)
        {
            if (!this.HasToken)
            {
                PluginLog.Warning("Cannot set temperature: LIFX token is not configured.");
                return false;
            }

            try
            {
                kelvin = Math.Max(1500, Math.Min(9000, kelvin));

                var payload = new { color = $"kelvin:{kelvin}", power = "on" };
                var payloadString = JsonSerializer.Serialize(payload);
                var content = new StringContent(payloadString, System.Text.Encoding.UTF8, "application/json");

                var startTime = DateTime.UtcNow;
                PluginLog.Info($"LIFX Client: Sending SetTemperatureAsync({kelvin}K) to all lights...");
                var response = await this._httpClient.PutAsync("https://api.lifx.com/v1/lights/all/state", content);
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

                if (response.IsSuccessStatusCode)
                {
                    PluginLog.Info($"LIFX Client: SetTemperatureAsync succeeded in {elapsed:0}ms.");
                    return true;
                }

                var contentString = await response.Content.ReadAsStringAsync();
                PluginLog.Warning($"LIFX Client: SetTemperatureAsync failed in {elapsed:0}ms. Status: {response.StatusCode}. Response: {contentString}");
                return false;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "HTTP request to set LIFX temperature failed.");
                return false;
            }
        }

        public async Task<int> GetGroupTemperatureAsync(string groupId)
        {
            if (!this.HasToken)
            {
                return 3500;
            }

            try
            {
                var responseJson = await this._httpClient.GetStringAsync("https://api.lifx.com/v1/lights/all");
                using var document = JsonDocument.Parse(responseJson);

                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    double sum = 0;
                    int count = 0;
                    foreach (var element in document.RootElement.EnumerateArray())
                    {
                        if (element.TryGetProperty("group", out var groupProp) && groupProp.ValueKind == JsonValueKind.Object)
                        {
                            if (groupProp.TryGetProperty("id", out var idProp) && idProp.GetString() == groupId)
                            {
                                if (element.TryGetProperty("connected", out var connectedProp) && connectedProp.GetBoolean())
                                {
                                    if (element.TryGetProperty("color", out var colorProp) && colorProp.ValueKind == JsonValueKind.Object)
                                    {
                                        if (colorProp.TryGetProperty("kelvin", out var kelvinProp))
                                        {
                                            sum += kelvinProp.GetInt32();
                                            count++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (count > 0)
                    {
                        return (int)Math.Round(sum / count);
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed to retrieve temperature for group {groupId}.");
            }

            return 3500;
        }

        public async Task<bool> SetGroupTemperatureAsync(string groupId, int kelvin)
        {
            if (!this.HasToken)
            {
                return false;
            }

            try
            {
                kelvin = Math.Max(1500, Math.Min(9000, kelvin));
                var payload = new { color = $"kelvin:{kelvin}", power = "on" };
                var payloadString = JsonSerializer.Serialize(payload);
                var content = new StringContent(payloadString, System.Text.Encoding.UTF8, "application/json");

                var startTime = DateTime.UtcNow;
                PluginLog.Info($"LIFX Client: Sending SetGroupTemperatureAsync({kelvin}K) for group {groupId}...");
                var response = await this._httpClient.PutAsync($"https://api.lifx.com/v1/lights/group_id:{groupId}/state", content);
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

                if (response.IsSuccessStatusCode)
                {
                    PluginLog.Info($"LIFX Client: SetGroupTemperatureAsync succeeded in {elapsed:0}ms.");
                    return true;
                }

                var contentString = await response.Content.ReadAsStringAsync();
                PluginLog.Warning($"LIFX Client: SetGroupTemperatureAsync failed in {elapsed:0}ms. Status: {response.StatusCode}. Response: {contentString}");
                return false;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed to set temperature for group {groupId}.");
                return false;
            }
        }
    }

    public class RequestCoalescer
    {
        private readonly Func<Task> _action;
        private readonly int _delayMs;
        private System.Threading.CancellationTokenSource _cts;
        private readonly object _lock = new object();
        private bool _isRunning;
        private bool _hasPending;

        public RequestCoalescer(Func<Task> action, int delayMs = 350)
        {
            this._action = action;
            this._delayMs = delayMs;
        }

        public void Trigger()
        {
            lock (this._lock)
            {
                if (this._cts != null)
                {
                    this._cts.Cancel();
                    this._cts.Dispose();
                }

                this._cts = new System.Threading.CancellationTokenSource();
                var token = this._cts.Token;

                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(this._delayMs, token);
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }

                    this.ExecuteAction();
                });
            }
        }

        private void ExecuteAction()
        {
            lock (this._lock)
            {
                if (this._isRunning)
                {
                    this._hasPending = true;
                    return;
                }
                this._isRunning = true;
            }

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await this._action();
                    }
                    catch (Exception ex)
                    {
                        PluginLog.Error(ex, "Error in throttled request execution.");
                    }

                    lock (this._lock)
                    {
                        if (!this._hasPending)
                        {
                            this._isRunning = false;
                            break;
                        }
                        this._hasPending = false;
                    }
                }
            });
        }
    }
}

