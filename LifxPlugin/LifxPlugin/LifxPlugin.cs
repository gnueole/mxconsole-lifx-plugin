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

        public event EventHandler GroupsUpdated;

        private string _selectedRoomId = null;
        public string SelectedRoomId
        {
            get => this._selectedRoomId;
            set
            {
                if (this._selectedRoomId != value)
                {
                    this._selectedRoomId = value;
                    this.SelectionUpdated?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler SelectionUpdated;

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

                    // Fetch groups asynchronously in the background so it doesn't block plugin loading
                    Task.Run(async () =>
                    {
                        try
                        {
                            var groupsList = await this.Client.GetGroupsAsync();
                            this.Groups = groupsList ?? new List<LifxGroup>();
                            PluginLog.Info($"LIFX Plugin: successfully loaded {this.Groups.Count} groups.");
                            
                            // Fire event to notify dynamic actions and adjustments to register parameters
                            this.GroupsUpdated?.Invoke(this, EventArgs.Empty);
                        }
                        catch (Exception ex)
                        {
                            PluginLog.Error(ex, "Exception in background group loader task.");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to initialize LIFX Client in Load()");
            }
        }

        // This method is called when the plugin is unloaded.
        public override void Unload()
        {
        }
    }
}
