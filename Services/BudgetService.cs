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
using BudgetManagement.Features.Income.Queries;
using BudgetManagement.Features.Spending.Queries;
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

        /// <summary>
        /// Gets the earliest date from either income or spending entries
        /// </summary>
        /// <returns>The earliest date found, or null if no entries exist</returns>
        public async Task<DateTime?> GetEarliestEntryDateAsync()
        {
            const string sql = @"
                SELECT MIN(earliest_date) as EarliestDate
                FROM (
                    SELECT MIN(Date) as earliest_date FROM Income
                    UNION ALL
                    SELECT MIN(Date) as earliest_date FROM Spending
                ) combined";

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqliteCommand(sql, connection);
            
            var result = await command.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
            {
                return DateTime.Parse(result.ToString()!);
            }
            
            return null;
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

        #region Advanced Search Operations

        public async Task<AdvancedIncomeSearchResult> AdvancedIncomeSearchAsync(
            string? descriptionPattern = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            decimal? minAmount = null,
            decimal? maxAmount = null,
            int skip = 0,
            int take = 50,
            IncomeSortBy sortBy = IncomeSortBy.Date,
            BudgetManagement.Features.Income.Queries.SortDirection sortDirection = BudgetManagement.Features.Income.Queries.SortDirection.Descending)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var conditions = new List<string>();
            var parameters = new List<SqliteParameter>();

            // Build WHERE clause dynamically
            if (!string.IsNullOrWhiteSpace(descriptionPattern))
            {
                conditions.Add("Description LIKE @descriptionPattern");
                parameters.Add(new SqliteParameter("@descriptionPattern", $"%{descriptionPattern}%"));
            }

            if (startDate.HasValue)
            {
                conditions.Add("Date >= @startDate");
                parameters.Add(new SqliteParameter("@startDate", startDate.Value.ToString("yyyy-MM-dd")));
            }

            if (endDate.HasValue)
            {
                conditions.Add("Date <= @endDate");
                parameters.Add(new SqliteParameter("@endDate", endDate.Value.ToString("yyyy-MM-dd")));
            }

            if (minAmount.HasValue)
            {
                conditions.Add("Amount >= @minAmount");
                parameters.Add(new SqliteParameter("@minAmount", minAmount.Value));
            }

            if (maxAmount.HasValue)
            {
                conditions.Add("Amount <= @maxAmount");
                parameters.Add(new SqliteParameter("@maxAmount", maxAmount.Value));
            }

            var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";
            
            // Build ORDER BY clause
            var orderByField = sortBy switch
            {
                IncomeSortBy.Date => "Date",
                IncomeSortBy.Amount => "Amount",
                IncomeSortBy.Description => "Description",
                IncomeSortBy.CreatedAt => "CreatedAt",
                _ => "Date"
            };
            var orderByDirection = sortDirection == BudgetManagement.Features.Income.Queries.SortDirection.Ascending ? "ASC" : "DESC";
            var orderByClause = $"ORDER BY {orderByField} {orderByDirection}";

            // Get total count for pagination
            var countSql = $@"
                SELECT COUNT(*), COALESCE(SUM(Amount), 0) as TotalAmount
                FROM Income 
                {whereClause}";

            int totalCount = 0;
            decimal totalAmount = 0;
            using (var countCommand = new SqliteCommand(countSql, connection))
            {
                foreach (var param in parameters)
                {
                    countCommand.Parameters.Add(new SqliteParameter(param.ParameterName, param.Value));
                }
                
                using var reader = await countCommand.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    totalCount = reader.GetInt32(0);
                    totalAmount = reader.GetDecimal(1);
                }
            }

            // Get paginated results
            var dataSql = $@"
                SELECT Id, Description, Amount, Date, CreatedAt
                FROM Income 
                {whereClause}
                {orderByClause}
                LIMIT @take OFFSET @skip";

            parameters.Add(new SqliteParameter("@take", take));
            parameters.Add(new SqliteParameter("@skip", skip));

            var incomes = new List<Income>();
            using (var dataCommand = new SqliteCommand(dataSql, connection))
            {
                foreach (var param in parameters)
                {
                    dataCommand.Parameters.Add(new SqliteParameter(param.ParameterName, param.Value));
                }
                
                using var reader = await dataCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    incomes.Add(new Income
                    {
                        Id = reader.GetInt32("Id"),
                        Description = reader.GetString("Description"),
                        Amount = reader.GetDecimal("Amount"),
                        Date = DateTime.Parse(reader.GetString("Date")),
                        CreatedAt = DateTime.Parse(reader.GetString("CreatedAt"))
                    });
                }
            }

            return new AdvancedIncomeSearchResult
            {
                Incomes = incomes,
                TotalCount = totalCount,
                TotalAmount = totalAmount,
                HasMore = (skip + take) < totalCount
            };
        }

        public async Task<AdvancedSpendingSearchResult> AdvancedSpendingSearchAsync(
            string? descriptionPattern = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            decimal? minAmount = null,
            decimal? maxAmount = null,
            List<int>? categoryIds = null,
            int skip = 0,
            int take = 50,
            SpendingSortBy sortBy = SpendingSortBy.Date,
            BudgetManagement.Features.Spending.Queries.SortDirection sortDirection = BudgetManagement.Features.Spending.Queries.SortDirection.Descending)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var conditions = new List<string>();
            var parameters = new List<SqliteParameter>();

            // Build WHERE clause dynamically
            if (!string.IsNullOrWhiteSpace(descriptionPattern))
            {
                conditions.Add("s.Description LIKE @descriptionPattern");
                parameters.Add(new SqliteParameter("@descriptionPattern", $"%{descriptionPattern}%"));
            }

            if (startDate.HasValue)
            {
                conditions.Add("s.Date >= @startDate");
                parameters.Add(new SqliteParameter("@startDate", startDate.Value.ToString("yyyy-MM-dd")));
            }

            if (endDate.HasValue)
            {
                conditions.Add("s.Date <= @endDate");
                parameters.Add(new SqliteParameter("@endDate", endDate.Value.ToString("yyyy-MM-dd")));
            }

            if (minAmount.HasValue)
            {
                conditions.Add("s.Amount >= @minAmount");
                parameters.Add(new SqliteParameter("@minAmount", minAmount.Value));
            }

            if (maxAmount.HasValue)
            {
                conditions.Add("s.Amount <= @maxAmount");
                parameters.Add(new SqliteParameter("@maxAmount", maxAmount.Value));
            }

            if (categoryIds != null && categoryIds.Any())
            {
                var categoryPlaceholders = string.Join(",", categoryIds.Select((_, i) => $"@categoryId{i}"));
                conditions.Add($"s.CategoryId IN ({categoryPlaceholders})");
                
                for (int i = 0; i < categoryIds.Count; i++)
                {
                    parameters.Add(new SqliteParameter($"@categoryId{i}", categoryIds[i]));
                }
            }

            var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";
            
            // Build ORDER BY clause
            var orderByField = sortBy switch
            {
                SpendingSortBy.Date => "s.Date",
                SpendingSortBy.Amount => "s.Amount",
                SpendingSortBy.Description => "s.Description",
                SpendingSortBy.Category => "c.Name",
                SpendingSortBy.CreatedAt => "s.CreatedAt",
                _ => "s.Date"
            };
            var orderByDirection = sortDirection == BudgetManagement.Features.Spending.Queries.SortDirection.Ascending ? "ASC" : "DESC";
            var orderByClause = $"ORDER BY {orderByField} {orderByDirection}";

            // Get total count and amount for pagination, plus category totals
            var countSql = $@"
                SELECT COUNT(*), COALESCE(SUM(s.Amount), 0) as TotalAmount
                FROM Spending s 
                JOIN Categories c ON s.CategoryId = c.Id 
                {whereClause}";

            int totalCount = 0;
            decimal totalAmount = 0;
            using (var countCommand = new SqliteCommand(countSql, connection))
            {
                foreach (var param in parameters)
                {
                    countCommand.Parameters.Add(new SqliteParameter(param.ParameterName, param.Value));
                }
                
                using var reader = await countCommand.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    totalCount = reader.GetInt32(0);
                    totalAmount = reader.GetDecimal(1);
                }
            }

            // Get category totals
            var categoryTotalsSql = $@"
                SELECT s.CategoryId, COALESCE(SUM(s.Amount), 0) as CategoryTotal
                FROM Spending s 
                JOIN Categories c ON s.CategoryId = c.Id 
                {whereClause}
                GROUP BY s.CategoryId";

            var categoryTotals = new Dictionary<int, decimal>();
            using (var categoryCommand = new SqliteCommand(categoryTotalsSql, connection))
            {
                foreach (var param in parameters)
                {
                    categoryCommand.Parameters.Add(new SqliteParameter(param.ParameterName, param.Value));
                }
                
                using var reader = await categoryCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var categoryId = reader.GetInt32("CategoryId");
                    var categoryTotal = reader.GetDecimal("CategoryTotal");
                    categoryTotals[categoryId] = categoryTotal;
                }
            }

            // Get paginated results
            var dataSql = $@"
                SELECT s.Id, s.Date, s.Amount, s.Description, s.CategoryId, 
                       c.Name as CategoryName, s.CreatedAt
                FROM Spending s 
                JOIN Categories c ON s.CategoryId = c.Id 
                {whereClause}
                {orderByClause}
                LIMIT @take OFFSET @skip";

            parameters.Add(new SqliteParameter("@take", take));
            parameters.Add(new SqliteParameter("@skip", skip));

            var spendings = new List<SpendingWithCategory>();
            using (var dataCommand = new SqliteCommand(dataSql, connection))
            {
                foreach (var param in parameters)
                {
                    dataCommand.Parameters.Add(new SqliteParameter(param.ParameterName, param.Value));
                }
                
                using var reader = await dataCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    spendings.Add(new SpendingWithCategory
                    {
                        Id = reader.GetInt32("Id"),
                        Date = DateTime.Parse(reader.GetString("Date")),
                        Amount = reader.GetDecimal("Amount"),
                        Description = reader.GetString("Description"),
                        CategoryId = reader.GetInt32("CategoryId"),
                        CategoryName = reader.GetString("CategoryName"),
                        CreatedAt = DateTime.Parse(reader.GetString("CreatedAt"))
                    });
                }
            }

            return new AdvancedSpendingSearchResult
            {
                Spendings = spendings,
                TotalCount = totalCount,
                TotalAmount = totalAmount,
                CategoryTotals = categoryTotals,
                HasMore = (skip + take) < totalCount
            };
        }

        #endregion

        #region Simplified Analytics Operations

        /// <summary>
        /// Calculates hero metrics for the simplified analytics dashboard
        /// </summary>
        public async Task<BudgetHealthMetrics> GetBudgetHealthMetricsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // Get summary data
                var summary = await GetBudgetSummaryAsync(startDate, endDate);
                
                // Get previous period for comparison (same duration before startDate)
                var periodDays = (endDate - startDate).Days;
                var previousStart = startDate.AddDays(-periodDays);
                var previousEnd = startDate.AddDays(-1);
                var previousSummary = await GetBudgetSummaryAsync(previousStart, previousEnd);
                
                // Calculate spending trend
                var spendingTrend = "Stable";
                if (summary.TotalSpending > previousSummary.TotalSpending * 1.1m)
                    spendingTrend = "Increasing";
                else if (summary.TotalSpending < previousSummary.TotalSpending * 0.9m)
                    spendingTrend = "Decreasing";

                // Get top category
                var spendingData = await GetSpendingWithCategoryAsync(startDate, endDate);
                var topCategory = spendingData
                    .GroupBy(s => s.CategoryName)
                    .OrderByDescending(g => g.Sum(s => s.Amount))
                    .FirstOrDefault();

                // Calculate health metrics
                var healthPercentage = summary.TotalIncome > 0 
                    ? Math.Max(0, (summary.RemainingBudget / summary.TotalIncome) * 100)
                    : 0;

                var healthStatus = healthPercentage switch
                {
                    >= 20 => "Excellent",
                    >= 10 => "Good", 
                    >= 0 => "Warning",
                    _ => "Critical"
                };

                // Calculate days left in period
                var daysLeft = Math.Max(0, (endDate - DateTime.Now).Days);

                return new BudgetHealthMetrics
                {
                    MonthlySpending = summary.TotalSpending,
                    BudgetRemaining = summary.RemainingBudget,
                    TopCategoryName = topCategory?.Key ?? "No data",
                    TopCategoryAmount = topCategory?.Sum(s => s.Amount) ?? 0,
                    BudgetHealthPercentage = healthPercentage,
                    HealthStatus = healthStatus,
                    SpendingTrend = spendingTrend,
                    DaysLeft = daysLeft
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetBudgetHealthMetricsAsync error: {ex.Message}");
                return new BudgetHealthMetrics { HealthStatus = "Critical" };
            }
        }

        /// <summary>
        /// Gets weekly spending patterns for simple bar chart display
        /// </summary>
        public async Task<IEnumerable<WeeklySpendingPattern>> GetWeeklySpendingPatternsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var spending = await GetSpendingAsync(startDate, endDate);
                
                // Group by week and calculate totals
                var weeklyData = spending
                    .GroupBy(s => GetWeekStart(s.Date))
                    .OrderByDescending(g => g.Key)
                    .Take(6)
                    .Select((g, index) => new WeeklySpendingPattern
                    {
                        WeekLabel = g.Key.ToString("MMM dd"),
                        Amount = g.Sum(s => s.Amount),
                        WeekNumber = index
                    })
                    .ToList();

                // Calculate normalized heights for bar chart (20-80 range)
                if (weeklyData.Any())
                {
                    var maxAmount = weeklyData.Max(w => w.Amount);
                    var avgAmount = weeklyData.Average(w => w.Amount);
                    
                    foreach (var week in weeklyData)
                    {
                        week.NormalizedHeight = maxAmount > 0 
                            ? Math.Max(20, Math.Min(80, (double)(week.Amount / maxAmount) * 60 + 20))
                            : 20;
                        week.IsHighSpendingWeek = week.Amount > avgAmount;
                    }
                }

                return weeklyData.OrderBy(w => w.WeekNumber);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetWeeklySpendingPatternsAsync error: {ex.Message}");
                return new List<WeeklySpendingPattern>();
            }
        }

        /// <summary>
        /// Generates plain English insights for user-friendly analytics
        /// </summary>
        public async Task<IEnumerable<BudgetInsight>> GenerateBudgetInsightsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var insights = new List<BudgetInsight>();
                var summary = await GetBudgetSummaryAsync(startDate, endDate);
                
                // Budget health insight
                if (summary.RemainingBudget < 0)
                {
                    insights.Add(new BudgetInsight
                    {
                        InsightType = "overspending",
                        Title = "Budget Alert",
                        Description = $"You've spent ${Math.Abs(summary.RemainingBudget):F0} more than your income this period.",
                        ActionRecommendation = "Consider reducing spending in your top categories.",
                        Priority = 1
                    });
                }
                else if (summary.RemainingBudget > summary.TotalIncome * 0.2m)
                {
                    insights.Add(new BudgetInsight
                    {
                        InsightType = "good_savings",
                        Title = "Great Job!",
                        Description = $"You have ${summary.RemainingBudget:F0} remaining in your budget.",
                        ActionRecommendation = "Consider saving or investing this amount.",
                        Priority = 2
                    });
                }

                // Top category insight
                var spendingData = await GetSpendingWithCategoryAsync(startDate, endDate);
                var topCategory = spendingData
                    .GroupBy(s => s.CategoryName)
                    .OrderByDescending(g => g.Sum(s => s.Amount))
                    .FirstOrDefault();

                if (topCategory != null)
                {
                    var categoryTotal = topCategory.Sum(s => s.Amount);
                    var categoryPercentage = summary.TotalSpending > 0 
                        ? (categoryTotal / summary.TotalSpending) * 100 
                        : 0;

                    insights.Add(new BudgetInsight
                    {
                        InsightType = "top_category",
                        Title = "Top Spending Category",
                        Description = $"{topCategory.Key} accounts for {categoryPercentage:F0}% of your spending (${categoryTotal:F0}).",
                        ActionRecommendation = categoryPercentage > 40 
                            ? "Consider ways to reduce spending in this category."
                            : "Your spending is well distributed across categories.",
                        Priority = 3
                    });
                }

                return insights.OrderBy(i => i.Priority).Take(3);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GenerateBudgetInsightsAsync error: {ex.Message}");
                return new List<BudgetInsight>();
            }
        }

        #region New Actionable Analytics Operations

        /// <summary>
        /// Gets quick stats for the top analytics bar
        /// </summary>
        public async Task<QuickStats> GetQuickStatsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var summary = await GetBudgetSummaryAsync(startDate, endDate);
                var categories = await GetCategoriesAsync();
                var spending = await GetSpendingAsync(startDate, endDate);
                
                // Calculate days left in period
                var daysLeft = Math.Max(0, (endDate - DateTime.Today).Days);
                
                // Calculate daily budget remaining
                var dailyBudgetRemaining = daysLeft > 0 ? summary.RemainingBudget / daysLeft : 0;
                
                // Calculate projected end balance
                var daysInPeriod = (endDate - startDate).Days + 1;
                var dailySpendingAverage = daysInPeriod > 0 ? summary.TotalSpending / daysInPeriod : 0;
                var projectedEndBalance = summary.RemainingBudget - (dailySpendingAverage * daysLeft);
                
                // Find best performing category
                var categoryPerformance = spending
                    .GroupBy(s => s.CategoryId)
                    .Select(g => new
                    {
                        CategoryId = g.Key,
                        CategoryName = categories.FirstOrDefault(c => c.Id == g.Key)?.Name ?? "Unknown",
                        TotalSpent = g.Sum(s => s.Amount),
                        // Assume budget of $500 per category for calculation - this should come from actual budget data
                        CategoryBudget = 500m
                    })
                    .Where(c => c.CategoryBudget > 0)
                    .Select(c => new
                    {
                        c.CategoryName,
                        UnderBudgetBy = c.CategoryBudget - c.TotalSpent,
                        Percentage = ((c.CategoryBudget - c.TotalSpent) / c.CategoryBudget) * 100
                    })
                    .Where(c => c.UnderBudgetBy > 0)
                    .OrderByDescending(c => c.Percentage)
                    .FirstOrDefault();
                
                return new QuickStats
                {
                    DaysLeft = daysLeft,
                    DailyBudgetRemaining = dailyBudgetRemaining,
                    ProjectedEndBalance = projectedEndBalance,
                    BestCategoryName = categoryPerformance?.CategoryName ?? "None",
                    BestCategoryPercentage = categoryPerformance?.Percentage ?? 0,
                    BestCategoryStatus = categoryPerformance != null 
                        ? $"under budget by {categoryPerformance.Percentage:F0}%" 
                        : "no categories under budget"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetQuickStatsAsync error: {ex.Message}");
                return new QuickStats();
            }
        }

        /// <summary>
        /// Gets monthly comparison data for the last specified months
        /// </summary>
        public async Task<IEnumerable<MonthlyComparison>> GetMonthlyComparisonAsync(int monthsBack = 6)
        {
            try
            {
                var comparisons = new List<MonthlyComparison>();
                
                for (int i = monthsBack - 1; i >= 0; i--)
                {
                    var monthStart = DateTime.Today.AddMonths(-i).Date.AddDays(1 - DateTime.Today.Day);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                    
                    var spending = await GetSpendingAsync(monthStart, monthEnd);
                    var totalSpending = spending.Sum(s => s.Amount);
                    
                    // Assume monthly budget of $2000 - this should come from actual budget settings
                    var budgetAmount = 2000m;
                    
                    comparisons.Add(new MonthlyComparison
                    {
                        MonthName = monthStart.ToString("MMM yyyy"),
                        TotalSpending = totalSpending,
                        BudgetAmount = budgetAmount,
                        Month = monthStart
                    });
                }
                
                return comparisons.OrderBy(c => c.Month);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetMonthlyComparisonAsync error: {ex.Message}");
                return new List<MonthlyComparison>();
            }
        }

        /// <summary>
        /// Gets spending velocity analysis
        /// </summary>
        public async Task<SpendingVelocity> GetSpendingVelocityAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var spending = await GetSpendingAsync(startDate, endDate);
                var daysInPeriod = (endDate - startDate).Days + 1;
                var dailySpendingAverage = daysInPeriod > 0 ? spending.Sum(s => s.Amount) / daysInPeriod : 0;
                
                // Assume daily budget target of $65 - this should come from actual budget settings
                var dailyBudgetTarget = 65m;
                
                var difference = dailySpendingAverage - dailyBudgetTarget;
                var isOverPace = dailySpendingAverage > dailyBudgetTarget;
                
                DateTime? projectedOverBudgetDate = null;
                if (isOverPace && difference > 0)
                {
                    var daysUntilOverBudget = (2000m / difference); // Assuming $2000 total budget
                    projectedOverBudgetDate = DateTime.Today.AddDays((double)daysUntilOverBudget);
                }
                
                var velocityMessage = $"You're spending ${dailySpendingAverage:F0}/day but budgeted for ${dailyBudgetTarget:F0}/day";
                var projectionMessage = projectedOverBudgetDate.HasValue 
                    ? $"At this rate, you'll exceed budget by {projectedOverBudgetDate:MMM dd}"
                    : "You're on track to stay within budget";
                
                return new SpendingVelocity
                {
                    DailySpendingAverage = dailySpendingAverage,
                    DailyBudgetTarget = dailyBudgetTarget,
                    ProjectedOverBudgetDate = projectedOverBudgetDate,
                    VelocityMessage = velocityMessage,
                    ProjectionMessage = projectionMessage
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetSpendingVelocityAsync error: {ex.Message}");
                return new SpendingVelocity();
            }
        }

        /// <summary>
        /// Gets category trend analysis for the specified months
        /// </summary>
        public async Task<IEnumerable<CategoryTrend>> GetCategoryTrendsAsync(int monthsBack = 3)
        {
            try
            {
                var trends = new List<CategoryTrend>();
                var categories = await GetCategoriesAsync();
                
                foreach (var category in categories.Where(c => c.IsActive))
                {
                    var monthlyAmounts = new List<decimal>();
                    
                    for (int i = monthsBack - 1; i >= 0; i--)
                    {
                        var monthStart = DateTime.Today.AddMonths(-i).Date.AddDays(1 - DateTime.Today.Day);
                        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                        
                        var spending = await GetSpendingAsync(monthStart, monthEnd);
                        var categoryAmount = spending.Where(s => s.CategoryId == category.Id).Sum(s => s.Amount);
                        monthlyAmounts.Add(categoryAmount);
                    }
                    
                    // Calculate month-over-month trend percentage (this month vs last month)
                    var trendPercentage = 0m;
                    if (monthlyAmounts.Count >= 2)
                    {
                        // Get the last two months for month-over-month comparison
                        var lastMonth = monthlyAmounts[monthlyAmounts.Count - 1];     // Current/most recent month
                        var previousMonth = monthlyAmounts[monthlyAmounts.Count - 2]; // Previous month
                        
                        if (previousMonth > 0)
                        {
                            // Normal month-over-month calculation: (this month - last month) / last month * 100
                            trendPercentage = ((lastMonth - previousMonth) / previousMonth) * 100;
                        }
                        else if (previousMonth == 0 && lastMonth > 0)
                        {
                            // Started spending in this category (no previous spending)
                            trendPercentage = 100; // New spending indicator
                        }
                        else if (previousMonth > 0 && lastMonth == 0)
                        {
                            // Stopped spending in this category
                            trendPercentage = -100; // No more spending
                        }
                        // If both months are 0, trend stays 0
                        
                        // Cap extreme values for realistic display
                        trendPercentage = Math.Max(-100, Math.Min(300, trendPercentage));
                    }
                    
                    // Generate sparkline pattern
                    var maxAmount = monthlyAmounts.Max();
                    var sparkline = string.Join("", monthlyAmounts.Select(amount =>
                        maxAmount > 0 ? (amount / maxAmount) switch
                        {
                            <= 0.33m => "▂",
                            <= 0.66m => "▄",
                            _ => "█"
                        } : "▂"
                    ));
                    
                    var direction = trendPercentage switch
                    {
                        > 5 => "↑",
                        < -5 => "↓",
                        _ => "→"
                    };
                    
                    trends.Add(new CategoryTrend
                    {
                        CategoryName = category.Name,
                        Last3MonthsAmounts = monthlyAmounts,
                        TrendPercentage = trendPercentage,
                        TrendDirection = direction,
                        SparklinePattern = sparkline
                    });
                }
                
                return trends.OrderByDescending(t => Math.Abs(t.TrendPercentage));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCategoryTrendsAsync error: {ex.Message}");
                return new List<CategoryTrend>();
            }
        }

        /// <summary>
        /// Gets calendar heatmap data for the specified period
        /// </summary>
        public async Task<IEnumerable<CalendarHeatmapDay>> GetCalendarHeatmapAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var heatmapDays = new List<CalendarHeatmapDay>();
                var spending = await GetSpendingAsync(startDate, endDate);
                
                // Assume daily budget target of $65
                var dailyBudgetTarget = 65m;
                
                for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                {
                    var daySpending = spending.Where(s => s.Date.Date == date).Sum(s => s.Amount);
                    
                    heatmapDays.Add(new CalendarHeatmapDay
                    {
                        Date = date,
                        SpentAmount = daySpending,
                        DailyBudgetTarget = dailyBudgetTarget
                    });
                }
                
                return heatmapDays;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCalendarHeatmapAsync error: {ex.Message}");
                return new List<CalendarHeatmapDay>();
            }
        }

        /// <summary>
        /// Gets budget performance score analysis
        /// </summary>
        public async Task<BudgetPerformanceScore> GetBudgetPerformanceScoreAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var summary = await GetBudgetSummaryAsync(startDate, endDate);
                var spending = await GetSpendingAsync(startDate, endDate);
                var categories = await GetCategoriesAsync();
                
                var factors = new List<PerformanceFactor>();
                var score = 50; // Base score
                
                // Factor 1: Staying under budget
                if (summary.RemainingBudget >= 0)
                {
                    var budgetFactor = Math.Min(30, (int)(summary.RemainingBudget / 100));
                    factors.Add(new PerformanceFactor
                    {
                        FactorName = "Staying under budget",
                        Impact = budgetFactor,
                        Description = $"Remaining budget: ${summary.RemainingBudget:F0}"
                    });
                    score += budgetFactor;
                }
                else
                {
                    var overBudgetPenalty = Math.Max(-30, (int)(summary.RemainingBudget / 100));
                    factors.Add(new PerformanceFactor
                    {
                        FactorName = "Over budget",
                        Impact = overBudgetPenalty,
                        Description = $"Over budget by: ${Math.Abs(summary.RemainingBudget):F0}"
                    });
                    score += overBudgetPenalty;
                }
                
                // Factor 2: Category distribution
                var categorySpending = spending.GroupBy(s => s.CategoryId).Count();
                if (categorySpending <= 2)
                {
                    factors.Add(new PerformanceFactor
                    {
                        FactorName = "Limited category use",
                        Impact = -10,
                        Description = "Consider using more categories for better tracking"
                    });
                    score -= 10;
                }
                
                // Factor 3: "Others" category usage
                var othersCategory = categories.FirstOrDefault(c => c.Name.ToLower().Contains("other"));
                if (othersCategory != null)
                {
                    var othersSpending = spending.Where(s => s.CategoryId == othersCategory.Id).Sum(s => s.Amount);
                    var othersPercentage = summary.TotalSpending > 0 ? (othersSpending / summary.TotalSpending) * 100 : 0;
                    
                    if (othersPercentage > 30)
                    {
                        factors.Add(new PerformanceFactor
                        {
                            FactorName = "High 'others' spending",
                            Impact = -15,
                            Description = $"{othersPercentage:F0}% in 'others' category"
                        });
                        score -= 15;
                    }
                }
                
                // Ensure score is within bounds
                score = Math.Max(0, Math.Min(100, score));
                
                return new BudgetPerformanceScore
                {
                    OverallScore = score,
                    Factors = factors
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetBudgetPerformanceScoreAsync error: {ex.Message}");
                return new BudgetPerformanceScore { OverallScore = 50 };
            }
        }

        /// <summary>
        /// Gets actionable category insights
        /// </summary>
        public async Task<IEnumerable<CategoryInsight>> GetCategoryInsightsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var insights = new List<CategoryInsight>();
                var spending = await GetSpendingAsync(startDate, endDate);
                var categories = await GetCategoriesAsync();
                
                // Get previous period for comparison
                var periodLength = (endDate - startDate).Days + 1;
                var previousStart = startDate.AddDays(-periodLength);
                var previousEnd = startDate.AddDays(-1);
                var previousSpending = await GetSpendingAsync(previousStart, previousEnd);
                
                // Analyze each category
                foreach (var category in categories.Where(c => c.IsActive))
                {
                    var currentAmount = spending.Where(s => s.CategoryId == category.Id).Sum(s => s.Amount);
                    var previousAmount = previousSpending.Where(s => s.CategoryId == category.Id).Sum(s => s.Amount);
                    
                    // Skip if no spending in either period
                    if (currentAmount == 0 && previousAmount == 0) continue;
                    
                    // Check for significant increases
                    if (previousAmount > 0)
                    {
                        var changePercentage = ((currentAmount / previousAmount) - 1) * 100;
                        
                        if (changePercentage > 45) // Significant increase
                        {
                            insights.Add(new CategoryInsight
                            {
                                InsightType = "increase",
                                Title = "Spending Increase Alert",
                                Description = $"{category.Name} spending up {changePercentage:F0}% vs last period",
                                ActionRecommendation = $"Review {category.Name} expenses and consider setting a lower limit",
                                RelevantAmount = currentAmount - previousAmount,
                                PercentageChange = changePercentage,
                                CategoryName = category.Name,
                                Priority = 1
                            });
                        }
                    }
                    else if (currentAmount > 100) // New category with significant spending
                    {
                        insights.Add(new CategoryInsight
                        {
                            InsightType = "new_category",
                            Title = "New Spending Detected",
                            Description = $"{category.Name} spending detected - is this a new category?",
                            ActionRecommendation = "Consider if this should be tracked separately or merged with existing categories",
                            RelevantAmount = currentAmount,
                            CategoryName = category.Name,
                            Priority = 2
                        });
                    }
                }
                
                // Check for savings opportunities
                var totalSpending = spending.Sum(s => s.Amount);
                var othersSpending = spending.Where(s => 
                    categories.Any(c => c.Id == s.CategoryId && c.Name.ToLower().Contains("other")))
                    .Sum(s => s.Amount);
                
                if (othersSpending > totalSpending * 0.1m) // More than 10% in "others"
                {
                    var savingsAmount = othersSpending * 0.1m;
                    insights.Add(new CategoryInsight
                    {
                        InsightType = "savings",
                        Title = "Savings Opportunity",
                        Description = $"Reduce 'others' spending by 10% to save ${savingsAmount:F0}/month",
                        ActionRecommendation = "Review 'others' transactions and categorize them properly",
                        RelevantAmount = savingsAmount,
                        CategoryName = "Others",
                        Priority = 3
                    });
                }
                
                return insights.OrderBy(i => i.Priority).Take(3); // Return top 3 insights
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCategoryInsightsAsync error: {ex.Message}");
                return new List<CategoryInsight>();
            }
        }

        #endregion

        /// <summary>
        /// Helper method to get the start of the week (Monday) for a given date
        /// </summary>
        private static DateTime GetWeekStart(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }

        #endregion
    }
}