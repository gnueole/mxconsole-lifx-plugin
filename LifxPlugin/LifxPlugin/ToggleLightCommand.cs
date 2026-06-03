namespace Loupedeck.LifxPlugin
{
    using System;
    using System.Threading.Tasks;

    public class ToggleLightCommand : PluginDynamicCommand
    {
        public ToggleLightCommand()
            : base()
        {
        }

        protected override bool OnLoad()
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin != null)
            {
                plugin.GroupsUpdated += this.OnGroupsUpdated;

                // Load groups if already populated
                if (plugin.Groups.Count > 0)
                {
                    this.OnGroupsUpdated(this, EventArgs.Empty);
                }
                else
                {
                    // Register default "All Lights" parameter
                    this.AddParameter(string.Empty, "All Lights", "LIFX");
                }
            }
            return true;
        }

        protected override bool OnUnload()
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin != null)
            {
                plugin.GroupsUpdated -= this.OnGroupsUpdated;
            }
            return true;
        }

        private void OnGroupsUpdated(object sender, EventArgs e)
        {
            var plugin = (LifxPlugin)this.Plugin;

            this.RemoveAllParameters();

            // Register global action
            this.AddParameter(string.Empty, "All Lights", "LIFX");

            // Register group actions
            foreach (var group in plugin.Groups)
            {
                this.AddParameter(group.Id, group.Name, "LIFX Rooms");
            }

            this.ParametersChanged();
        }

        protected override void RunCommand(String actionParameter)
        {
            var plugin = (LifxPlugin)this.Plugin;

            Task.Run(async () =>
            {
                if (string.IsNullOrEmpty(actionParameter))
                {
                    await plugin.Client.ToggleLightsAsync();
                }
                else
                {
                    await plugin.Client.ToggleGroupAsync(actionParameter);
                }
            });
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            if (string.IsNullOrEmpty(actionParameter))
            {
                return "Toggle\nAll";
            }

            var plugin = (LifxPlugin)this.Plugin;
            var group = plugin.Groups.Find(g => g.Id == actionParameter);
            return group != null ? $"Toggle\n{group.Name}" : "Toggle Group";
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var text = this.GetCommandDisplayName(actionParameter, imageSize);
            var isGroup = !string.IsNullOrEmpty(actionParameter);
            return PluginImages.CreateBulbButtonImage(imageSize, text, isGroup);
        }
    }
}
