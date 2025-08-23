using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BudgetManagement.Converters
{
    /// <summary>
    /// Converts boolean values to Visibility enum values
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                bool invert = parameter?.ToString()?.ToLower() == "invert";
                return (boolValue ^ invert) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool invert = parameter?.ToString()?.ToLower() == "invert";
                return (visibility == Visibility.Visible) ^ invert;
            }
            return false;
        }
    }

    /// <summary>
    /// Converts decimal numbers to currency format with senior-friendly formatting
    /// </summary>
    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                // Use larger, clearer currency formatting for seniors
                return decimalValue.ToString("C2", CultureInfo.CurrentCulture);
            }
            if (value is double doubleValue)
            {
                return doubleValue.ToString("C2", CultureInfo.CurrentCulture);
            }
            if (value is float floatValue)
            {
                return floatValue.ToString("C2", CultureInfo.CurrentCulture);
            }
            return "$0.00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && decimal.TryParse(stringValue.Replace("$", "").Replace(",", ""), out decimal result))
            {
                return result;
            }
            return 0m;
        }
    }

    /// <summary>
    /// Converts dates to senior-friendly format
    /// </summary>
    public class DateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateValue)
            {
                string format = parameter?.ToString() ?? "long";
                return format.ToLower() switch
                {
                    "short" => dateValue.ToString("MM/dd/yyyy"),
                    "medium" => dateValue.ToString("MMM dd, yyyy"),
                    "long" => dateValue.ToString("MMMM dd, yyyy"),
                    "dayofweek" => dateValue.ToString("dddd, MMMM dd, yyyy"),
                    _ => dateValue.ToString("MM/dd/yyyy")
                };
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && DateTime.TryParse(stringValue, out DateTime result))
            {
                return result;
            }
            return DateTime.Today;
        }
    }

    /// <summary>
    /// Converts numbers to positive/negative/zero for styling purposes
    /// </summary>
    public class NumberToSignConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                return decimalValue > 0 ? "Positive" : decimalValue < 0 ? "Negative" : "Zero";
            }
            if (value is double doubleValue)
            {
                return doubleValue > 0 ? "Positive" : doubleValue < 0 ? "Negative" : "Zero";
            }
            if (value is float floatValue)
            {
                return floatValue > 0 ? "Positive" : floatValue < 0 ? "Negative" : "Zero";
            }
            if (value is int intValue)
            {
                return intValue > 0 ? "Positive" : intValue < 0 ? "Negative" : "Zero";
            }
            return "Zero";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Adds a plus sign to positive numbers for better visual distinction
    /// </summary>
    public class NumberToPlusMinusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                if (decimalValue > 0)
                    return "+" + decimalValue.ToString("C2");
                else if (decimalValue < 0)
                    return decimalValue.ToString("C2");
                else
                    return decimalValue.ToString("C2");
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts collection count to user-friendly text
    /// </summary>
    public class CountToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                string itemType = parameter?.ToString() ?? "item";
                return count switch
                {
                    0 => $"No {itemType}s",
                    1 => $"1 {itemType}",
                    _ => $"{count} {itemType}s"
                };
            }
            return "0 items";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Truncates long text with ellipsis for better display in grids
    /// </summary>
    public class TextTruncateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text && !string.IsNullOrEmpty(text))
            {
                int maxLength = 50; // Default length
                if (parameter is string lengthParam && int.TryParse(lengthParam, out int customLength))
                {
                    maxLength = customLength;
                }

                if (text.Length > maxLength)
                {
                    return text.Substring(0, maxLength - 3) + "...";
                }
                return text;
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts budget remaining to color indication
    /// </summary>
    public class BudgetStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal remaining)
            {
                if (remaining > 0)
                    return "Green"; // Positive budget
                else if (remaining < 0)
                    return "Red"; // Over budget
                else
                    return "Blue"; // Exactly on budget
            }
            return "Blue";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts percentage to user-friendly format
    /// </summary>
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double percentage)
            {
                return (percentage * 100).ToString("F1") + "%";
            }
            if (value is decimal decimalPercentage)
            {
                return (decimalPercentage * 100).ToString("F1") + "%";
            }
            return "0.0%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                string cleanValue = stringValue.Replace("%", "").Trim();
                if (double.TryParse(cleanValue, out double result))
                {
                    return result / 100.0;
                }
            }
            return 0.0;
        }
    }

    /// <summary>
    /// Multi-value converter for enabling buttons based on multiple conditions
    /// </summary>
    public class MultiBooleanToEnabledConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // All values must be true for the control to be enabled
            foreach (var value in values)
            {
                if (value is bool boolValue && !boolValue)
                {
                    return false;
                }
            }
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts null or empty values to visibility for conditional display
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = parameter?.ToString()?.ToLower() == "invert";
            bool hasValue = value != null && !string.IsNullOrWhiteSpace(value.ToString());
            
            return (hasValue ^ invert) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}