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
            this.AddParameter("breathe", "Breathe Effect", "LIFX Effects");
            this.AddParameter("move", "Move Effect", "LIFX Effects");
            this.AddParameter("morph", "Morph Effect", "LIFX Effects");
            this.AddParameter("flame", "Flame Effect", "LIFX Effects");
            this.AddParameter("pulse", "Pulse Purple", "LIFX Effects");
            this.AddParameter("clouds", "Clouds Effect", "LIFX Effects");
            this.AddParameter("sunrise", "Sunrise Effect", "LIFX Effects");
            this.AddParameter("sunset", "Sunset Effect", "LIFX Effects");
            this.AddParameter("off", "Effects off", "LIFX Effects");
            this.AddParameter("cycle", "Cycle", "LIFX Effects");

            this.AddParameter("breathe:red", "Breathe Red", "LIFX Effects");
            this.AddParameter("breathe:green", "Breathe Green", "LIFX Effects");
            this.AddParameter("breathe:blue", "Breathe Blue", "LIFX Effects");
            this.AddParameter("breathe:purple", "Breathe Purple", "LIFX Effects");
            this.AddParameter("pulse:red", "Pulse Red", "LIFX Effects");
            this.AddParameter("pulse:green", "Pulse Green", "LIFX Effects");
            this.AddParameter("pulse:blue", "Pulse Blue", "LIFX Effects");
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

            // Track last triggered command for real-time speed adjustment
            plugin.LastEffectParameter = actionParameter;

            var roomId = plugin.ActiveSelector; // applies to active selector if selected, or all if null
            var period = plugin.EffectPeriod;

            Task.Run(async () =>
            {
                var success = false;
                if (actionParameter == "stop" || actionParameter == "off")
                {
                    success = await plugin.Client.StopEffectsAsync(roomId);
                }
                else if (actionParameter == "breathe")
                {
                    success = await plugin.Client.PlayBreatheEffectAsync("purple", period, roomId);
                }
                else if (actionParameter.StartsWith("breathe:"))
                {
                    var color = actionParameter.Substring("breathe:".Length);
                    success = await plugin.Client.PlayBreatheEffectAsync(color, period, roomId);
                }
                else if (actionParameter == "pulse")
                {
                    success = await plugin.Client.PlayPulseEffectAsync("purple", period, roomId);
                }
                else if (actionParameter.StartsWith("pulse:"))
                {
                    var color = actionParameter.Substring("pulse:".Length);
                    success = await plugin.Client.PlayPulseEffectAsync(color, period, roomId);
                }
                else if (actionParameter == "move")
                {
                    success = await plugin.Client.PlayMoveEffectAsync(period, roomId);
                }
                else if (actionParameter == "morph")
                {
                    success = await plugin.Client.PlayMorphEffectAsync(period, roomId);
                }
                else if (actionParameter == "flame")
                {
                    success = await plugin.Client.PlayFlameEffectAsync(period, roomId);
                }
                else if (actionParameter == "clouds")
                {
                    success = await plugin.Client.PlayCloudsEffectAsync(period, roomId);
                }
                else if (actionParameter == "sunrise")
                {
                    success = await plugin.Client.PlaySunriseEffectAsync(period, roomId);
                }
                else if (actionParameter == "sunset")
                {
                    success = await plugin.Client.PlaySunsetEffectAsync(period, roomId);
                }
                else if (actionParameter == "cycle")
                {
                    success = await plugin.Client.PlayCycleEffectAsync(roomId);
                }

                if (success)
                {
                    plugin.TriggerManualRefresh();
                }
            });
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            if (string.IsNullOrEmpty(actionParameter))
            {
                return "LIFX Effect";
            }

            switch (actionParameter)
            {
                case "stop":
                case "off":
                    return "Effects off";
                case "breathe":
                    return "Breathe Effect";
                case "move":
                    return "Move Effect";
                case "morph":
                    return "Morph Effect";
                case "flame":
                    return "Flame Effect";
                case "pulse":
                    return "Pulse Purple";
                case "clouds":
                    return "Clouds Effect";
                case "sunrise":
                    return "Sunrise Effect";
                case "sunset":
                    return "Sunset Effect";
                case "cycle":
                    return "Cycle";
            }

            var parts = actionParameter.Split(':');
            if (parts.Length == 2)
            {
                var effectName = char.ToUpper(parts[0][0]) + parts[0].Substring(1);
                var colorName = char.ToUpper(parts[1][0]) + parts[1].Substring(1);
                return $"{effectName} {colorName}";
            }

            return actionParameter;
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            if (string.IsNullOrEmpty(actionParameter) || imageSize == PluginImageSize.None)
            {
                return null;
            }

            switch (actionParameter)
            {
                case "stop":
                case "off":
                    return PluginImages.CreateEffectsOffImage(imageSize);
                case "breathe":
                    return PluginImages.CreateBreatheEffectImage(imageSize);
                case "move":
                    return PluginImages.CreateMoveEffectImage(imageSize);
                case "morph":
                    return PluginImages.CreateMorphEffectImage(imageSize);
                case "flame":
                    return PluginImages.CreateFlameEffectImage(imageSize);
                case "pulse":
                    return PluginImages.CreatePulseEffectImage(imageSize);
                case "clouds":
                    return PluginImages.CreateCloudsEffectImage(imageSize);
                case "sunrise":
                    return PluginImages.CreateSunriseEffectImage(imageSize);
                case "sunset":
                    return PluginImages.CreateSunsetEffectImage(imageSize);
                case "cycle":
                    return PluginImages.CreateCycleEffectImage(imageSize);
            }

            if (actionParameter.StartsWith("breathe:"))
            {
                var colorStr = actionParameter.Substring("breathe:".Length);
                var color = ParseColorString(colorStr);
                return PluginImages.CreateBreatheEffectImage(imageSize, color);
            }

            if (actionParameter.StartsWith("pulse:"))
            {
                var colorStr = actionParameter.Substring("pulse:".Length);
                var color = ParseColorString(colorStr);
                return PluginImages.CreatePulseEffectImage(imageSize, color);
            }

            return null;
        }

        private static BitmapColor ParseColorString(string colorStr)
        {
            switch (colorStr)
            {
                case "red":
                    return new BitmapColor(255, 50, 50);
                case "green":
                    return new BitmapColor(50, 255, 50);
                case "blue":
                    return new BitmapColor(50, 150, 255);
                case "purple":
                    return PluginImages.PurpleColor;
                default:
                    return PluginImages.PurpleColor;
            }
        }
    }
}
