// Selectable Category Model for UI Selection
// File: Models/SelectableCategory.cs

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BudgetManagement.Models
{
    /// <summary>
    /// Wrapper class for Category that adds selection state for UI binding
    /// Used in search and filter scenarios where categories need to be selectable
    /// </summary>
    public class SelectableCategory : INotifyPropertyChanged
    {
        private bool _isSelected;

        public SelectableCategory(Category category, bool isSelected = false)
        {
            Category = category ?? throw new ArgumentNullException(nameof(category));
            _isSelected = isSelected;
        }

        /// <summary>
        /// The underlying category data
        /// </summary>
        public Category Category { get; }

        /// <summary>
        /// Convenience properties from the underlying category
        /// </summary>
        public int Id => Category.Id;
        public string Name => Category.Name;
        public string Icon => Category.Icon;
        public int DisplayOrder => Category.DisplayOrder;
        public bool IsActive => Category.IsActive;

        /// <summary>
        /// Selection state for UI binding
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override bool Equals(object? obj)
        {
            if (obj is SelectableCategory other)
                return Category.Id == other.Category.Id;
            if (obj is Category category)
                return Category.Id == category.Id;
            return false;
        }

        public override int GetHashCode()
        {
            return Category.Id.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Name} (Selected: {IsSelected})";
        }
    }
}