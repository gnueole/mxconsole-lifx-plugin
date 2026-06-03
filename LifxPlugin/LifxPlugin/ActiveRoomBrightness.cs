namespace Loupedeck.LifxPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class ActiveRoomBrightness : PluginDynamicAdjustment
    {
        private double _globalBrightness = 0.5;
        private bool _globalInitialized = false;

        private readonly Dictionary<string, double> _groupBrightnesses = new Dictionary<string, double>();
        private readonly HashSet<string> _initializedGroups = new HashSet<string>();

        public ActiveRoomBrightness()
            : base(displayName: "Active Brightness", description: "Adjust brightness of active room", groupName: "LIFX", hasReset: true)
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
                this.AdjustmentValueChanged();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Error in ActiveRoomBrightness.OnSelectionUpdated");
            }
        }

        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin == null)
            {
                return;
            }

            var roomId = plugin.SelectedRoomId;

            if (string.IsNullOrEmpty(roomId))
            {
                // Global brightness adjustment
                this._globalBrightness += diff * 0.05;
                this._globalBrightness = Math.Max(0.0, Math.Min(1.0, this._globalBrightness));

                this.AdjustmentValueChanged();

                Task.Run(async () =>
                {
                    await plugin.Client.SetBrightnessAsync(this._globalBrightness);
                });
            }
            else
            {
                // Group-specific brightness adjustment
                double currentVal = 0.5;
                lock (this._groupBrightnesses)
                {
                    if (this._groupBrightnesses.TryGetValue(roomId, out double cachedVal))
                    {
                        currentVal = cachedVal;
                    }
                }

                currentVal += diff * 0.05;
                currentVal = Math.Max(0.0, Math.Min(1.0, currentVal));

                lock (this._groupBrightnesses)
                {
                    this._groupBrightnesses[roomId] = currentVal;
                }

                this.AdjustmentValueChanged();

                Task.Run(async () =>
                {
                    await plugin.Client.SetGroupBrightnessAsync(roomId, currentVal);
                });
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin == null)
            {
                return;
            }

            var roomId = plugin.SelectedRoomId;

            if (string.IsNullOrEmpty(roomId))
            {
                // Reset global brightness to 100%
                this._globalBrightness = 1.0;
                this.AdjustmentValueChanged();

                Task.Run(async () =>
                {
                    await plugin.Client.SetBrightnessAsync(this._globalBrightness);
                });
            }
            else
            {
                // Reset group brightness to 100%
                lock (this._groupBrightnesses)
                {
                    this._groupBrightnesses[roomId] = 1.0;
                }

                this.AdjustmentValueChanged();

                Task.Run(async () =>
                {
                    await plugin.Client.SetGroupBrightnessAsync(roomId, 1.0);
                });
            }
        }

        protected override String GetAdjustmentValue(String actionParameter)
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin == null)
            {
                return "50%";
            }

            var roomId = plugin.SelectedRoomId;

            if (string.IsNullOrEmpty(roomId))
            {
                if (!this._globalInitialized)
                {
                    this._globalInitialized = true;
                    Task.Run(async () =>
                    {
                        if (plugin?.Client != null)
                        {
                            this._globalBrightness = await plugin.Client.GetBrightnessAsync();
                            this.AdjustmentValueChanged();
                        }
                    });
                }
                return $"{Math.Round(this._globalBrightness * 100)}%";
            }
            else
            {
                bool shouldInit = false;
                lock (this._initializedGroups)
                {
                    if (!this._initializedGroups.Contains(roomId))
                    {
                        this._initializedGroups.Add(roomId);
                        shouldInit = true;
                    }
                }

                if (shouldInit)
                {
                    Task.Run(async () =>
                    {
                        if (plugin?.Client != null)
                        {
                            double brightness = await plugin.Client.GetGroupBrightnessAsync(roomId);
                            lock (this._groupBrightnesses)
                            {
                                this._groupBrightnesses[roomId] = brightness;
                            }
                            this.AdjustmentValueChanged();
                        }
                    });
                }

                double val = 0.5;
                lock (this._groupBrightnesses)
                {
                    if (this._groupBrightnesses.TryGetValue(roomId, out double cachedVal))
                    {
                        val = cachedVal;
                    }
                }
                return $"{Math.Round(val * 100)}%";
            }
        }

        protected override String GetAdjustmentDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin == null || string.IsNullOrEmpty(plugin.SelectedRoomId))
            {
                return "Brightness";
            }

            var group = plugin.Groups.Find(g => g.Id == plugin.SelectedRoomId);
            return group != null ? $"{group.Name}" : "Brightness";
        }
    }
}
