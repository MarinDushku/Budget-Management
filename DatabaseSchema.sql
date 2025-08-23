-- SQLite Database Schema for Budget Management Application
-- File: DatabaseSchema.sql

PRAGMA foreign_keys = ON;

-- Income entries table
CREATE TABLE Income (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Date DATE NOT NULL,
    Amount DECIMAL(10,2) NOT NULL CHECK (Amount >= 0),
    Description TEXT NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Spending categories lookup table
CREATE TABLE Categories (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    DisplayOrder INTEGER NOT NULL DEFAULT 0,
    IsActive BOOLEAN DEFAULT 1
);

-- Insert default categories
INSERT INTO Categories (Name, DisplayOrder, IsActive) VALUES 
    ('Family', 1, 1),
    ('Personal', 2, 1),
    ('Marini', 3, 1);

-- Spending entries table
CREATE TABLE Spending (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Date DATE NOT NULL,
    Amount DECIMAL(10,2) NOT NULL CHECK (Amount >= 0),
    Description TEXT NOT NULL,
    CategoryId INTEGER NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE RESTRICT
);

-- Application settings table
CREATE TABLE AppSettings (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Insert default settings
INSERT INTO AppSettings (Key, Value) VALUES 
    ('FontSize', '14'),
    ('Theme', 'Light'),
    ('CurrencySymbol', '$'),
    ('DateFormat', 'MM/dd/yyyy');

-- Indexes for performance
CREATE INDEX idx_income_date ON Income(Date DESC);
CREATE INDEX idx_spending_date ON Spending(Date DESC);
CREATE INDEX idx_spending_category ON Spending(CategoryId);

-- Triggers for UpdatedAt timestamps
CREATE TRIGGER update_income_timestamp 
    AFTER UPDATE ON Income
    FOR EACH ROW
BEGIN
    UPDATE Income SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = NEW.Id;
END;

CREATE TRIGGER update_spending_timestamp 
    AFTER UPDATE ON Spending
    FOR EACH ROW
BEGIN
    UPDATE Spending SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = NEW.Id;
END;

-- Views for common queries
CREATE VIEW v_spending_with_category AS
SELECT 
    s.Id,
    s.Date,
    s.Amount,
    s.Description,
    s.CategoryId,
    c.Name as CategoryName,
    s.CreatedAt,
    s.UpdatedAt
FROM Spending s
JOIN Categories c ON s.CategoryId = c.Id
WHERE c.IsActive = 1;

CREATE VIEW v_monthly_summary AS
SELECT 
    strftime('%Y-%m', Date) as Month,
    'Income' as Type,
    NULL as CategoryName,
    SUM(Amount) as TotalAmount
FROM Income
GROUP BY strftime('%Y-%m', Date)
UNION ALL
SELECT 
    strftime('%Y-%m', s.Date) as Month,
    'Spending' as Type,
    c.Name as CategoryName,
    SUM(s.Amount) as TotalAmount
FROM Spending s
JOIN Categories c ON s.CategoryId = c.Id
WHERE c.IsActive = 1
GROUP BY strftime('%Y-%m', s.Date), c.Name;