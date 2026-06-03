namespace Loupedeck.LifxPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class BrightnessAdjustment : PluginDynamicAdjustment
    {
        private double _cachedBrightness = 0.5;
        private bool _isInitialized = false;

        private readonly Dictionary<string, double> _groupBrightnesses = new Dictionary<string, double>();
        private readonly HashSet<string> _initializedGroups = new HashSet<string>();

        public BrightnessAdjustment()
            : base(displayName: "Brightness", description: "Adjust room or light brightness", groupName: "LIFX", hasReset: true)
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
                    this.AddParameter(string.Empty, "All Brightness", "LIFX");
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
            this.AddParameter(string.Empty, "All Brightness", "LIFX");

            // Register group actions
            foreach (var group in plugin.Groups)
            {
                this.AddParameter(group.Id, group.Name, "LIFX Rooms");
            }

            this.ParametersChanged();
        }

        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            var plugin = (LifxPlugin)this.Plugin;

            if (string.IsNullOrEmpty(actionParameter))
            {
                // Global brightness adjustment
                this._cachedBrightness += diff * 0.05;
                this._cachedBrightness = Math.Max(0.0, Math.Min(1.0, this._cachedBrightness));

                this.AdjustmentValueChanged();

                Task.Run(async () =>
                {
                    await plugin.Client.SetBrightnessAsync(this._cachedBrightness);
                });
            }
            else
            {
                // Group-specific brightness adjustment
                double currentVal = 0.5;
                lock (this._groupBrightnesses)
                {
                    if (this._groupBrightnesses.TryGetValue(actionParameter, out double cachedVal))
                    {
                        currentVal = cachedVal;
                    }
                }

                currentVal += diff * 0.05;
                currentVal = Math.Max(0.0, Math.Min(1.0, currentVal));

                lock (this._groupBrightnesses)
                {
                    this._groupBrightnesses[actionParameter] = currentVal;
                }

                this.AdjustmentValueChanged(actionParameter);

                Task.Run(async () =>
                {
                    await plugin.Client.SetGroupBrightnessAsync(actionParameter, currentVal);
                });
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            var plugin = (LifxPlugin)this.Plugin;

            if (string.IsNullOrEmpty(actionParameter))
            {
                // Reset global brightness to 100%
                this._cachedBrightness = 1.0;
                this.AdjustmentValueChanged();

                Task.Run(async () =>
                {
                    await plugin.Client.SetBrightnessAsync(this._cachedBrightness);
                });
            }
            else
            {
                // Reset group brightness to 100%
                lock (this._groupBrightnesses)
                {
                    this._groupBrightnesses[actionParameter] = 1.0;
                }

                this.AdjustmentValueChanged(actionParameter);

                Task.Run(async () =>
                {
                    await plugin.Client.SetGroupBrightnessAsync(actionParameter, 1.0);
                });
            }
        }

        protected override String GetAdjustmentValue(String actionParameter)
        {
            var plugin = (LifxPlugin)this.Plugin;

            if (string.IsNullOrEmpty(actionParameter))
            {
                if (!this._isInitialized)
                {
                    this._isInitialized = true;
                    Task.Run(async () =>
                    {
                        if (plugin?.Client != null)
                        {
                            this._cachedBrightness = await plugin.Client.GetBrightnessAsync();
                            this.AdjustmentValueChanged();
                        }
                    });
                }
                return $"{Math.Round(this._cachedBrightness * 100)}%";
            }
            else
            {
                if (!this._initializedGroups.Contains(actionParameter))
                {
                    this._initializedGroups.Add(actionParameter);
                    Task.Run(async () =>
                    {
                        if (plugin?.Client != null)
                        {
                            double brightness = await plugin.Client.GetGroupBrightnessAsync(actionParameter);
                            lock (this._groupBrightnesses)
                            {
                                this._groupBrightnesses[actionParameter] = brightness;
                            }
                            this.AdjustmentValueChanged(actionParameter);
                        }
                    });
                }

                double val = 0.5;
                lock (this._groupBrightnesses)
                {
                    if (this._groupBrightnesses.TryGetValue(actionParameter, out double cachedVal))
                    {
                        val = cachedVal;
                    }
                }
                return $"{Math.Round(val * 100)}%";
            }
        }
    }
}
