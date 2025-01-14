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
    using System.Text.RegularExpressions;
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

            if (ApplicationSettings.Default.RecentBindings.Count > 0)
            {
                this.RebuildRecentsMenu();
            }

            this.LoadVisibleColumns();
        }

        /// <summary>
        /// Rebuilds the recentlz used files menu.
        /// </summary>
        private void RebuildRecentsMenu()
        {
            this.RecentBindingsMenu.Items.Clear();
            // Checking again here since the menu might have been cleared by the user
            if (ApplicationSettings.Default.RecentBindings.Count > 0)
            {
                foreach (var recentBinding in ApplicationSettings.Default.RecentBindings)
                {
                    var menuItem = new MenuItem();
                    menuItem.Header = recentBinding;
                    menuItem.Click += this.FileRecentBindingsMenuSelected;
                    this.RecentBindingsMenu.Items.Add(menuItem);
                }

                var separator = new Separator();
                this.RecentBindingsMenu.Items.Add(separator);
                var menuItemClear = new MenuItem();
                menuItemClear.Header = "Clear Recent Files";
                menuItemClear.Click += this.ClearRecentFileList;
                this.RecentBindingsMenu.Items.Add(menuItemClear);
            }
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
        /// Open the file that was selected from the "Recents" list
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void FileRecentBindingsMenuSelected(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                string fileName = menuItem.Header.ToString();
                if (File.Exists(fileName))
                {
                    this.BindingFile = BindingFile.Open(fileName);
                    this.ProcessBindingFile();
                }
                else
                {
                    MessageBox.Show($"File not found: {fileName}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

            if (!ApplicationSettings.Default.RecentBindings.Contains(this.BindingFile.FileName))
            {
                ApplicationSettings.Default.RecentBindings.Add(this.BindingFile.FileName);
                ApplicationSettings.Default.Save();
                this.RebuildRecentsMenu();
            }
        }

        /// <summary>
        /// Clears the recent file list after asking for confirmation.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void ClearRecentFileList(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear the recent file list?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                ApplicationSettings.Default.RecentBindings.Clear();
                ApplicationSettings.Default.Save();
                this.RecentBindingsMenu.Items.Clear();
            }
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
                    this.SaveVisibleColumns();
                }
            }
        }

        /// <summary>
        /// Loads which columns are visible from persistent settings.
        /// </summary>
        private void LoadVisibleColumns()
        {
            if (ApplicationSettings.Default.VisibleColumns != null)
            {
                foreach (var column in this.KeyBindingDataGrid.Columns)
                {
                    string name = column.Header.ToString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        // Use regular expression to replace all non-alphanumeric characters with an empty string
                        name = Regex.Replace(name, @"[^a-zA-Z0-9]", string.Empty);
                    }
                    var menuItem = (MenuItem)this.FindName(name);
                    if (ApplicationSettings.Default.VisibleColumns.Contains("All") || ApplicationSettings.Default.VisibleColumns.Contains(column.Header.ToString()))
                    {
                        column.Visibility = Visibility.Visible;
                        if (menuItem != null)
                        {
                            menuItem.IsChecked = true;
                        }
                    }
                    else
                    {
                        column.Visibility = Visibility.Collapsed;
                        if (menuItem != null)
                        {
                            menuItem.IsChecked = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Save set of visible columns to persistent settings.
        /// </summary>
        private void SaveVisibleColumns()
        {
            var visibleColumns = new System.Collections.Specialized.StringCollection();
            foreach (var column in this.KeyBindingDataGrid.Columns)
            {
                if (column.Visibility == Visibility.Visible)
                {
                    visibleColumns.Add(column.Header.ToString());
                }
            }
            ApplicationSettings.Default.VisibleColumns = visibleColumns;
            ApplicationSettings.Default.Save();
        }

        /// <summary>
        /// Handles the Sorting event of the KeyBindingDataGrid control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeyBindingDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            // Prevent the default sorting
            e.Handled = true;

            var collectionView = CollectionViewSource.GetDefaultView(KeyBindingDataGrid.ItemsSource);
            var sortDescriptions = collectionView.SortDescriptions;

            // Check if the column is already sorted
            var existingSortDescription = sortDescriptions
                .FirstOrDefault(sd => sd.PropertyName == e.Column.SortMemberPath);

            ListSortDirection newDirection;

            if (existingSortDescription.PropertyName != null)
            {
                // Toggle sort direction if the column is already sorted
                newDirection = existingSortDescription.Direction == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;

                // Remove the existing sort description
                sortDescriptions.Remove(existingSortDescription);
            }
            else
            {
                // Default to ascending if the column isn't sorted yet
                newDirection = ListSortDirection.Ascending;
            }

            // Add the new sort description to the collection
            sortDescriptions.Add(new SortDescription(e.Column.SortMemberPath, newDirection));

            // Update the column's sort direction indicator
            e.Column.SortDirection = newDirection;

            // Refresh the view
            collectionView.Refresh();
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
