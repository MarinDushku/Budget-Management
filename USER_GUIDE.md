# Budget Management - User Guide

## Welcome to Your Personal Budget Manager!

This application is designed to help you easily track your income and spending. Everything is stored safely on your computer - no internet connection required!

## Getting Started

When you first open the application, you'll see:
- Large, easy-to-read buttons and text
- Three main sections: Income, Family Spending, Personal Spending, and Marini Spending
- Summary boxes showing your totals at the top

## Main Features

### 📊 **Budget Summary (Top Section)**
- **Total Income**: Shows all money you've received
- **Total Spending**: Shows all money you've spent
- **Family/Personal/Marini Spending**: Shows spending by category
- **Remaining Budget**: Shows how much money you have left (Green = good, Red = over budget)

### 💰 **Adding Income**
1. Click the large **"+ Add Income"** button
2. Select the date (today's date is already filled in)
3. Type a description (like "Social Security" or "Pension")
4. Enter the amount (just numbers, like 1500.50)
5. Click **"Save Income"**

### 💸 **Adding Spending**
1. Click the large **"+ Add Spending"** button
2. Select the date
3. Choose a category:
   - **Family**: Groceries, utilities, house expenses
   - **Personal**: Clothing, personal care, hobbies
   - **Marini**: Special Marini-related expenses
4. Type a description (like "Grocery shopping" or "Electric bill")
5. Enter the amount
6. Click **"Save Spending"**

### 📅 **Viewing Different Time Periods**
- Use the date pickers to choose start and end dates
- Click **"This Month"** to see current month
- Click **"This Year"** to see current year

### ✏️ **Editing or Deleting Entries**
- In the data tables, each entry has **"Edit"** and **"Delete"** buttons
- Edit: Change any information about an entry
- Delete: Remove an entry completely (asks for confirmation)

## Helpful Tips

### 💡 **Navigation Tips**
- Press **F5** to refresh the data
- Press **Ctrl+N** to add new income
- Press **Ctrl+E** to add new spending
- Use **Tab** key to move between fields in forms
- Press **Enter** to move to the next field
- Press **Escape** to cancel and close dialogs

### 🔄 **Data Management**
- Your data is automatically saved to your computer
- The app creates daily backups in the "Backups" folder
- Click **"🔄 Refresh"** if data seems outdated
- Click **"📊 Export Data"** to save your information to a file

### 🎯 **Best Practices**
- Use clear descriptions like "Walmart groceries" instead of just "store"
- Check your entries regularly to catch any mistakes
- Use the category system consistently
- Review your budget summary weekly

## Common Questions

**Q: Where is my data stored?**
A: All data is stored safely on your computer in the "AppData/BudgetManagement" folder. No internet required!

**Q: What if I make a mistake?**
A: You can edit any entry by clicking the "Edit" button next to it, or delete it with the "Delete" button.

**Q: Can I print my budget information?**
A: Use the "Export Data" button to save your information to a file, then open it in a program like Excel to print.

**Q: What if the text is too small?**
A: The application is designed with large, clear text. If needed, you can use Windows' built-in zoom features.

## Getting Help

If you need assistance:
1. Look for the blue information boxes with 💡 tips in the forms
2. Hover your mouse over buttons to see helpful tooltips
3. All required fields are marked with a * symbol

Remember: This application is designed to be simple and safe. Take your time, and don't worry about making mistakes - everything can be edited or corrected!

---

**Enjoy tracking your budget with confidence!** 🏠💰

dotnet clean
dotnet publish -c Release --self-contained true -r win-x64 -o publish
 cd publish
.\BudgetManagement.exe