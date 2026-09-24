-- Apply only after a verified full backup. Existing expenses and inventory are preserved.
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Expenses', 'SalaryEntryId') IS NULL
    ALTER TABLE dbo.Expenses ADD SalaryEntryId int NULL;
IF COL_LENGTH('dbo.Expenses', 'ReversedAt') IS NULL
    ALTER TABLE dbo.Expenses ADD ReversedAt datetime2(0) NULL;
IF COL_LENGTH('dbo.Expenses', 'ReversalReason') IS NULL
    ALTER TABLE dbo.Expenses ADD ReversalReason nvarchar(400) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_Expenses_SalaryEntry')
    ALTER TABLE dbo.Expenses ADD CONSTRAINT FK_Expenses_SalaryEntry
        FOREIGN KEY (SalaryEntryId) REFERENCES dbo.EmployeeSalaryEntries(SalaryEntryId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_Expenses_SalaryEntry' AND object_id=OBJECT_ID('dbo.Expenses'))
    CREATE UNIQUE INDEX UX_Expenses_SalaryEntry ON dbo.Expenses(SalaryEntryId) WHERE SalaryEntryId IS NOT NULL;

IF OBJECT_ID(N'dbo.InventoryAdjustments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryAdjustments
    (
        AdjustmentId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryAdjustments PRIMARY KEY,
        ProductId int NOT NULL,
        QuantityChange int NOT NULL,
        StockBefore int NOT NULL,
        StockAfter int NOT NULL,
        Reason nvarchar(400) NOT NULL,
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_InventoryAdjustments_CreatedAt DEFAULT SYSDATETIME(),
        CONSTRAINT FK_InventoryAdjustments_Product FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId),
        CONSTRAINT CK_InventoryAdjustments_Quantity CHECK
            (QuantityChange <> 0 AND StockBefore >= 0 AND StockAfter >= 0 AND StockAfter = StockBefore + QuantityChange),
        CONSTRAINT CK_InventoryAdjustments_Reason CHECK (LEN(LTRIM(RTRIM(Reason))) > 0)
    );
    CREATE INDEX IX_InventoryAdjustments_ProductDate ON dbo.InventoryAdjustments(ProductId,CreatedAt DESC);
END;

COMMIT TRANSACTION;
