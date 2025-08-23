using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BudgetManagement.Models;

namespace BudgetManagement.Views.UserControls
{
    /// <summary>
    /// Recent entries user control for displaying latest income and spending activities
    /// </summary>
    public partial class RecentEntriesUserControl : UserControl
    {
        public RecentEntriesUserControl()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        // Recent income entries property
        public static readonly DependencyProperty RecentIncomeEntriesProperty =
            DependencyProperty.Register("RecentIncomeEntries", typeof(ObservableCollection<Income>), typeof(RecentEntriesUserControl), new PropertyMetadata(new ObservableCollection<Income>()));

        public ObservableCollection<Income> RecentIncomeEntries
        {
            get { return (ObservableCollection<Income>)GetValue(RecentIncomeEntriesProperty); }
            set { SetValue(RecentIncomeEntriesProperty, value); }
        }

        // Recent spending entries property
        public static readonly DependencyProperty RecentSpendingEntriesProperty =
            DependencyProperty.Register("RecentSpendingEntries", typeof(ObservableCollection<SpendingWithCategory>), typeof(RecentEntriesUserControl), new PropertyMetadata(new ObservableCollection<SpendingWithCategory>()));

        public ObservableCollection<SpendingWithCategory> RecentSpendingEntries
        {
            get { return (ObservableCollection<SpendingWithCategory>)GetValue(RecentSpendingEntriesProperty); }
            set { SetValue(RecentSpendingEntriesProperty, value); }
        }

        // Has no entries property for empty state
        public static readonly DependencyProperty HasNoEntriesProperty =
            DependencyProperty.Register("HasNoEntries", typeof(bool), typeof(RecentEntriesUserControl), new PropertyMetadata(true));

        public bool HasNoEntries
        {
            get { return (bool)GetValue(HasNoEntriesProperty); }
            set { SetValue(HasNoEntriesProperty, value); }
        }

        #endregion

        /// <summary>
        /// Updates the HasNoEntries property based on the current collections
        /// </summary>
        public void UpdateEmptyState()
        {
            HasNoEntries = (RecentIncomeEntries?.Count ?? 0) == 0 && (RecentSpendingEntries?.Count ?? 0) == 0;
        }
    }
}