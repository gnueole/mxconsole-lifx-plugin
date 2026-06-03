namespace Loupedeck.LifxPlugin
{
    using System;
    using System.Threading.Tasks;

    public class ActiveRoomPowerToggle : PluginDynamicCommand
    {
        public ActiveRoomPowerToggle()
            : base()
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
                this.ActionImageChanged();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Error in ActiveRoomPowerToggle.OnSelectionUpdated");
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin == null)
            {
                return;
            }

            Task.Run(async () =>
            {
                if (string.IsNullOrEmpty(plugin.SelectedRoomId))
                {
                    await plugin.Client.ToggleLightsAsync();
                }
                else
                {
                    await plugin.Client.ToggleGroupAsync(plugin.SelectedRoomId);
                }
            });
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin == null || string.IsNullOrEmpty(plugin.SelectedRoomId))
            {
                return "Toggle\nAll";
            }

            var group = plugin.Groups.Find(g => g.Id == plugin.SelectedRoomId);
            return group != null ? $"Toggle\n{group.Name}" : "Toggle\nActive";
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var plugin = (LifxPlugin)this.Plugin;
            var isGroup = plugin != null && !string.IsNullOrEmpty(plugin.SelectedRoomId);
            return PluginImages.CreateBulbButtonImage(imageSize, isGroup);
        }
    }
}
