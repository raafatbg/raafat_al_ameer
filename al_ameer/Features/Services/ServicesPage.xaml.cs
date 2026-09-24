using al_ameer.Services;
using System.Windows;
using System.Windows.Controls;

namespace al_ameer.Features.Services;

public partial class ServicesPage : Page
{
    private readonly ServicesCatalogService _services = new();
    private int? _editingId;
    private bool _loading;

    public ServicesPage()
    {
        InitializeComponent();
        LoadServices();
    }

    private void LoadServices()
    {
        try
        {
            _loading = true;
            var items = _services.GetAll();
            dgServices.ItemsSource = items;
            dgServices.SelectedItem = items.FirstOrDefault(x => x.ServiceId == _editingId);
            _loading = false;
        }
        catch (Exception ex) { _loading = false; MessageBox.Show("Services could not load: " + ex.Message); }
    }

    private void ServiceSelection_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || dgServices.SelectedItem is not ServiceCatalogItem item) return;
        _editingId = item.ServiceId;
        txtEditorTitle.Text = "Edit service";
        txtName.Text = item.ServiceName;
        txtPrice.Text = item.Price.ToString("0.##");
        txtDescription.Text = item.Description ?? "";
        chkActive.IsChecked = item.IsActive;
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        _editingId = null;
        dgServices.SelectedItem = null;
        txtEditorTitle.Text = "Add service";
        txtName.Clear(); txtPrice.Clear(); txtDescription.Clear();
        chkActive.IsChecked = true;
        txtName.Focus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!InputParser.TryNonNegativeMoney(txtPrice.Text, out decimal price))
        { MessageBox.Show("Enter a valid non-negative price in LBP."); return; }
        try
        {
            _editingId = _services.Save(_editingId, txtName.Text, txtDescription.Text, price, chkActive.IsChecked == true);
            LoadServices();
        }
        catch (Exception ex) { MessageBox.Show("Service could not be saved: " + ex.Message); }
    }
}
