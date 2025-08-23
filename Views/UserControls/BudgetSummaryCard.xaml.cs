using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BudgetManagement.Views.UserControls
{
    /// <summary>
    /// Reusable budget summary card control designed for senior-friendly interface
    /// </summary>
    public partial class BudgetSummaryCard : UserControl
    {
        public BudgetSummaryCard()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        // Header property
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(string), typeof(BudgetSummaryCard), new PropertyMetadata(string.Empty));

        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        // Value property
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(decimal), typeof(BudgetSummaryCard), new PropertyMetadata(0m));

        public decimal Value
        {
            get { return (decimal)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // Icon property
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(string), typeof(BudgetSummaryCard), new PropertyMetadata(string.Empty));

        public string Icon
        {
            get { return (string)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        // Subtitle property
        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register("Subtitle", typeof(string), typeof(BudgetSummaryCard), new PropertyMetadata(string.Empty));

        public string Subtitle
        {
            get { return (string)GetValue(SubtitleProperty); }
            set { SetValue(SubtitleProperty, value); }
        }

        // Change text property
        public static readonly DependencyProperty ChangeTextProperty =
            DependencyProperty.Register("ChangeText", typeof(string), typeof(BudgetSummaryCard), new PropertyMetadata(string.Empty));

        public string ChangeText
        {
            get { return (string)GetValue(ChangeTextProperty); }
            set { SetValue(ChangeTextProperty, value); }
        }

        // Change value property
        public static readonly DependencyProperty ChangeValueProperty =
            DependencyProperty.Register("ChangeValue", typeof(decimal), typeof(BudgetSummaryCard), new PropertyMetadata(0m));

        public decimal ChangeValue
        {
            get { return (decimal)GetValue(ChangeValueProperty); }
            set { SetValue(ChangeValueProperty, value); }
        }

        // Action button text property
        public static readonly DependencyProperty ActionButtonTextProperty =
            DependencyProperty.Register("ActionButtonText", typeof(string), typeof(BudgetSummaryCard), new PropertyMetadata(string.Empty));

        public string ActionButtonText
        {
            get { return (string)GetValue(ActionButtonTextProperty); }
            set { SetValue(ActionButtonTextProperty, value); }
        }

        // Action command property
        public static readonly DependencyProperty ActionCommandProperty =
            DependencyProperty.Register("ActionCommand", typeof(ICommand), typeof(BudgetSummaryCard), new PropertyMetadata(null));

        public ICommand ActionCommand
        {
            get { return (ICommand)GetValue(ActionCommandProperty); }
            set { SetValue(ActionCommandProperty, value); }
        }

        #endregion
    }
}