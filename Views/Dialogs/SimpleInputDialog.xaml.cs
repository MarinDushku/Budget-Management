using System.Windows;

namespace BudgetManagement.Views.Dialogs
{
    public partial class SimpleInputDialog : Window
    {
        public string InputText => InputTextBox.Text;

        public SimpleInputDialog(string title, string message, string defaultValue = "")
        {
            InitializeComponent();
            
            Title = title;
            MessageTextBlock.Text = message;
            InputTextBox.Text = defaultValue;
            
            // Focus and select the text for easy editing
            Loaded += (s, e) =>
            {
                InputTextBox.Focus();
                InputTextBox.SelectAll();
            };
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
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