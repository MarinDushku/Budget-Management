// Budget Service Implementation
// File: Services/BudgetService.cs

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using BudgetManagement.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace BudgetManagement.Services
{
    /// <summary>
    /// Implementation of budget management service using SQLite
    /// </summary>
    public class BudgetService : IBudgetService
    {
        private readonly string _connectionString;
        private readonly ISettingsService _settingsService;

        public BudgetService(string databasePath, ISettingsService settingsService)
        {
            _connectionString = $"Data Source={databasePath}";
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        }

        #region Income Operations

        public async Task<IEnumerable<Income>> GetIncomeAsync(DateTime startDate, DateTime endDate)
        {
            const string sql = @"
                SELECT Id, Date, Amount, Description, CreatedAt, UpdatedAt 
                FROM Income 
                WHERE Date >= @StartDate AND Date <= @EndDate 
                ORDER BY Date DESC";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);
            
            command.Parameters.AddWithValue("@StartDate", startDate.Date);
            command.Parameters.AddWithValue("@EndDate", endDate.Date);

            var incomes = new List<Income>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                incomes.Add(new Income
                {
                    Id = reader.GetInt32("Id"),
                    Date = reader.GetDateTime("Date"),
                    Amount = reader.GetDecimal("Amount"),
                    Description = reader.GetString("Description"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    UpdatedAt = reader.GetDateTime("UpdatedAt")
                });
            }

            return incomes;
        }

        public async Task<Income> GetIncomeByIdAsync(int id)
        {
            const string sql = @"
                SELECT Id, Date, Amount, Description, CreatedAt, UpdatedAt 
                FROM Income 
                WHERE Id = @Id";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Income
                {
                    Id = reader.GetInt32("Id"),
                    Date = reader.GetDateTime("Date"),
                    Amount = reader.GetDecimal("Amount"),
                    Description = reader.GetString("Description"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    UpdatedAt = reader.GetDateTime("UpdatedAt")
                };
            }

            throw new InvalidOperationException($"Income with ID {id} not found");
        }

        public async Task<Income> AddIncomeAsync(Income income)
        {
            const string sql = @"
                INSERT INTO Income (Date, Amount, Description) 
                VALUES (@Date, @Amount, @Description);
                SELECT last_insert_rowid();";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);

            command.Parameters.AddWithValue("@Date", income.Date.Date);
            command.Parameters.AddWithValue("@Amount", income.Amount);
            command.Parameters.AddWithValue("@Description", income.Description);

            var newId = Convert.ToInt32(await command.ExecuteScalarAsync());
            return await GetIncomeByIdAsync(newId);
        }

        public async Task<Income> UpdateIncomeAsync(Income income)
        {
            const string sql = @"
                UPDATE Income 
                SET Date = @Date, Amount = @Amount, Description = @Description 
                WHERE Id = @Id";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);

            command.Parameters.AddWithValue("@Id", income.Id);
            command.Parameters.AddWithValue("@Date", income.Date.Date);
            command.Parameters.AddWithValue("@Amount", income.Amount);
            command.Parameters.AddWithValue("@Description", income.Description);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            if (rowsAffected == 0)
                throw new InvalidOperationException($"Income with ID {income.Id} not found");

            return await GetIncomeByIdAsync(income.Id);
        }

        public async Task DeleteIncomeAsync(int id)
        {
            const string sql = "DELETE FROM Income WHERE Id = @Id";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            if (rowsAffected == 0)
                throw new InvalidOperationException($"Income with ID {id} not found");
        }

        #endregion

        #region Spending Operations

        public async Task<IEnumerable<Spending>> GetSpendingAsync(DateTime startDate, DateTime endDate)
        {
            const string sql = @"
                SELECT Id, Date, Amount, Description, CategoryId, CreatedAt, UpdatedAt 
                FROM Spending 
                WHERE Date >= @StartDate AND Date <= @EndDate 
                ORDER BY Date DESC";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);
            
            command.Parameters.AddWithValue("@StartDate", startDate.Date);
            command.Parameters.AddWithValue("@EndDate", endDate.Date);

            var spendings = new List<Spending>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                spendings.Add(new Spending
                {
                    Id = reader.GetInt32("Id"),
                    Date = reader.GetDateTime("Date"),
                    Amount = reader.GetDecimal("Amount"),
                    Description = reader.GetString("Description"),
                    CategoryId = reader.GetInt32("CategoryId"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    UpdatedAt = reader.GetDateTime("UpdatedAt")
                });
            }

            return spendings;
        }

        public async Task<IEnumerable<SpendingWithCategory>> GetSpendingWithCategoryAsync(DateTime startDate, DateTime endDate)
        {
            const string sql = @"
                SELECT s.Id, s.Date, s.Amount, s.Description, s.CategoryId, 
                       c.Name as CategoryName, s.CreatedAt, s.UpdatedAt
                FROM Spending s
                JOIN Categories c ON s.CategoryId = c.Id
                WHERE s.Date >= @StartDate AND s.Date <= @EndDate AND c.IsActive = 1
                ORDER BY s.Date DESC";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);
            
            command.Parameters.AddWithValue("@StartDate", startDate.Date);
            command.Parameters.AddWithValue("@EndDate", endDate.Date);

            var spendings = new List<SpendingWithCategory>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                spendings.Add(new SpendingWithCategory
                {
                    Id = reader.GetInt32("Id"),
                    Date = reader.GetDateTime("Date"),
                    Amount = reader.GetDecimal("Amount"),
                    Description = reader.GetString("Description"),
                    CategoryId = reader.GetInt32("CategoryId"),
                    CategoryName = reader.GetString("CategoryName"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    UpdatedAt = reader.GetDateTime("UpdatedAt")
                });
            }

            return spendings;
        }

        public async Task<Spending> GetSpendingByIdAsync(int id)
        {
            const string sql = @"
                SELECT Id, Date, Amount, Description, CategoryId, CreatedAt, UpdatedAt 
                FROM Spending 
                WHERE Id = @Id";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Spending
                {
                    Id = reader.GetInt32("Id"),
                    Date = reader.GetDateTime("Date"),
                    Amount = reader.GetDecimal("Amount"),
                    Description = reader.GetString("Description"),
                    CategoryId = reader.GetInt32("CategoryId"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    UpdatedAt = reader.GetDateTime("UpdatedAt")
                };
            }

            throw new InvalidOperationException($"Spending with ID {id} not found");
        }

        public async Task<Spending> AddSpendingAsync(Spending spending)
        {
            const string sql = @"
                INSERT INTO Spending (Date, Amount, Description, CategoryId) 
                VALUES (@Date, @Amount, @Description, @CategoryId);
                SELECT last_insert_rowid();";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);

            command.Parameters.AddWithValue("@Date", spending.Date.Date);
            command.Parameters.AddWithValue("@Amount", spending.Amount);
            command.Parameters.AddWithValue("@Description", spending.Description);
            command.Parameters.AddWithValue("@CategoryId", spending.CategoryId);

            var newId = Convert.ToInt32(await command.ExecuteScalarAsync());
            return await GetSpendingByIdAsync(newId);
        }

        public async Task<Spending> UpdateSpendingAsync(Spending spending)
        {
            const string sql = @"
                UPDATE Spending 
                SET Date = @Date, Amount = @Amount, Description = @Description, CategoryId = @CategoryId 
                WHERE Id = @Id";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);

            command.Parameters.AddWithValue("@Id", spending.Id);
            command.Parameters.AddWithValue("@Date", spending.Date.Date);
            command.Parameters.AddWithValue("@Amount", spending.Amount);
            command.Parameters.AddWithValue("@Description", spending.Description);
            command.Parameters.AddWithValue("@CategoryId", spending.CategoryId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            if (rowsAffected == 0)
                throw new InvalidOperationException($"Spending with ID {spending.Id} not found");

            return await GetSpendingByIdAsync(spending.Id);
        }

        public async Task DeleteSpendingAsync(int id)
        {
            const string sql = "DELETE FROM Spending WHERE Id = @Id";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            if (rowsAffected == 0)
                throw new InvalidOperationException($"Spending with ID {id} not found");
        }

        #endregion

        #region Category Operations

        public async Task<IEnumerable<Category>> GetCategoriesAsync()
        {
            const string sql = @"
                SELECT Id, Name, DisplayOrder, IsActive, CreatedAt, UpdatedAt 
                FROM Categories 
                WHERE IsActive = 1 
                ORDER BY DisplayOrder, Name";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);

            var categories = new List<Category>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                categories.Add(new Category
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    DisplayOrder = reader.GetInt32("DisplayOrder"),
                    IsActive = reader.GetBoolean("IsActive"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    UpdatedAt = reader.GetDateTime("UpdatedAt")
                });
            }

            return categories;
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            System.Diagnostics.Debug.WriteLine("BudgetService: GetAllCategoriesAsync called");
            
            const string sql = @"
                SELECT Id, Name, DisplayOrder, IsActive, CreatedAt, UpdatedAt 
                FROM Categories 
                ORDER BY DisplayOrder, Name";

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                System.Diagnostics.Debug.WriteLine($"BudgetService: Opening connection with string: {_connectionString}");
                await connection.OpenAsync();
                System.Diagnostics.Debug.WriteLine("BudgetService: Connection opened successfully");
                
                using var command = new SqliteCommand(sql, connection);

                var categories = new List<Category>();
                using var reader = await command.ExecuteReaderAsync();
                
                System.Diagnostics.Debug.WriteLine("BudgetService: Starting to read categories...");
                
                while (await reader.ReadAsync())
                {
                    var category = new Category
                    {
                        Id = reader.GetInt32("Id"),
                        Name = reader.GetString("Name"),
                        DisplayOrder = reader.GetInt32("DisplayOrder"),
                        IsActive = reader.GetBoolean("IsActive"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        UpdatedAt = reader.GetDateTime("UpdatedAt")
                    };
                    
                    categories.Add(category);
                    System.Diagnostics.Debug.WriteLine($"BudgetService: Read category - Id: {category.Id}, Name: {category.Name}, Active: {category.IsActive}");
                }

                System.Diagnostics.Debug.WriteLine($"BudgetService: GetAllCategoriesAsync returning {categories.Count} categories");
                
                // Quick test - also check if table exists and has any rows at all
                var testCommand = new SqliteCommand("SELECT COUNT(*) FROM Categories", connection);
                var totalCount = Convert.ToInt32(await testCommand.ExecuteScalarAsync());
                System.Diagnostics.Debug.WriteLine($"BudgetService: Total categories in database (including inactive): {totalCount}");
                
                return categories;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BudgetService: GetAllCategoriesAsync exception: {ex}");
                throw;
            }
        }

        public async Task<Category> GetCategoryByIdAsync(int id)
        {
            const string sql = @"
                SELECT Id, Name, DisplayOrder, IsActive, CreatedAt, UpdatedAt 
                FROM Categories 
                WHERE Id = @Id";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Category
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    DisplayOrder = reader.GetInt32("DisplayOrder"),
                    IsActive = reader.GetBoolean("IsActive"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    UpdatedAt = reader.GetDateTime("UpdatedAt")
                };
            }

            throw new InvalidOperationException($"Category with ID {id} not found");
        }

        public async Task<Category> AddCategoryAsync(Category category)
        {
            const string sql = @"
                INSERT INTO Categories (Name, DisplayOrder, IsActive) 
                VALUES (@Name, @DisplayOrder, @IsActive);
                SELECT last_insert_rowid();";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);

            command.Parameters.AddWithValue("@Name", category.Name);
            command.Parameters.AddWithValue("@DisplayOrder", category.DisplayOrder);
            command.Parameters.AddWithValue("@IsActive", category.IsActive);

            var newId = Convert.ToInt32(await command.ExecuteScalarAsync());
            return await GetCategoryByIdAsync(newId);
        }

        public async Task<Category> UpdateCategoryAsync(Category category)
        {
            const string sql = @"
                UPDATE Categories 
                SET Name = @Name, DisplayOrder = @DisplayOrder, IsActive = @IsActive 
                WHERE Id = @Id";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);

            command.Parameters.AddWithValue("@Id", category.Id);
            command.Parameters.AddWithValue("@Name", category.Name);
            command.Parameters.AddWithValue("@DisplayOrder", category.DisplayOrder);
            command.Parameters.AddWithValue("@IsActive", category.IsActive);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            if (rowsAffected == 0)
                throw new InvalidOperationException($"Category with ID {category.Id} not found");

            return await GetCategoryByIdAsync(category.Id);
        }

        public async Task DeleteCategoryAsync(int id)
        {
            // Check if category is being used
            const string checkSql = "SELECT COUNT(*) FROM Spending WHERE CategoryId = @Id";
            
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var checkCommand = new SqliteCommand(checkSql, connection);
            checkCommand.Parameters.AddWithValue("@Id", id);

            var usageCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
            if (usageCount > 0)
            {
                throw new InvalidOperationException($"Cannot delete category: it is being used by {usageCount} spending entries");
            }

            const string sql = "DELETE FROM Categories WHERE Id = @Id";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            if (rowsAffected == 0)
                throw new InvalidOperationException($"Category with ID {id} not found");
        }

        #endregion

        #region Summary Operations

        public async Task<BudgetSummary> GetBudgetSummaryAsync(DateTime startDate, DateTime endDate)
        {
            const string sql = @"
                SELECT 
                    COALESCE(SUM(CASE WHEN source = 'Income' THEN amount ELSE 0 END), 0) as TotalIncome,
                    COALESCE(SUM(CASE WHEN source = 'Spending' THEN amount ELSE 0 END), 0) as TotalSpending,
                    COALESCE(SUM(CASE WHEN source = 'Spending' AND category = 'Family' THEN amount ELSE 0 END), 0) as FamilySpending,
                    COALESCE(SUM(CASE WHEN source = 'Spending' AND category = 'Personal' THEN amount ELSE 0 END), 0) as PersonalSpending,
                    COALESCE(SUM(CASE WHEN source = 'Spending' AND category = 'Marini' THEN amount ELSE 0 END), 0) as MariniSpending
                FROM (
                    SELECT Amount, 'Income' as source, NULL as category FROM Income 
                    WHERE Date >= @StartDate AND Date <= @EndDate
                    UNION ALL
                    SELECT s.Amount, 'Spending' as source, c.Name as category 
                    FROM Spending s 
                    JOIN Categories c ON s.CategoryId = c.Id 
                    WHERE s.Date >= @StartDate AND s.Date <= @EndDate AND c.IsActive = 1
                ) combined";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);
            
            command.Parameters.AddWithValue("@StartDate", startDate.Date);
            command.Parameters.AddWithValue("@EndDate", endDate.Date);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new BudgetSummary
                {
                    TotalIncome = reader.GetDecimal("TotalIncome"),
                    TotalSpending = reader.GetDecimal("TotalSpending"),
                    FamilySpending = reader.GetDecimal("FamilySpending"),
                    PersonalSpending = reader.GetDecimal("PersonalSpending"),
                    MariniSpending = reader.GetDecimal("MariniSpending"),
                    PeriodStart = startDate,
                    PeriodEnd = endDate
                };
            }

            return new BudgetSummary { PeriodStart = startDate, PeriodEnd = endDate };
        }

        public async Task<IEnumerable<MonthlySummary>> GetMonthlySummaryAsync(int year)
        {
            const string sql = @"
                SELECT Month, Type, CategoryName, TotalAmount 
                FROM v_monthly_summary 
                WHERE Month LIKE @Year || '%' 
                ORDER BY Month, Type, CategoryName";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Year", year.ToString());

            var summaries = new List<MonthlySummary>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                summaries.Add(new MonthlySummary
                {
                    Month = reader.GetString("Month"),
                    Type = reader.GetString("Type"),
                    CategoryName = reader.IsDBNull("CategoryName") ? null : reader.GetString("CategoryName"),
                    TotalAmount = reader.GetDecimal("TotalAmount")
                });
            }

            return summaries;
        }

        public async Task<decimal> GetCategoryTotalAsync(int categoryId, DateTime startDate, DateTime endDate)
        {
            const string sql = @"
                SELECT COALESCE(SUM(Amount), 0) 
                FROM Spending 
                WHERE CategoryId = @CategoryId AND Date >= @StartDate AND Date <= @EndDate";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);
            
            command.Parameters.AddWithValue("@CategoryId", categoryId);
            command.Parameters.AddWithValue("@StartDate", startDate.Date);
            command.Parameters.AddWithValue("@EndDate", endDate.Date);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToDecimal(result);
        }

        #endregion

        #region Bank Statement Operations

        /// <summary>
        /// Calculates bank statement period from beginning of records to the most recent statement day
        /// </summary>
        /// <param name="statementDay">Day of the month for bank statements (1-31)</param>
        /// <returns>Tuple of (start date, end date) from beginning of tracking to current statement day</returns>
        public (DateTime StartDate, DateTime EndDate) GetLastBankStatementPeriod(int statementDay)
        {
            var today = DateTime.Today;
            var currentMonth = new DateTime(today.Year, today.Month, 1);
            
            // Find the statement day for current month (handle months with fewer days)
            var currentStatementDay = Math.Min(statementDay, DateTime.DaysInMonth(today.Year, today.Month));
            var currentStatementDate = new DateTime(today.Year, today.Month, currentStatementDay);
            
            DateTime periodEnd;
            
            if (today >= currentStatementDate)
            {
                // We've passed this month's statement date, so the period ends on current statement date
                periodEnd = currentStatementDate;
            }
            else
            {
                // We haven't reached this month's statement date, so use previous month's statement date
                var prevMonth = currentMonth.AddMonths(-1);
                var prevStatementDay = Math.Min(statementDay, DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month));
                periodEnd = new DateTime(prevMonth.Year, prevMonth.Month, prevStatementDay);
            }
            
            // Period start is from the beginning of all data (use a reasonable early date)
            var periodStart = new DateTime(2020, 1, 1); // Start from beginning of tracking
            
            return (periodStart, periodEnd);
        }

        /// <summary>
        /// Gets bank statement summary for the most recent complete statement period
        /// </summary>
        /// <param name="statementDay">Day of the month for bank statements (1-31)</param>
        /// <returns>Bank statement summary with income, spending, and period information</returns>
        public async Task<BankStatementSummary> GetBankStatementSummaryAsync(int statementDay)
        {
            var (startDate, endDate) = GetLastBankStatementPeriod(statementDay);
            
            const string sql = @"
                SELECT 
                    COALESCE(SUM(CASE WHEN source = 'Income' THEN amount ELSE 0 END), 0) as TotalIncome,
                    COALESCE(SUM(CASE WHEN source = 'Spending' THEN amount ELSE 0 END), 0) as TotalSpending
                FROM (
                    SELECT Amount, 'Income' as source FROM Income 
                    WHERE Date >= @StartDate AND Date <= @EndDate
                    UNION ALL
                    SELECT Amount, 'Spending' as source FROM Spending 
                    WHERE Date >= @StartDate AND Date <= @EndDate
                )";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@StartDate", startDate.Date);
            command.Parameters.AddWithValue("@EndDate", endDate.Date);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new BankStatementSummary
                {
                    TotalIncome = reader.GetDecimal("TotalIncome"),
                    TotalSpending = reader.GetDecimal("TotalSpending"),
                    PeriodStart = startDate,
                    PeriodEnd = endDate,
                    StatementDay = statementDay
                };
            }

            // Fallback if no data
            return new BankStatementSummary
            {
                TotalIncome = 0,
                TotalSpending = 0,
                PeriodStart = startDate,
                PeriodEnd = endDate,
                StatementDay = statementDay
            };
        }

        #endregion

        #region Export Operations

        public async Task ExportDataAsync(DateTime startDate, DateTime endDate, string? filePath = null)
        {
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            if (string.IsNullOrEmpty(filePath))
            {
                var fileName = $"budget_export_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";
                filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
            }

            await ExportToExcelAsync(startDate, endDate, filePath);
        }

        public async Task ExportToExcelAsync(DateTime startDate, DateTime endDate, string filePath)
        {
            using var package = new ExcelPackage();

            // Get data
            var income = await GetIncomeAsync(startDate, endDate);
            var spending = await GetSpendingWithCategoryAsync(startDate, endDate);
            var summary = await GetBudgetSummaryAsync(startDate, endDate);
            var categories = await GetCategoriesAsync();

            // Create Summary sheet
            CreateSummarySheet(package, summary, startDate, endDate);

            // Create Income sheet
            CreateIncomeSheet(package, income.OrderBy(i => i.Date));

            // Create Spending sheet
            CreateSpendingSheet(package, spending.OrderBy(s => s.Date));

            // Create Category Analysis sheet
            CreateCategoryAnalysisSheet(package, spending, categories);

            // Save file
            var file = new FileInfo(filePath);
            await package.SaveAsAsync(file);
        }

        public async Task<string> ExportToCsvAsync(DateTime startDate, DateTime endDate)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Date,Type,Category,Description,Amount");

            // Export income
            var income = await GetIncomeAsync(startDate, endDate);
            foreach (var item in income.OrderBy(i => i.Date))
            {
                csv.AppendLine($"{item.Date:yyyy-MM-dd},Income,,\"{item.Description}\",{item.Amount}");
            }

            // Export spending
            var spending = await GetSpendingWithCategoryAsync(startDate, endDate);
            foreach (var item in spending.OrderBy(s => s.Date))
            {
                csv.AppendLine($"{item.Date:yyyy-MM-dd},Spending,{item.CategoryName},\"{item.Description}\",{item.Amount}");
            }

            return csv.ToString();
        }

        private void CreateSummarySheet(ExcelPackage package, BudgetSummary summary, DateTime startDate, DateTime endDate)
        {
            var worksheet = package.Workbook.Worksheets.Add("Summary");

            // Header
            worksheet.Cells["A1"].Value = "Budget Summary Report";
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;

            worksheet.Cells["A2"].Value = $"Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}";
            worksheet.Cells["A2"].Style.Font.Size = 12;

            // Summary data
            worksheet.Cells["A4"].Value = "Total Income:";
            worksheet.Cells["B4"].Value = summary.TotalIncome;
            worksheet.Cells["B4"].Style.Numberformat.Format = "$#,##0.00";
            worksheet.Cells["B4"].Style.Font.Bold = true;
            worksheet.Cells["B4"].Style.Font.Color.SetColor(System.Drawing.Color.Green);

            worksheet.Cells["A5"].Value = "Total Spending:";
            worksheet.Cells["B5"].Value = summary.TotalSpending;
            worksheet.Cells["B5"].Style.Numberformat.Format = "$#,##0.00";
            worksheet.Cells["B5"].Style.Font.Bold = true;
            worksheet.Cells["B5"].Style.Font.Color.SetColor(System.Drawing.Color.Red);

            worksheet.Cells["A6"].Value = "Remaining Budget:";
            worksheet.Cells["B6"].Value = summary.RemainingBudget;
            worksheet.Cells["B6"].Style.Numberformat.Format = "$#,##0.00";
            worksheet.Cells["B6"].Style.Font.Bold = true;
            worksheet.Cells["B6"].Style.Font.Color.SetColor(summary.RemainingBudget >= 0 ? System.Drawing.Color.Green : System.Drawing.Color.Red);

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateIncomeSheet(ExcelPackage package, IOrderedEnumerable<Income> income)
        {
            var worksheet = package.Workbook.Worksheets.Add("Income");

            // Headers
            worksheet.Cells["A1"].Value = "Date";
            worksheet.Cells["B1"].Value = "Description";
            worksheet.Cells["C1"].Value = "Amount";

            // Header styling
            using (var range = worksheet.Cells["A1:C1"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Data
            int row = 2;
            foreach (var item in income)
            {
                worksheet.Cells[row, 1].Value = item.Date;
                worksheet.Cells[row, 1].Style.Numberformat.Format = "yyyy-mm-dd";
                worksheet.Cells[row, 2].Value = item.Description;
                worksheet.Cells[row, 3].Value = item.Amount;
                worksheet.Cells[row, 3].Style.Numberformat.Format = "$#,##0.00";
                row++;
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateSpendingSheet(ExcelPackage package, IOrderedEnumerable<SpendingWithCategory> spending)
        {
            var worksheet = package.Workbook.Worksheets.Add("Spending");

            // Headers
            worksheet.Cells["A1"].Value = "Date";
            worksheet.Cells["B1"].Value = "Category";
            worksheet.Cells["C1"].Value = "Description";
            worksheet.Cells["D1"].Value = "Amount";

            // Header styling
            using (var range = worksheet.Cells["A1:D1"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Data
            int row = 2;
            foreach (var item in spending)
            {
                worksheet.Cells[row, 1].Value = item.Date;
                worksheet.Cells[row, 1].Style.Numberformat.Format = "yyyy-mm-dd";
                worksheet.Cells[row, 2].Value = item.CategoryName;
                worksheet.Cells[row, 3].Value = item.Description;
                worksheet.Cells[row, 4].Value = item.Amount;
                worksheet.Cells[row, 4].Style.Numberformat.Format = "$#,##0.00";
                row++;
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateCategoryAnalysisSheet(ExcelPackage package, IEnumerable<SpendingWithCategory> spending, IEnumerable<Category> categories)
        {
            var worksheet = package.Workbook.Worksheets.Add("Category Analysis");

            // Group spending by category
            var categoryTotals = spending
                .GroupBy(s => new { s.CategoryId, s.CategoryName })
                .Select(g => new
                {
                    CategoryName = g.Key.CategoryName,
                    Amount = g.Sum(s => s.Amount),
                    Count = g.Count(),
                    Average = g.Average(s => s.Amount)
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            // Headers
            worksheet.Cells["A1"].Value = "Category";
            worksheet.Cells["B1"].Value = "Total Amount";
            worksheet.Cells["C1"].Value = "Number of Entries";
            worksheet.Cells["D1"].Value = "Average per Entry";

            // Header styling
            using (var range = worksheet.Cells["A1:D1"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Data
            int row = 2;
            foreach (var category in categoryTotals)
            {
                worksheet.Cells[row, 1].Value = category.CategoryName;
                worksheet.Cells[row, 2].Value = category.Amount;
                worksheet.Cells[row, 2].Style.Numberformat.Format = "$#,##0.00";
                worksheet.Cells[row, 3].Value = category.Count;
                worksheet.Cells[row, 4].Value = category.Average;
                worksheet.Cells[row, 4].Style.Numberformat.Format = "$#,##0.00";
                row++;
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        #endregion

        #region Settings Operations

        public async Task<string> GetSettingAsync(string key, string defaultValue = "")
        {
            const string sql = "SELECT Value FROM AppSettings WHERE Key = @Key";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Key", key);

            var result = await command.ExecuteScalarAsync();
            return result?.ToString() ?? defaultValue;
        }

        public async Task SetSettingAsync(string key, string value)
        {
            const string sql = @"
                INSERT INTO AppSettings (Key, Value) 
                VALUES (@Key, @Value) 
                ON CONFLICT(Key) DO UPDATE SET Value = @Value, UpdatedAt = CURRENT_TIMESTAMP";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);
            
            command.Parameters.AddWithValue("@Key", key);
            command.Parameters.AddWithValue("@Value", value);

            await command.ExecuteNonQueryAsync();
        }

        #endregion

        #region Database Operations

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                
                // CRITICAL: Check if required tables actually exist
                var tablesExist = await DoesTableExistAsync(connection, "Categories") &&
                                 await DoesTableExistAsync(connection, "Income") &&
                                 await DoesTableExistAsync(connection, "Spending");
                                 
                return tablesExist;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> DoesTableExistAsync(SqliteConnection connection, string tableName)
        {
            const string sql = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@tableName";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@tableName", tableName);
            
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // Step 1: Create Categories table
                await ExecuteSqlAsync(connection, @"
                    CREATE TABLE IF NOT EXISTS Categories (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL UNIQUE,
                        Description TEXT,
                        DisplayOrder INTEGER DEFAULT 0,
                        IsActive BOOLEAN DEFAULT 1,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    )");

                // Step 2: Create Income table
                await ExecuteSqlAsync(connection, @"
                    CREATE TABLE IF NOT EXISTS Income (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Date DATE NOT NULL,
                        Amount DECIMAL(10,2) NOT NULL,
                        Description TEXT NOT NULL,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    )");

                // Step 3: Create Spending table
                await ExecuteSqlAsync(connection, @"
                    CREATE TABLE IF NOT EXISTS Spending (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Date DATE NOT NULL,
                        Amount DECIMAL(10,2) NOT NULL,
                        Description TEXT NOT NULL,
                        CategoryId INTEGER NOT NULL,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
                    )");

                // Step 4: Insert default categories (safe operation)
                await ExecuteSqlAsync(connection, @"
                    INSERT OR IGNORE INTO Categories (Name, Description, DisplayOrder, IsActive) VALUES 
                        ('Family', 'Family-related expenses like groceries, utilities, household items', 1, 1),
                        ('Personal', 'Personal expenses like clothing, hobbies, entertainment', 2, 1),
                        ('Marini', 'Special category for Marini-related expenses', 3, 1)");

                // Step 5: CRITICAL VERIFICATION - Ensure all tables exist
                var categoriesExist = await DoesTableExistAsync(connection, "Categories");
                var incomeExist = await DoesTableExistAsync(connection, "Income");
                var spendingExist = await DoesTableExistAsync(connection, "Spending");

                if (!categoriesExist || !incomeExist || !spendingExist)
                {
                    throw new InvalidOperationException("Database initialization failed - tables were not created properly");
                }

                // Step 6: Verify categories were inserted
                var categoryCount = await ExecuteScalarAsync<int>(connection, "SELECT COUNT(*) FROM Categories");
                if (categoryCount == 0)
                {
                    throw new InvalidOperationException("Database initialization failed - default categories were not created");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize database: {ex.Message}", ex);
            }
        }

        private async Task ExecuteSqlAsync(SqliteConnection connection, string sql)
        {
            using var command = new SqliteCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private async Task<T> ExecuteScalarAsync<T>(SqliteConnection connection, string sql)
        {
            using var command = new SqliteCommand(sql, connection);
            var result = await command.ExecuteScalarAsync();
            return (T)Convert.ChangeType(result!, typeof(T));
        }

        public async Task BackupDatabaseAsync(string backupPath)
        {
            using var source = new SqliteConnection(_connectionString);
            using var destination = new SqliteConnection($"Data Source={backupPath}");
            
            await source.OpenAsync();
            await destination.OpenAsync();
            
            // Simple backup using file copy approach
            await source.CloseAsync();
            await destination.CloseAsync();
            
            var sourceFile = _connectionString.Replace("Data Source=", "").Split(';')[0];
            File.Copy(sourceFile, backupPath, true);
        }

        public async Task RestoreDatabaseAsync(string backupPath)
        {
            if (!File.Exists(backupPath))
                throw new FileNotFoundException("Backup file not found", backupPath);

            // Simple restore using file copy approach
            var destinationFile = _connectionString.Replace("Data Source=", "").Split(';')[0];
            File.Copy(backupPath, destinationFile, true);
        }

        #endregion
    }
}