using System;
using System.Windows;
using System.Windows.Input;
using al_ameer.Features.Login;
using al_ameer.Features.Inventory;
using al_ameer.Features.Dashboard;
using al_ameer.Features.Sales;
using al_ameer.Features.Customers;
using al_ameer.Features.Suppliers;
using al_ameer.Features.Expenses;
using al_ameer.Features.Reports; // Make sure this namespace exists
using al_ameer.Features.Settings; // Make sure this namespace exists

namespace al_ameer.Features.Home
{
    public partial class HomeWindow : Window
    {
        // Constructor for Designer
        public HomeWindow()
        {
            InitializeComponent();
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight; // Prevents covering taskbar

            // Initial Page
            MainFrame.Navigate(new DashboardPage());
        }

        // Constructor from Login
        public HomeWindow(string adminName) : this()
        {
            txtAdminName.Text = adminName;
        }

        // Window Dragging
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        #region Navigation Logic

        private void NavDashboard_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Control Center";
            MainFrame.Navigate(new DashboardPage());
        }

        private void NavInventory_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Inventory Management";
            MainFrame.Navigate(new InventoryPage());
        }

        private void NavSales_Click(object sender, RoutedEventArgs e) 
        {
            txtPageTitle.Text = "Sales & POS";
            MainFrame.Navigate(new SalesPage());

        }
        private void NavCustomers_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Customer Registry";
            MainFrame.Navigate(new CustomersPage());
        }
        private void NavSuppliers_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Supplier Directory";
            MainFrame.Navigate(new SuppliersPage());
        }
        private void NavExpenses_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Expenses Orders";
            MainFrame.Navigate(new ExpensesPage());
        }
        private void NavReports_Click(object sender, RoutedEventArgs e) 
        {
            txtPageTitle.Text = "Financial Analytics";
            MainFrame.Navigate(new ReportsPage());
        }

        private void NavSettings_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "System Configuration";
            MainFrame.Navigate(new SettingsPage());
        }

        #endregion

        #region Window Controls

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
                MainBorder.CornerRadius = new CornerRadius(0); // Remove rounded corners when maximized
                btnMaximize.Content = "❐";
            }
            else
            {
                this.WindowState = WindowState.Normal;
                MainBorder.CornerRadius = new CornerRadius(30); // Restore rounded corners
                btnMaximize.Content = "▢";
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to log out?", "Logout",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                LoginWindow login = new LoginWindow();
                login.Show();
                this.Close();
            }
        }

        #endregion

    }
}