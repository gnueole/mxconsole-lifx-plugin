namespace Loupedeck.LifxPlugin
{
    using System;
    using System.Threading.Tasks;

    public class ToggleLightCommand : PluginDynamicCommand
    {
        public ToggleLightCommand()
            : base("On/Off", "Toggle active room or house lights", "LIFX")
        {
        }

        protected override bool OnLoad()
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin != null)
            {
                plugin.GroupsUpdated += this.OnGroupsUpdated;
                plugin.LightsUpdated += this.OnLightsUpdated;

                if (plugin.Groups.Count > 0 || plugin.Lights.Count > 0)
                {
                    this.UpdateParameters();
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
                plugin.LightsUpdated -= this.OnLightsUpdated;
            }
            return true;
        }

        private void OnGroupsUpdated(object sender, EventArgs e) => this.UpdateParameters();
        private void OnLightsUpdated(object sender, EventArgs e) => this.UpdateParameters();

        private void UpdateParameters()
        {
            try
            {
                var plugin = (LifxPlugin)this.Plugin;
                if (plugin == null)
                {
                    return;
                }

                this.RemoveAllParameters();

                // Add Toggle All
                this.AddParameter("all", "Toggle All", "LIFX Toggle");

                // Register group toggles
                if (plugin.Groups != null)
                {
                    foreach (var group in plugin.Groups)
                    {
                        this.AddParameter($"group_id:{group.Id}", $"Toggle {group.Name}", "LIFX Group Toggle");
                    }
                }

                // Register light toggles
                if (plugin.Lights != null)
                {
                    foreach (var light in plugin.Lights)
                    {
                        this.AddParameter($"id:{light.Id}", $"Toggle {light.Name}", "LIFX Light Toggle");
                    }
                }

                this.ParametersChanged();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Error in ToggleLightCommand.UpdateParameters");
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
                var success = false;
                if (string.IsNullOrEmpty(actionParameter))
                {
                    if (string.IsNullOrEmpty(plugin.ActiveSelector))
                    {
                        success = await plugin.Client.ToggleLightsAsync();
                    }
                    else
                    {
                        success = await plugin.Client.ToggleGroupAsync(plugin.ActiveSelector);
                    }
                }
                else
                {
                    success = await plugin.Client.ToggleGroupAsync(actionParameter);
                }

                if (success)
                {
                    plugin.TriggerManualRefresh();
                }
            });
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (string.IsNullOrEmpty(actionParameter))
            {
                if (plugin != null && !string.IsNullOrEmpty(plugin.ActiveSelector))
                {
                    if (plugin.ActiveSelector.StartsWith("group_id:"))
                    {
                        var groupId = plugin.ActiveSelector.Substring("group_id:".Length);
                        var group = plugin.Groups.Find(g => g.Id == groupId);
                        return group != null ? $"Toggle\n{group.Name}" : "Toggle Room";
                    }
                    else if (plugin.ActiveSelector.StartsWith("id:"))
                    {
                        var lightId = plugin.ActiveSelector.Substring("id:".Length);
                        var light = plugin.Lights.Find(l => l.Id == lightId);
                        return light != null ? $"Toggle\n{light.Name}" : "Toggle Light";
                    }
                }
                return "On/Off";
            }

            if (plugin == null)
            {
                return "Toggle Group";
            }

            if (actionParameter.StartsWith("group_id:"))
            {
                var groupId = actionParameter.Substring("group_id:".Length);
                var group = plugin.Groups.Find(g => g.Id == groupId);
                return group != null ? $"Toggle\n{group.Name}" : "Toggle Group";
            }
            else if (actionParameter.StartsWith("id:"))
            {
                var lightId = actionParameter.Substring("id:".Length);
                var light = plugin.Lights.Find(l => l.Id == lightId);
                return light != null ? $"Toggle\n{light.Name}" : "Toggle Light";
            }
            else
            {
                var group = plugin.Groups.Find(g => g.Id == actionParameter);
                return group != null ? $"Toggle\n{group.Name}" : "Toggle Group";
            }
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            if (string.IsNullOrEmpty(actionParameter))
            {
                return PluginImages.CreatePowerButtonImage(imageSize);
            }

            var plugin = (LifxPlugin)this.Plugin;
            var parameterIsGroup = actionParameter.StartsWith("group_id:") || !actionParameter.StartsWith("id:");

            if (parameterIsGroup)
            {
                return PluginImages.CreateBulbButtonImage(imageSize, true);
            }
            else
            {
                var lightId = actionParameter.StartsWith("id:") ? actionParameter.Substring("id:".Length) : actionParameter;
                var light = plugin?.Lights.Find(l => l.Id == lightId);
                var type = light?.Type ?? "bulb";
                return PluginImages.CreateLightTypeButtonImage(imageSize, type, false);
            }
        }
    }
}
