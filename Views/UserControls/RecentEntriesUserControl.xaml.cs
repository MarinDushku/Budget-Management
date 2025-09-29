using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BudgetManagement.Models;

namespace BudgetManagement.Views.UserControls
{
    /// <summary>
    /// Simple data structure for grouped entries by date
    /// </summary>
    public class DateGroup
    {
        public DateTime Date { get; set; }
        public string DateLabel { get; set; }
        public bool IsRecentDate { get; set; }
        public List<Income> IncomeEntries { get; set; } = new List<Income>();
        public List<SpendingWithCategory> SpendingEntries { get; set; } = new List<SpendingWithCategory>();
        public int TotalEntries => IncomeEntries.Count + SpendingEntries.Count;
        public decimal TotalIncome => IncomeEntries.Sum(i => i.Amount);
        public decimal TotalSpending => SpendingEntries.Sum(s => s.Amount);
        
        public string SummaryText
        {
            get
            {
                var entryText = TotalEntries == 1 ? 
                    Application.Current.FindResource("Entry")?.ToString() ?? "entry" :
                    Application.Current.FindResource("Entries")?.ToString() ?? "entries";
                var parts = new List<string> { $"{TotalEntries} {entryText}" };
                if (TotalIncome > 0) parts.Add($"+{TotalIncome:C}");
                if (TotalSpending > 0) parts.Add($"-{TotalSpending:C}");
                return string.Join(" • ", parts);
            }
        }
    }

    /// <summary>
    /// Recent entries user control for displaying latest income and spending activities
    /// </summary>
    public partial class RecentEntriesUserControl : UserControl, INotifyPropertyChanged
    {
        private ObservableCollection<DateGroup> _groupedEntries = new ObservableCollection<DateGroup>();
        private string _debugInfo = "No debug info yet";

        /// <summary>
        /// Debug information for UI display
        /// </summary>
        public string DebugInfo
        {
            get { return _debugInfo; }
            set
            {
                _debugInfo = value;
                OnPropertyChanged(nameof(DebugInfo));
            }
        }

        public RecentEntriesUserControl()
        {
            InitializeComponent();
            Loaded += RecentEntriesUserControl_Loaded;
        }

        private void RecentEntriesUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Update grouping when the control is loaded (in case data was already set)
            UpdateGroupedEntries();
        }

        #region Dependency Properties

        // Recent income entries property
        public static readonly DependencyProperty RecentIncomeEntriesProperty =
            DependencyProperty.Register("RecentIncomeEntries", typeof(ObservableCollection<Income>), typeof(RecentEntriesUserControl), 
                new PropertyMetadata(new ObservableCollection<Income>(), OnEntriesChanged));

        public ObservableCollection<Income> RecentIncomeEntries
        {
            get { return (ObservableCollection<Income>)GetValue(RecentIncomeEntriesProperty); }
            set { SetValue(RecentIncomeEntriesProperty, value); }
        }

        // Recent spending entries property
        public static readonly DependencyProperty RecentSpendingEntriesProperty =
            DependencyProperty.Register("RecentSpendingEntries", typeof(ObservableCollection<SpendingWithCategory>), typeof(RecentEntriesUserControl), 
                new PropertyMetadata(new ObservableCollection<SpendingWithCategory>(), OnEntriesChanged));

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

        #region Computed Properties

        /// <summary>
        /// Grouped entries by date for timeline-accordion display
        /// </summary>
        public ObservableCollection<DateGroup> GroupedEntries
        {
            get { return _groupedEntries; }
            set
            {
                _groupedEntries = value;
                OnPropertyChanged(nameof(GroupedEntries));
            }
        }

        #endregion

        #region Property Change Handling

