namespace Loupedeck.LifxPlugin
{
    using System;
    using System.Threading.Tasks;

    public class LifxEffectsCommand : PluginDynamicCommand
    {
        public LifxEffectsCommand()
            : base()
        {
        }

        protected override bool OnLoad()
        {
            this.AddParameter("breathe:red", "Breathe Red", "LIFX Effects");
            this.AddParameter("breathe:green", "Breathe Green", "LIFX Effects");
            this.AddParameter("breathe:blue", "Breathe Blue", "LIFX Effects");
            this.AddParameter("breathe:purple", "Breathe Purple", "LIFX Effects");
            this.AddParameter("pulse:red", "Pulse Red", "LIFX Effects");
            this.AddParameter("pulse:green", "Pulse Green", "LIFX Effects");
            this.AddParameter("pulse:blue", "Pulse Blue", "LIFX Effects");
            this.AddParameter("pulse:purple", "Pulse Purple", "LIFX Effects");
            this.AddParameter("stop", "Stop Effects", "LIFX Effects");

            this.ParametersChanged();
            return true;
        }

        protected override void RunCommand(String actionParameter)
        {
            if (string.IsNullOrEmpty(actionParameter))
            {
                return;
            }

            var plugin = (LifxPlugin)this.Plugin;
            if (plugin == null || plugin.Client == null)
            {
                return;
            }

            var roomId = plugin.SelectedRoomId; // applies to active room if selected, or all if null

            Task.Run(async () =>
            {
                if (actionParameter == "stop")
                {
                    await plugin.Client.StopEffectsAsync(roomId);
                }
                else if (actionParameter.StartsWith("breathe:"))
                {
                    var color = actionParameter.Substring("breathe:".Length);
                    await plugin.Client.PlayBreatheEffectAsync(color, roomId);
                }
                else if (actionParameter.StartsWith("pulse:"))
                {
                    var color = actionParameter.Substring("pulse:".Length);
                    await plugin.Client.PlayPulseEffectAsync(color, roomId);
                }
            });
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            if (string.IsNullOrEmpty(actionParameter))
            {
                return "LIFX Effect";
            }

            if (actionParameter == "stop")
            {
                return "Stop\nEffects";
            }

            var parts = actionParameter.Split(':');
            if (parts.Length == 2)
            {
                var effectName = char.ToUpper(parts[0][0]) + parts[0].Substring(1);
                var colorName = char.ToUpper(parts[1][0]) + parts[1].Substring(1);
                return $"{effectName}\n{colorName}";
            }

            return actionParameter;
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            if (string.IsNullOrEmpty(actionParameter) || imageSize == PluginImageSize.None)
            {
                return null;
            }

            if (actionParameter == "stop")
            {
                return PluginImages.CreateButtonImage(imageSize, "Stop\nEffects", PluginImages.DullWhiteColor, PluginImages.BlackColor);
            }

            var parts = actionParameter.Split(':');
            if (parts.Length == 2)
            {
                var effect = parts[0];
                var colorStr = parts[1];

                BitmapColor color;
                switch (colorStr)
                {
                    case "red":
                        color = new BitmapColor(255, 50, 50);
                        break;
                    case "green":
                        color = new BitmapColor(50, 255, 50);
                        break;
                    case "blue":
                        color = new BitmapColor(50, 150, 255);
                        break;
                    case "purple":
                        color = PluginImages.PurpleColor;
                        break;
                    default:
                        color = PluginImages.PurpleColor;
                        break;
                }

                var dispName = char.ToUpper(effect[0]) + effect.Substring(1) + "\n" + char.ToUpper(colorStr[0]) + colorStr.Substring(1);
                return PluginImages.CreateButtonImage(imageSize, dispName, color, PluginImages.BlackColor);
            }

            return null;
        }
    }
}
