-- Apply only after a verified full backup. Preserves existing sales and their balances.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Sales', 'IsVoided') IS NULL
    ALTER TABLE dbo.Sales ADD IsVoided bit NOT NULL
        CONSTRAINT DF_Sales_IsVoided DEFAULT (0);
IF COL_LENGTH('dbo.Sales', 'VoidedAt') IS NULL
    ALTER TABLE dbo.Sales ADD VoidedAt datetime2(0) NULL;

IF OBJECT_ID(N'dbo.SaleTenderReceipts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SaleTenderReceipts
    (
        ReceiptId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SaleTenderReceipts PRIMARY KEY,
        SaleId int NOT NULL CONSTRAINT UQ_SaleTenderReceipts_SaleId UNIQUE,
        ReceiptNumber nvarchar(30) NOT NULL CONSTRAINT UQ_SaleTenderReceipts_Number UNIQUE,
        ReceivedLBP decimal(18,2) NOT NULL,
        ReceivedUSD decimal(18,2) NOT NULL,
        LbpPerUsd decimal(18,4) NOT NULL,
        TenderTotalLBP decimal(18,2) NOT NULL,
        AppliedLBP decimal(18,2) NOT NULL,
        ChangeLBP decimal(18,2) NOT NULL,
        ChangeUSD decimal(18,2) NOT NULL,
        PaymentMethod nvarchar(20) NOT NULL,
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_SaleTenderReceipts_CreatedAt DEFAULT SYSDATETIME(),
        CONSTRAINT FK_SaleTenderReceipts_Sales FOREIGN KEY (SaleId) REFERENCES dbo.Sales(SaleId),
        CONSTRAINT CK_SaleTenderReceipts_Amounts CHECK
            (ReceivedLBP >= 0 AND ReceivedUSD >= 0 AND LbpPerUsd > 0 AND
             TenderTotalLBP >= 0 AND AppliedLBP >= 0 AND ChangeLBP >= 0 AND ChangeUSD >= 0 AND
             TenderTotalLBP = AppliedLBP + ChangeLBP)
    );
END;

COMMIT TRANSACTION;