        /// <summary>
        /// Called when income or spending entries change
        /// </summary>
        private static void OnEntriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RecentEntriesUserControl control)
            {
                // Unsubscribe from old collections
                if (e.OldValue is ObservableCollection<Income> oldIncomeCollection)
                {
                    oldIncomeCollection.CollectionChanged -= control.OnIncomeCollectionChanged;
                }
                if (e.OldValue is ObservableCollection<SpendingWithCategory> oldSpendingCollection)
                {
                    oldSpendingCollection.CollectionChanged -= control.OnSpendingCollectionChanged;
                }

                // Subscribe to new collections
                if (e.NewValue is ObservableCollection<Income> newIncomeCollection)
                {
                    newIncomeCollection.CollectionChanged += control.OnIncomeCollectionChanged;
                }
                if (e.NewValue is ObservableCollection<SpendingWithCategory> newSpendingCollection)
                {
                    newSpendingCollection.CollectionChanged += control.OnSpendingCollectionChanged;
                }

                control.UpdateGroupedEntries();
                control.UpdateEmptyState();
            }
        }

        /// <summary>
        /// Updates the grouped entries collection
        /// </summary>
        private void UpdateGroupedEntries()
        {
            var allEntries = new List<(DateTime date, object entry, bool isIncome)>();
            var debugLines = new List<string>();

            // Add income entries
            if (RecentIncomeEntries != null)
            {
                debugLines.Add($"Income: {RecentIncomeEntries.Count} entries");
                foreach (var income in RecentIncomeEntries)
                {
                    allEntries.Add((income.Date, income, true));
                }
            }
            else
            {
                debugLines.Add("Income: NULL");
            }

            // Add spending entries
            if (RecentSpendingEntries != null)
            {
                debugLines.Add($"Spending: {RecentSpendingEntries.Count} entries");
                foreach (var spending in RecentSpendingEntries)
                {
                    allEntries.Add((spending.Date, spending, false));
                }
            }
            else
            {
                debugLines.Add("Spending: NULL");
            }

            debugLines.Add($"Total: {allEntries.Count} entries");

            // Group by date and create DateGroup objects
            var groups = allEntries
                .GroupBy(e => e.date.Date)
                .OrderByDescending(g => g.Key)
                .Select((group, index) => new DateGroup
                {
                    Date = group.Key,
                    DateLabel = GetDateLabel(group.Key),
                    IsRecentDate = index == 0, // Auto-expand most recent date
                    IncomeEntries = group.Where(e => e.isIncome).Select(e => (Income)e.entry).ToList(),
                    SpendingEntries = group.Where(e => !e.isIncome).Select(e => (SpendingWithCategory)e.entry).ToList()
                })
                .ToList();

            debugLines.Add($"Groups: {groups.Count} created");
            DebugInfo = string.Join(" | ", debugLines);

            GroupedEntries = new ObservableCollection<DateGroup>(groups);
        }

        /// <summary>
        /// Gets user-friendly date label
        /// </summary>
        private string GetDateLabel(DateTime date)
        {
            var today = DateTime.Today;
            var daysDiff = (today - date.Date).Days;

            return daysDiff switch
            {
                0 => Application.Current.FindResource("Today")?.ToString() ?? "Today",
                1 => Application.Current.FindResource("Yesterday")?.ToString() ?? "Yesterday",
                7 => Application.Current.FindResource("OneWeekAgo")?.ToString() ?? "1 week ago",
                _ when daysDiff >= 2 && daysDiff <= 6 => 
                    string.Format(Application.Current.FindResource("DaysAgo")?.ToString() ?? "{0} days ago", daysDiff),
                _ when daysDiff < 14 => 
                    string.Format(Application.Current.FindResource("DaysAgo")?.ToString() ?? "{0} days ago", daysDiff),
                _ when daysDiff < 30 => 
                    string.Format(Application.Current.FindResource("WeeksAgo")?.ToString() ?? "{0} weeks ago", daysDiff / 7),
                _ => date.ToString("MMM dd, yyyy")
            };
        }

        /// <summary>
        /// Handles changes to the income collection
        /// </summary>
        private void OnIncomeCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateGroupedEntries();
            UpdateEmptyState();
        }

        /// <summary>
        /// Handles changes to the spending collection
        /// </summary>
        private void OnSpendingCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateGroupedEntries();
            UpdateEmptyState();
        }

        #endregion

        /// <summary>
        /// Updates the HasNoEntries property based on the current collections
        /// </summary>
        public void UpdateEmptyState()
        {
            HasNoEntries = (RecentIncomeEntries?.Count ?? 0) == 0 && (RecentSpendingEntries?.Count ?? 0) == 0;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}