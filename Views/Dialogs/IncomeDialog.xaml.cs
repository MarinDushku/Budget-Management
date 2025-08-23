using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BudgetManagement.Models;

namespace BudgetManagement.Views.Dialogs
{
    public partial class IncomeDialog : Window
    {
        public Income? Income { get; private set; }
        private bool _isPlaceholderActive = true;

        public IncomeDialog()
        {
            InitializeComponent();
            DatePicker.SelectedDate = DateTime.Today;
            SetPlaceholder();
        }

        public IncomeDialog(Income income) : this()
        {
            Income = income;
            if (income != null)
            {
                DatePicker.SelectedDate = income.Date;
                DescriptionTextBox.Text = income.Description;
                SetAmountValue(income.Amount);
                Title = Application.Current.Resources["EditIncomeTitle"]?.ToString() ?? "Edit Income Entry";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Simple validation
            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                MessageBox.Show(
                    Application.Current.Resources["EnterDescription"]?.ToString() ?? "Please enter a description.",
                    Application.Current.Resources["ValidationError"]?.ToString() ?? "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DescriptionTextBox.Focus();
                return;
            }

            string amountText = _isPlaceholderActive ? "0" : AmountTextBox.Text;
            if (!decimal.TryParse(amountText, out decimal amount) || amount <= 0)
            {
                MessageBox.Show(
                    Application.Current.Resources["EnterValidAmount"]?.ToString() ?? "Please enter a valid amount greater than 0.",
                    Application.Current.Resources["ValidationError"]?.ToString() ?? "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                AmountTextBox.Focus();
                return;
            }

            if (DatePicker.SelectedDate == null)
            {
                MessageBox.Show(
                    Application.Current.Resources["SelectDate"]?.ToString() ?? "Please select a date.",
                    Application.Current.Resources["ValidationError"]?.ToString() ?? "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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

        #region Amount Input Helpers

        private void SetPlaceholder()
        {
            _isPlaceholderActive = true;
            AmountTextBox.Text = "0.00";
            AmountTextBox.Foreground = new SolidColorBrush(Color.FromArgb(128, 128, 128, 128)); // Gray placeholder
        }

        private void ClearPlaceholder()
        {
            if (_isPlaceholderActive)
            {
                _isPlaceholderActive = false;
                AmountTextBox.Text = "";
                AmountTextBox.Foreground = new SolidColorBrush(Colors.Black); // Normal text color
            }
        }

        private void SetAmountValue(decimal value)
        {
            _isPlaceholderActive = false;
            AmountTextBox.Text = value.ToString("F2");
            AmountTextBox.Foreground = new SolidColorBrush(Colors.Black);
        }

        private void AmountTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow only digits, decimal point, and ensure only one decimal point
            var regex = new Regex(@"^[0-9]*\.?[0-9]{0,2}$");
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox == null) return;
            var newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            
            // Clear placeholder on first character
            if (_isPlaceholderActive)
            {
                ClearPlaceholder();
                newText = e.Text;
            }
            
            e.Handled = !regex.IsMatch(newText) || newText.Count(c => c == '.') > 1;
        }

        private void AmountTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox == null) return;
            
            if (_isPlaceholderActive)
            {
                ClearPlaceholder();
            }
            else
            {
                // Select all text for easy replacement
                textBox.SelectAll();
            }
        }

        private void AmountTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox == null) return;
            
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                SetPlaceholder();
            }
        }

        private void AmountTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Allow backspace, delete, tab, escape, enter
            if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Tab || 
                e.Key == Key.Escape || e.Key == Key.Enter)
            {
                return;
            }

            // Allow Ctrl+A, Ctrl+C, Ctrl+V, etc.
            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None)
            {
                return;
            }

            // Block everything else except digits and decimal
            if (!((e.Key >= Key.D0 && e.Key <= Key.D9) || 
                  (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || 
                  e.Key == Key.Decimal || e.Key == Key.OemPeriod))
            {
                e.Handled = true;
            }
        }

        #endregion

        #region Quick Amount Helpers

        private void QuickAmount_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button?.Tag != null && decimal.TryParse(button.Tag.ToString(), out decimal amount))
            {
                SetAmountValue(amount);
            }
        }

        #endregion
    }
}