using al_ameer.Services;
using System.Windows;
using System.Windows.Controls;

namespace al_ameer.Features.Employees;

public partial class EmployeesPage : Page
{
    private readonly EmployeeLedgerService _ledger = new();
    private IReadOnlyList<EmployeeRecord> _employees = [];
    private int? _selectedEmployeeId;

    public EmployeesPage()
    {
        InitializeComponent();
        dpEntryDate.SelectedDate = DateTime.Today;
        LoadEmployees();
    }

    private void LoadEmployees()
    {
        try
        {
            _employees = _ledger.GetEmployees();
            FilterEmployees();
            if (_selectedEmployeeId is int id)
                dgEmployees.SelectedItem = _employees.FirstOrDefault(e => e.EmployeeId == id);
        }
        catch (Exception ex) { MessageBox.Show("Employees could not load: " + ex.Message); }
    }

    private void FilterEmployees()
    {
        string search = txtSearch.Text.Trim();
        dgEmployees.ItemsSource = _employees.Where(e => e.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            (e.JobTitle?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
    }

    private void Search_Changed(object sender, TextChangedEventArgs e)
    {
        if (dgEmployees != null) FilterEmployees();
    }

    private void EmployeeSelection_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (dgEmployees.SelectedItem is not EmployeeRecord employee) return;
        _selectedEmployeeId = employee.EmployeeId;
        txtEmployeeName.Text = employee.FullName;
        txtEmployeeDetails.Text = AppSettings.Current.Language == "ar"
            ? $"{employee.JobTitle ?? "موظف"} · {employee.Phone ?? "بلا هاتف"} · شهرياً {employee.MonthlySalary:N0} ل.ل. · {(employee.IsActive ? "نشط" : "غير نشط")}"
            : $"{employee.JobTitle ?? "Employee"} · {employee.Phone ?? "No phone"} · Monthly {employee.MonthlySalary:N0} LBP · {(employee.IsActive ? "Active" : "Inactive")}";
        txtSalaryBalance.Text = AppSettings.Current.Language == "ar"
            ? $"الراتب المستحق: {employee.SalaryBalance:N2} ل.ل." : $"Salary due: {employee.SalaryBalance:N2} LBP";
        try { dgEntries.ItemsSource = _ledger.GetEntries(employee.EmployeeId); }
        catch (Exception ex) { MessageBox.Show("Salary history could not load: " + ex.Message); }
    }

    private void AddEmployee_Click(object sender, RoutedEventArgs e)
    {
        if (new EmployeeEditorWindow { Owner = Window.GetWindow(this) }.ShowDialog() == true) LoadEmployees();
    }

    private void EditEmployee_Click(object sender, RoutedEventArgs e)
    {
        if (dgEmployees.SelectedItem is not EmployeeRecord employee) { MessageBox.Show("Select an employee first."); return; }
        if (new EmployeeEditorWindow(employee) { Owner = Window.GetWindow(this) }.ShowDialog() == true) LoadEmployees();
    }

    private void PostEntry_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedEmployeeId is not int employeeId) { MessageBox.Show("Select an employee first."); return; }
        if (!InputParser.TryNonNegativeMoney(txtAmount.Text, out decimal amount) || amount <= 0 ||
            dpEntryDate.SelectedDate is not DateTime date)
        {
            MessageBox.Show("Enter an amount greater than zero and a date.");
            return;
        }
        string type = (cbEntryType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Due";
        try
        {
            _ledger.AddEntry(employeeId, date, type, amount, txtNotes.Text);
            txtAmount.Clear();
            txtNotes.Clear();
            LoadEmployees();
        }
        catch (Exception ex) { MessageBox.Show("Salary entry was not recorded: " + ex.Message); }
    }

    private void ReverseEntry_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedEmployeeId is not int employeeId || dgEntries.SelectedItem is not SalaryEntry entry)
        { MessageBox.Show("Select a salary entry first."); return; }
        if (entry.ReversedAt is not null) { MessageBox.Show("Entry is already reversed."); return; }
        string? reason = ReversalPrompt.Ask(Window.GetWindow(this), "Reverse salary entry");
        if (reason is null) return;
        if (MessageBox.Show("Reverse this salary entry?", "Confirm", MessageBoxButton.YesNo,
            MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { _ledger.ReverseEntry(employeeId, entry.SalaryEntryId, reason); LoadEmployees(); }
        catch (Exception ex) { MessageBox.Show("Reversal failed: " + ex.Message); }
    }
}
