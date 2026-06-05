namespace Loupedeck.LifxPlugin
{
    using System;
    using System.Threading.Tasks;

    public class EffectPeriodAdjustment : PluginDynamicAdjustment
    {
        private RequestCoalescer _coalescer;

        public EffectPeriodAdjustment()
            : base(displayName: "Effect Speed", description: "Adjust speed of LIFX light effects", groupName: "LIFX", hasReset: true)
        {
        }

        protected override bool OnLoad()
        {
            this.AddParameter(string.Empty, "Effect Speed", "LIFX");
            return true;
        }

        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin == null)
            {
                return;
            }

            // Adjust period by 0.1s per tick, clamped between 0.5s and 10.0s
            plugin.EffectPeriod += diff * 0.1;
            plugin.EffectPeriod = Math.Max(0.5, Math.Min(10.0, plugin.EffectPeriod));

            PluginLog.Info($"[Effect Speed] diff={diff:+0;-0}, target={plugin.EffectPeriod:0.0}s");
            this.AdjustmentValueChanged();

            if (this._coalescer == null)
            {
                this._coalescer = new RequestCoalescer(async () =>
                {
                    if (plugin.Client == null || string.IsNullOrEmpty(plugin.LastEffectParameter))
                    {
                        return;
                    }

                    var roomId = plugin.ActiveSelector;
                    var param = plugin.LastEffectParameter;
                    var period = plugin.EffectPeriod;

                    PluginLog.Info($"[Effect Speed] Re-triggering last effect '{param}' at period {period:0.0}s...");

                    if (LifxEffectsCommand.Effects.TryGetValue(param, out var def))
                    {
                        await def.Run(plugin, roomId, period);
                    }
                }, 300); // 300ms delay to throttle rapid turns
            }
            this._coalescer.Trigger();
        }

        protected override void RunCommand(String actionParameter)
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin == null)
            {
                return;
            }

            // Reset period to default 2.0s
            plugin.EffectPeriod = 2.0;
            PluginLog.Info("[Effect Speed] Reset to 2.0s");
            this.AdjustmentValueChanged();

            if (this._coalescer != null)
            {
                this._coalescer.Trigger();
            }
        }

        protected override String GetAdjustmentValue(String actionParameter)
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin == null)
            {
                return "2.0s";
            }
            return $"{plugin.EffectPeriod:0.0}s";
        }

        protected override BitmapImage GetAdjustmentImage(String actionParameter, PluginImageSize imageSize)
        {
            return PluginImages.CreateBrightnessGaugeImage(imageSize);
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            return PluginImages.CreateBulbButtonImage(imageSize, false, PluginImages.PurpleColor, PluginImages.BlackColor);
        }

        protected override Boolean ProcessEncoderEvent(String actionParameter, DeviceEncoderEvent encoderEvent)
        {
            this.ApplyAdjustment(actionParameter, encoderEvent.Clicks);
            return true;
        }
    }
}
