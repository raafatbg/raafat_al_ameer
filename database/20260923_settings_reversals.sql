-- Run after a verified backup. Idempotent, preserves all existing ledger rows.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.CustomerPayments', 'ReversedAt') IS NULL
BEGIN
    ALTER TABLE dbo.CustomerPayments ADD ReversedAt datetime2(0) NULL,
        ReversalReason nvarchar(400) NULL;
END;
IF COL_LENGTH('dbo.EmployeeSalaryEntries', 'ReversedAt') IS NULL
BEGIN
    ALTER TABLE dbo.EmployeeSalaryEntries ADD ReversedAt datetime2(0) NULL,
        ReversalReason nvarchar(400) NULL;
END;
COMMIT TRANSACTION;
