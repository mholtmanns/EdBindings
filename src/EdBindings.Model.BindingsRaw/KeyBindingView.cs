namespace EdBindings.Model
{
    using EdBindings.Model.BindingsRaw.Bindings;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Class KeyBindingView.
    /// </summary>
    [DebuggerDisplay("{Name} {PrimaryDevice}/{PrimaryKey}; {SecondaryDevice}/{SecondaryKey}")]
    public class KeyBindingView
    {
        /// <summary>
        /// Gets or sets the area.
        /// </summary>
        /// <value>The area.</value>
        public string Area { get; set; }

        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        /// <value>The category.</value>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        /// <value>The action.</value>
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the primary device.
        /// </summary>
        /// <value>The primary device.</value>
        public string PrimaryDevice { get; set; }

        /// <summary>
        /// Gets or sets the primary key.
        /// </summary>
        /// <value>The primary key.</value>
        public string PrimaryKey { get; set; }

        /// <summary>
        /// Gets or sets the secondary device.
        /// </summary>
        /// <value>The secondary device.</value>
        public string SecondaryDevice { get; set; }

        /// <summary>
        /// Gets or sets the secondary key.
        /// </summary>
        /// <value>The secondary key.</value>
        public string SecondaryKey { get; set; }

        /// <summary>
        /// Gets or sets the bind ed variable.
        /// </summary>
        /// <value>The bind ed variable.</value>
        public string BindEdVariable { get; set; }

        /// <summary>
        /// Gets notes for this keybind.
        /// </summary>
        /// <value>In-Game notes for this keybind</value>
        public string Note { get; set; }


        /// <summary>
        /// Makes the key binding view.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="deviceMaps">The device mapping list.</param>
        /// <returns>KeyBindingView.</returns>
        public static KeyBindingView MakeKeyBindingView(BindingGroup group, List<DeviceMap> deviceMaps, List<ActionMapping> actionMappings)
        {
            var view = new KeyBindingView();

            var actionMapping = actionMappings.FirstOrDefault(am => am.Code.Equals(group.Name, StringComparison.OrdinalIgnoreCase));
            view.Area = actionMapping?.Area ?? string.Empty;
            view.Category = actionMapping?.Category ?? string.Empty;
            view.Action = actionMapping?.Action ?? group.Name;
            view.Note = actionMapping?.Note ?? string.Empty;

            var primary = (BindingDevice)group.Bindings.First(b => new[] { "Binding", "Primary" }.Contains(b.Name));

            var primaryDeviceMap = deviceMaps?.Select(dm => dm.FindControlMap(primary)).FirstOrDefault(dcm => dcm != null);

            var secondary = (BindingDevice)group.Bindings.FirstOrDefault(b => b.Name == "Secondary");

            // var secondaryDeviceMap = deviceMap.FindControlMap(secondary);
            var secondaryDeviceMap = deviceMaps?.Select(dm => dm.FindControlMap(secondary)).FirstOrDefault(dcm => dcm != null);

            view.PrimaryDevice = primaryDeviceMap?.DeviceName ?? primary.Device;
            view.PrimaryKey = primaryDeviceMap?.ControlLabel ?? primary.Key;
            view.SecondaryDevice = secondaryDeviceMap?.DeviceName ?? secondary?.Device;
            view.SecondaryKey = secondaryDeviceMap?.ControlLabel ?? secondary?.Key;
            view.BindEdVariable = $"ed{group.Name}";

            return view;
        }


    }
}
