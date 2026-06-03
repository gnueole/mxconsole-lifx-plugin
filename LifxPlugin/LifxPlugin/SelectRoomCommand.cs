namespace Loupedeck.LifxPlugin
{
    using System;

    public class SelectRoomCommand : PluginDynamicCommand
    {
        public SelectRoomCommand()
            : base()
        {
        }

        protected override bool OnLoad()
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin != null)
            {
                plugin.GroupsUpdated += this.OnGroupsUpdated;
                plugin.SelectionUpdated += this.OnSelectionUpdated;

                // Load groups if already populated
                if (plugin.Groups.Count > 0)
                {
                    this.OnGroupsUpdated(this, EventArgs.Empty);
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
                plugin.SelectionUpdated -= this.OnSelectionUpdated;
            }
            return true;
        }

        private void OnGroupsUpdated(object sender, EventArgs e)
        {
            var plugin = (LifxPlugin)this.Plugin;

            this.RemoveAllParameters();

            // Register room selectors
            foreach (var group in plugin.Groups)
            {
                this.AddParameter(group.Id, group.Name, "LIFX Room Selector");
            }

            this.ParametersChanged();
        }

        private void OnSelectionUpdated(object sender, EventArgs e)
        {
            this.ActionImageChanged();
        }

        protected override void RunCommand(String actionParameter)
        {
            var plugin = (LifxPlugin)this.Plugin;
            if (plugin == null || string.IsNullOrEmpty(actionParameter))
            {
                return;
            }

            if (plugin.SelectedRoomId == actionParameter)
            {
                // Toggle off (deselect, returning to "All Lights")
                plugin.SelectedRoomId = null;
            }
            else
            {
                plugin.SelectedRoomId = actionParameter;
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            if (string.IsNullOrEmpty(actionParameter))
            {
                return "Select Room";
            }

            var plugin = (LifxPlugin)this.Plugin;
            var group = plugin.Groups.Find(g => g.Id == actionParameter);
            return group != null ? group.Name : "Room";
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var plugin = (LifxPlugin)this.Plugin;
            var displayName = this.GetCommandDisplayName(actionParameter, imageSize);

            if (plugin != null && plugin.SelectedRoomId == actionParameter)
            {
                // Active/Selected state: Purple background, Black text
                return PluginImages.CreateBulbButtonImage(imageSize, displayName, true, PluginImages.BlackColor, PluginImages.PurpleColor);
            }
            else
            {
                // Inactive state: Black background, Purple text
                return PluginImages.CreateBulbButtonImage(imageSize, displayName, true, PluginImages.PurpleColor, PluginImages.BlackColor);
            }
        }
    }
}
