namespace EdBindings
{
    using EdBindings.Model;
    using EdBindings.Model.BindingsRaw;

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// The place holder text
        /// </summary>
        private const string placeHolderText = "Filter...";

        /// <summary>
        /// Gets or sets the binding file.
        /// </summary>
        /// <value>The binding file.</value>
        private BindingFile BindingFile { get; set; }

        /// <summary>
        /// Gets or sets the device map.
        /// </summary>
        /// <value>The device map.</value>
        private List<DeviceMap> DeviceMaps { get; set; }

        /// <summary>
        /// Gets or sets the key bindings.
        /// </summary>
        /// <value>The key bindings.</value>
        private ICollectionView KeyBindings { get; set; }

        /// <summary>
        /// Gets or sets the action mapping.
        /// </summary>
        /// <value>The action mapping.</value>
        private List<ActionMapping> ActionMappings { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            this.DeviceMaps = new List<DeviceMap>();

            this.ActionMappings = ActionMapping.Open(Path.GetFullPath(@".\ActionMappings.json"));

            var deviceMappingFiles = Directory.GetFiles(@".\DeviceMappings");

            foreach(var deviceMappingFile in deviceMappingFiles)
            {

                var deviceMapping = DeviceMap.Open(deviceMappingFile);

                var menuItem = new MenuItem();
                menuItem.Header = deviceMapping.Name;
                menuItem.DataContext = deviceMapping;
                menuItem.Click += this.DeviceMapSelected;
                menuItem.IsCheckable = true;

                this.DeviceMappingMenu.Items.Add(menuItem);
            }
            var checkedMenuItem = (MenuItem)this.DeviceMappingMenu.Items[ApplicationSettings.Default.DeviceMapSelection];
            checkedMenuItem.IsChecked = true;
            this.SelectActiveDeviceMapping(ApplicationSettings.Default.DeviceMapSelection);
        }

        /// <summary>
        /// Devices the map selected.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void DeviceMapSelected(object sender, RoutedEventArgs e)
        {
            var selectedIndex = this.DeviceMappingMenu.Items.IndexOf(sender);
            this.SelectActiveDeviceMapping(selectedIndex);
        }

        /// <summary>
        /// Selects the active device mapping.
        /// </summary>
        /// <param name="index">The index.</param>
        private void SelectActiveDeviceMapping(int index)
        {
            var menuItem = (MenuItem)this.DeviceMappingMenu.Items[index];
            string deviceMapName = "Device Mapping: ";

            foreach(MenuItem dmItem in this.DeviceMappingMenu.Items.OfType<MenuItem>())
            {
                if (dmItem.IsChecked)
                {
                    this.DeviceMaps.Add((DeviceMap)dmItem.DataContext);
                    deviceMapName += $"{dmItem.Header} | ";
                }
            }
            // Remove the trailing " | "
            deviceMapName = deviceMapName.Substring(0, deviceMapName.Length - 3);

            // The last selected Device mapping will be stored as default.
            // [TODO] This should be changed to take all selected mappigns into account
            if(index != ApplicationSettings.Default.DeviceMapSelection)
            {
                ApplicationSettings.Default.DeviceMapSelection = index;
                ApplicationSettings.Default.Save();
            }

            DeviceFileStatusBar.Content = deviceMapName;
            this.ProcessBindingFile();

        }

        /// <summary>
        /// Files the exit menu item click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void FileExitMenuItemClick(object sender, RoutedEventArgs e) => this.Close();

        /// <summary>
        /// Files the open bindings menu item click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void FileOpenBindingsMenuItemClick(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".binds";
            dialog.InitialDirectory = Environment.ExpandEnvironmentVariables(@"%localappdata%\Frontier Developments\Elite Dangerous\Options\Bindings");
            dialog.Filter = "Bindings (*.binds)|*.binds|All files (*.*)|*.*";
            if (dialog.ShowDialog() == true)
            {
                this.BindingFile = BindingFile.Open(dialog.FileName);
                this.ProcessBindingFile();
            }
        }

        /// <summary>
        /// Processes the binding file.
        /// </summary>
        private void ProcessBindingFile()
        {
            if (this.BindingFile == null)
            {
                return;
            }

            var justBindingGroups = this.BindingFile.Bindings.Where(binding => binding is EdBindings.Model.BindingsRaw.Bindings.BindingGroup).ToList();
            var dataSource = justBindingGroups.Select(group => KeyBindingView.MakeKeyBindingView((EdBindings.Model.BindingsRaw.Bindings.BindingGroup)group, this.DeviceMaps, this.ActionMappings)).ToList();
            var filterable = new CollectionViewSource() { Source = new ObservableCollection<KeyBindingView>(dataSource) };

            this.KeyBindings = filterable.View;

            this.KeyBindingDataGrid.ItemsSource = this.KeyBindings;
            this.BindingFileStatusBar.Content = Path.GetFileName(this.BindingFile.FileName);
            this.KeyboardLayoutStatusBar.Content = this.BindingFile.KeyboardLayout;
            this.txtFilter.Text = placeHolderText;
        }

        /// <summary>
        /// Texts the filter key up.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.Input.KeyEventArgs"/> instance containing the event data.</param>
        private void TxtFilterKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var p = new Predicate<object>(item =>
            {
                var binding = (KeyBindingView)item;
                return binding.Action.Contains(this.txtFilter.Text, StringComparison.InvariantCultureIgnoreCase) 
                || binding.PrimaryKey.Contains(this.txtFilter.Text, StringComparison.InvariantCultureIgnoreCase)
                || (binding.SecondaryKey?.Contains(this.txtFilter.Text, StringComparison.InvariantCultureIgnoreCase) ?? false)
                || binding.Area.Contains(this.txtFilter.Text, StringComparison.InvariantCultureIgnoreCase)
                || binding.Category.Contains(this.txtFilter.Text, StringComparison.InvariantCultureIgnoreCase)
                || (binding.PrimaryDevice?.Contains(this.txtFilter.Text, StringComparison.InvariantCultureIgnoreCase) ?? false);
            });

            if(string.IsNullOrWhiteSpace(this.txtFilter.Text))
            {
                this.KeyBindings.Filter = null;
            }
            else
            {
                this.KeyBindings.Filter = p;
            }
        }

        /// <summary>
        /// Texts the filter got focus.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void TxtFilterGotFocus(object sender, RoutedEventArgs e)
        {
            if (this.txtFilter.Text == placeHolderText)
            {
                this.txtFilter.Text = string.Empty;
            }

        }

        /// <summary>
        /// Texts the filter lost focus.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void TxtFilterLostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.txtFilter.Text))
            {
                this.txtFilter.Text = placeHolderText;
            }
        }

        /// <summary>
        /// Toggles the column visibility.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void ToggleColumnVisibility(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem != null && menuItem.Tag != null)
            {
                string columnTag = menuItem.Tag.ToString();

                var column = this.KeyBindingDataGrid.Columns.FirstOrDefault(c => c.Header.ToString() == columnTag);
                if (column != null)
                {
                    column.Visibility = menuItem.IsChecked ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Menus the item click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void MenuItemClick(object sender, RoutedEventArgs e)
        {
            var dialog = new AboutWindow();
            dialog.Owner = this;
            dialog.ShowDialog();
        }
    }
}
