namespace Loupedeck.LifxPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class LifxPlugin : Plugin
    {
        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is a Universal plugin or an Application plugin.
        public override Boolean HasNoApplication => true;

        public LifxClient Client { get; private set; }

        public List<LifxGroup> Groups { get; private set; } = new List<LifxGroup>();
        public List<LifxScene> Scenes { get; private set; } = new List<LifxScene>();
        public List<LifxLight> Lights { get; private set; } = new List<LifxLight>();

        public event EventHandler GroupsUpdated;
        public event EventHandler ScenesUpdated;
        public event EventHandler LightsUpdated;

        private string _selectedRoomId = null;
        public string SelectedRoomId
        {
            get => this._selectedRoomId;
            set
            {
                if (this._selectedRoomId != value)
                {
                    this._selectedRoomId = value;
                    if (value != null)
                    {
                        this._selectedLightId = null; // Mutual exclusion
                    }
                    this.SelectionUpdated?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private string _selectedLightId = null;
        public string SelectedLightId
        {
            get => this._selectedLightId;
            set
            {
                if (this._selectedLightId != value)
                {
                    this._selectedLightId = value;
                    if (value != null)
                    {
                        this._selectedRoomId = null; // Mutual exclusion
                    }
                    this.SelectionUpdated?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string ActiveSelector
        {
            get
            {
                if (!string.IsNullOrEmpty(this.SelectedLightId))
                {
                    return $"id:{this.SelectedLightId}";
                }
                if (!string.IsNullOrEmpty(this.SelectedRoomId))
                {
                    return $"group_id:{this.SelectedRoomId}";
                }
                return null;
            }
        }

        public event EventHandler SelectionUpdated;

        private double _effectPeriod = 2.0;
        public double EffectPeriod
        {
            get => this._effectPeriod;
            set => this._effectPeriod = Math.Max(0.5, Math.Min(10.0, value));
        }

        public string LastEffectParameter { get; set; } = null;

        // Initializes a new instance of the plugin class.
        public LifxPlugin()
        {
            // Initialize the plugin log.
            PluginLog.Init(this.Log);

            // Initialize the plugin resources.
            PluginResources.Init(this.Assembly);
        }

        // This method is called when the plugin is loaded.
        public override void Load()
        {
            try
            {
                this.Client = new LifxClient();
                if (!this.Client.HasToken)
                {
                    PluginLog.Warning("LIFX API Token was not found. Please create a text file named 'LIFX_Token.txt' in your Documents folder with your token.");
                }
                else
                {
                    PluginLog.Info("LIFX Plugin initializing...");

                    // Start background update loop
                    Task.Run(async () =>
                    {
                        var isFirstRun = true;
                        while (true)
                        {
                            try
                            {
                                // Fetch groups
                                var groupsList = await this.Client.GetGroupsAsync();
                                this.Groups = groupsList ?? new List<LifxGroup>();

                                // Fetch scenes
                                var scenesList = await this.Client.GetScenesAsync();
                                this.Scenes = scenesList ?? new List<LifxScene>();

                                // Fetch lights
                                var lightsList = await this.Client.GetLightsAsync();
                                this.Lights = lightsList ?? new List<LifxLight>();

                                if (isFirstRun)
                                {
                                    PluginLog.Info($"LIFX Plugin: Initial load completed. Groups: {this.Groups.Count}, Scenes: {this.Scenes.Count}, Lights: {this.Lights.Count}");
                                    isFirstRun = false;
                                }

                                // Notify UI
                                this.GroupsUpdated?.Invoke(this, EventArgs.Empty);
                                this.ScenesUpdated?.Invoke(this, EventArgs.Empty);
                                this.LightsUpdated?.Invoke(this, EventArgs.Empty);
                            }
                            catch (Exception ex)
                            {
                                PluginLog.Error(ex, "Exception in background update loop.");
                            }

                            // Wait 2 minutes before next update
                            await Task.Delay(TimeSpan.FromMinutes(2));
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to initialize LIFX Client in Load()");
            }
        }

        public void TriggerManualRefresh()
        {
            Task.Run(async () =>
            {
                try
                {
                    // Fetch groups
                    var groupsList = await this.Client.GetGroupsAsync();
                    this.Groups = groupsList ?? new List<LifxGroup>();

                    // Fetch scenes
                    var scenesList = await this.Client.GetScenesAsync();
                    this.Scenes = scenesList ?? new List<LifxScene>();

                    // Fetch lights
                    var lightsList = await this.Client.GetLightsAsync();
                    this.Lights = lightsList ?? new List<LifxLight>();

                    // Notify UI
                    this.GroupsUpdated?.Invoke(this, EventArgs.Empty);
                    this.ScenesUpdated?.Invoke(this, EventArgs.Empty);
                    this.LightsUpdated?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, "Exception in manual refresh.");
                }
            });
        }

        // This method is called when the plugin is unloaded.
        public override void Unload()
        {
        }
    }
}
