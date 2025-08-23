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

        #region Export Operations

        public async Task ExportDataAsync(DateTime startDate, DateTime endDate, string? filePath = null)
        {
            var csvContent = await ExportToCsvAsync(startDate, endDate);
            
            if (string.IsNullOrEmpty(filePath))
            {
                var fileName = $"budget_export_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv";
                filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
            }

            await File.WriteAllTextAsync(filePath, csvContent, Encoding.UTF8);
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