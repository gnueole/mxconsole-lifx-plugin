namespace Loupedeck.LifxPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class ActiveRoomHue : PluginDynamicAdjustment
    {
        private double _globalHue = 0.0;
        private bool _globalInitialized = false;

        private readonly Dictionary<string, double> _groupHues = new Dictionary<string, double>();
        private readonly HashSet<string> _initializedGroups = new HashSet<string>();

        public ActiveRoomHue()
            : base(displayName: "Active Hue", description: "Adjust color (hue) of active room", groupName: "LIFX", hasReset: true)
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
                PluginLog.Error(ex, "Error in ActiveRoomHue.OnSelectionUpdated");
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
                // Global hue adjustment
                this._globalHue = (this._globalHue + diff * 5.0) % 360.0;
                if (this._globalHue < 0)
                {
                    this._globalHue += 360.0;
                }

                this.AdjustmentValueChanged();

                Task.Run(async () =>
                {
                    await plugin.Client.SetHueAsync(this._globalHue);
                });
            }
            else
            {
                // Group-specific hue adjustment
                double currentVal = 0.0;
                lock (this._groupHues)
                {
                    if (this._groupHues.TryGetValue(roomId, out double cachedVal))
                    {
                        currentVal = cachedVal;
                    }
                }

                currentVal = (currentVal + diff * 5.0) % 360.0;
                if (currentVal < 0)
                {
                    currentVal += 360.0;
                }

                lock (this._groupHues)
                {
                    this._groupHues[roomId] = currentVal;
                }

                this.AdjustmentValueChanged();

                Task.Run(async () =>
                {
                    await plugin.Client.SetGroupHueAsync(roomId, currentVal);
                });
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            // Reset hue to Red (0 degrees)
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin == null)
            {
                return;
            }

            var roomId = plugin.SelectedRoomId;

            if (string.IsNullOrEmpty(roomId))
            {
                this._globalHue = 0.0;
                this.AdjustmentValueChanged();

                Task.Run(async () =>
                {
                    await plugin.Client.SetHueAsync(this._globalHue);
                });
            }
            else
            {
                lock (this._groupHues)
                {
                    this._groupHues[roomId] = 0.0;
                }

                this.AdjustmentValueChanged();

                Task.Run(async () =>
                {
                    await plugin.Client.SetGroupHueAsync(roomId, 0.0);
                });
            }
        }

        protected override String GetAdjustmentValue(String actionParameter)
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin == null)
            {
                return "0°";
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
                            this._globalHue = await plugin.Client.GetHueAsync();
                            this.AdjustmentValueChanged();
                        }
                    });
                }
                return $"{Math.Round(this._globalHue)}°";
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
                            double hue = await plugin.Client.GetGroupHueAsync(roomId);
                            lock (this._groupHues)
                            {
                                this._groupHues[roomId] = hue;
                            }
                            this.AdjustmentValueChanged();
                        }
                    });
                }

                double val = 0.0;
                lock (this._groupHues)
                {
                    if (this._groupHues.TryGetValue(roomId, out double cachedVal))
                    {
                        val = cachedVal;
                    }
                }
                return $"{Math.Round(val)}°";
            }
        }

        protected override String GetAdjustmentDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            if (imageSize == PluginImageSize.None)
            {
                var plugin = (LifxPlugin)this.Plugin;
                if (plugin != null && !string.IsNullOrEmpty(plugin.SelectedRoomId))
                {
                    var group = plugin.Groups.Find(g => g.Id == plugin.SelectedRoomId);
                    if (group != null)
                    {
                        return $"{group.Name} Hue";
                    }
                }
                return "Active Hue";
            }
            return "";
        }

        protected override BitmapImage GetAdjustmentImage(String actionParameter, PluginImageSize imageSize)
        {
            return PluginImages.CreateColorWheelImage(imageSize, "Hue");
        }
    }
}
