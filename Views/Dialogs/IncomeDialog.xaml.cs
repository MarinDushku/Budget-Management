using System;
using System.Windows;
using BudgetManagement.Models;

namespace BudgetManagement.Views.Dialogs
{
    public partial class IncomeDialog : Window
    {
        public Income Income { get; private set; }

        public IncomeDialog()
        {
            InitializeComponent();
            DatePicker.SelectedDate = DateTime.Today;
        }

        public IncomeDialog(Income income) : this()
        {
            Income = income;
            if (income != null)
            {
                DatePicker.SelectedDate = income.Date;
                DescriptionTextBox.Text = income.Description;
                AmountTextBox.Text = income.Amount.ToString("F2");
                Title = "Edit Income Entry";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Simple validation
            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                MessageBox.Show("Please enter a description.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                DescriptionTextBox.Focus();
                return;
            }

            if (!decimal.TryParse(AmountTextBox.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Please enter a valid amount greater than 0.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                AmountTextBox.Focus();
                return;
            }

            if (DatePicker.SelectedDate == null)
            {
                MessageBox.Show("Please select a date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                DatePicker.Focus();
                return;
            }

            // Create or update income
            if (Income == null)
            {
                Income = new Income();
            }

            Income.Date = DatePicker.SelectedDate.Value;
            Income.Description = DescriptionTextBox.Text.Trim();
            Income.Amount = amount;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}