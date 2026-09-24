using al_ameer.Services;
using System.Windows;
using System.Windows.Input;

namespace al_ameer.Features.Employees;

public partial class EmployeeEditorWindow : Window
{
    private readonly EmployeeLedgerService _service = new();
    private readonly int? _employeeId;

    public EmployeeEditorWindow(EmployeeRecord? employee = null)
    {
        InitializeComponent();
        _employeeId = employee?.EmployeeId;
        dpHireDate.SelectedDate = employee?.HireDate ?? DateTime.Today;
        if (employee == null) return;
        txtTitle.Text = "EDIT EMPLOYEE";
        txtName.Text = employee.FullName;
        txtPhone.Text = employee.Phone ?? "";
        txtJobTitle.Text = employee.JobTitle ?? "";
        txtMonthlySalary.Text = employee.MonthlySalary.ToString("0.##");
        chkActive.IsChecked = employee.IsActive;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!InputParser.TryNonNegativeMoney(txtMonthlySalary.Text, out decimal salary) || dpHireDate.SelectedDate is not DateTime date)
        {
            MessageBox.Show("Enter a valid monthly salary and hire date.");
            return;
        }
        try
        {
            _service.SaveEmployee(_employeeId, txtName.Text, txtPhone.Text, txtJobTitle.Text, date, salary, chkActive.IsChecked == true);
            DialogResult = true;
        }
        catch (Exception ex) { MessageBox.Show("Employee could not be saved: " + ex.Message); }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    private void Window_MouseDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
}
