namespace Loupedeck.LifxPlugin
{
    using System;
    using System.Threading.Tasks;

    public class ActiveRoomResetColor : PluginDynamicCommand
    {
        public ActiveRoomResetColor()
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
            this.ActionImageChanged();
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
                await plugin.Client.SetColorToWhiteAsync(plugin.SelectedRoomId);
            });
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin == null || string.IsNullOrEmpty(plugin.SelectedRoomId))
            {
                return "Reset\nColor";
            }

            var group = plugin.Groups.Find(g => g.Id == plugin.SelectedRoomId);
            return group != null ? $"Reset\n{group.Name}" : "Reset\nColor";
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var plugin = (LifxPlugin)this.Plugin;
            var text = this.GetCommandDisplayName(actionParameter, imageSize);
            var isGroup = plugin != null && !string.IsNullOrEmpty(plugin.SelectedRoomId);
            return PluginImages.CreateBulbButtonImage(imageSize, text, isGroup, BitmapColor.White, PluginImages.BlackColor);
        }
    }
}
