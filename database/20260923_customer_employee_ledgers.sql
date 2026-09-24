-- Apply only after a verified full backup. Existing rows are left unchanged.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.CustomerPayments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerPayments
    (
        CustomerPaymentId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CustomerPayments PRIMARY KEY,
        SaleId int NOT NULL,
        CustomerId int NOT NULL,
        Amount decimal(18,2) NOT NULL,
        PaymentDate datetime2(0) NOT NULL CONSTRAINT DF_CustomerPayments_PaymentDate DEFAULT SYSDATETIME(),
        PaymentMethod nvarchar(40) NOT NULL,
        Notes nvarchar(400) NULL,
        CONSTRAINT CK_CustomerPayments_Amount CHECK (Amount > 0),
        CONSTRAINT FK_CustomerPayments_Sales FOREIGN KEY (SaleId) REFERENCES dbo.Sales(SaleId),
        CONSTRAINT FK_CustomerPayments_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId)
    );
    CREATE INDEX IX_CustomerPayments_SaleDate ON dbo.CustomerPayments(SaleId, PaymentDate);
    CREATE INDEX IX_CustomerPayments_CustomerDate ON dbo.CustomerPayments(CustomerId, PaymentDate);
END;

IF OBJECT_ID(N'dbo.Employees', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Employees
    (
        EmployeeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Employees PRIMARY KEY,
        FullName nvarchar(100) NOT NULL,
        Phone nvarchar(40) NULL,
        JobTitle nvarchar(100) NULL,
        HireDate date NOT NULL,
        MonthlySalary decimal(18,2) NOT NULL CONSTRAINT DF_Employees_MonthlySalary DEFAULT (0),
        IsActive bit NOT NULL CONSTRAINT DF_Employees_IsActive DEFAULT (1),
        CONSTRAINT CK_Employees_MonthlySalary CHECK (MonthlySalary >= 0)
    );
END;

IF OBJECT_ID(N'dbo.EmployeeSalaryEntries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmployeeSalaryEntries
    (
        SalaryEntryId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmployeeSalaryEntries PRIMARY KEY,
        EmployeeId int NOT NULL,
        EntryDate date NOT NULL,
        EntryType nvarchar(10) NOT NULL,
        Amount decimal(18,2) NOT NULL,
        Notes nvarchar(400) NULL,
        CONSTRAINT CK_EmployeeSalaryEntries_Type CHECK (EntryType IN (N'Due', N'Payment')),
        CONSTRAINT CK_EmployeeSalaryEntries_Amount CHECK (Amount > 0),
        CONSTRAINT FK_EmployeeSalaryEntries_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(EmployeeId)
    );
    CREATE INDEX IX_EmployeeSalaryEntries_EmployeeDate ON dbo.EmployeeSalaryEntries(EmployeeId, EntryDate, SalaryEntryId);
END;

COMMIT TRANSACTION;
