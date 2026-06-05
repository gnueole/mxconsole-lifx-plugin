namespace Loupedeck.LifxPlugin
{
    using System;
    using System.Threading.Tasks;

    public class EffectPeriodAdjustment : PluginDynamicAdjustment
    {
        // ── Tunable constants ─────────────────────────────────────────────────────
        internal const double DefaultPeriod  = 2.0;   // seconds — initial / reset value
        internal const double MinPeriod      = 0.5;   // seconds — slowest allowed effect
        internal const double MaxPeriod      = 10.0;  // seconds — fastest allowed effect
        internal const double StepPerTick    = 0.1;   // seconds added/removed per encoder click
        internal const int    CoalesceDelayMs = 300;  // ms — throttle rapid encoder turns before re-triggering

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

            plugin.EffectPeriod += diff * StepPerTick;
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
                    var param  = plugin.LastEffectParameter;
                    var period = plugin.EffectPeriod;

                    PluginLog.Info($"[Effect Speed] Re-triggering last effect '{param}' at period {period:0.0}s...");

                    if (LifxEffectsCommand.Effects.TryGetValue(param, out var def))
                    {
                        await def.Run(plugin, roomId, period);
                    }
                }, CoalesceDelayMs);
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

            plugin.EffectPeriod = DefaultPeriod;
            PluginLog.Info($"[Effect Speed] Reset to {DefaultPeriod:0.0}s");
            this.AdjustmentValueChanged();

            if (this._coalescer != null)
            {
                this._coalescer.Trigger();
            }
        }

        protected override String GetAdjustmentValue(String actionParameter)
        {
            var plugin = (LifxPlugin)this.Plugin;
            return plugin == null ? $"{DefaultPeriod:0.0}s" : $"{plugin.EffectPeriod:0.0}s";
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
