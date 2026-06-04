namespace Loupedeck.LifxPlugin
{
    using System;
    using System.Threading.Tasks;

    public class ToggleLightCommand : PluginDynamicCommand
    {
        public ToggleLightCommand()
            : base("On/OFf", "Toggle active room or house lights", "LIFX")
        {
        }

        protected override bool OnLoad()
        {
            return true;
        }

        protected override bool OnUnload()
        {
            return true;
        }

        protected override void RunCommand(String actionParameter)
        {
            var plugin = (LifxPlugin)this.Plugin;

            Task.Run(async () =>
            {
                if (string.IsNullOrEmpty(actionParameter))
                {
                    if (string.IsNullOrEmpty(plugin.SelectedRoomId))
                    {
                        await plugin.Client.ToggleLightsAsync();
                    }
                    else
                    {
                        await plugin.Client.ToggleGroupAsync(plugin.SelectedRoomId);
                    }
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
                return "On/OFf";
            }

            var plugin = (LifxPlugin)this.Plugin;
            var group = plugin.Groups.Find(g => g.Id == actionParameter);
            return group != null ? $"Toggle\n{group.Name}" : "Toggle Group";
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            if (string.IsNullOrEmpty(actionParameter))
            {
                return PluginImages.CreatePowerButtonImage(imageSize);
            }

            var isGroup = !string.IsNullOrEmpty(actionParameter);
            return PluginImages.CreateBulbButtonImage(imageSize, isGroup);
        }
    }
}
