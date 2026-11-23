-- Run this once in every tenant database (each ThreadFlow_* business database).
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.CustomerInteractions', N'BranchId') IS NULL
    ALTER TABLE dbo.CustomerInteractions ADD BranchId uniqueidentifier NULL;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.CustomerInteractions')
      AND name = N'IX_CustomerInteractions_BranchId'
)
    CREATE INDEX IX_CustomerInteractions_BranchId
        ON dbo.CustomerInteractions (BranchId);

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_CustomerInteractions_Branches_BranchId'
      AND parent_object_id = OBJECT_ID(N'dbo.CustomerInteractions')
)
    ALTER TABLE dbo.CustomerInteractions
        ADD CONSTRAINT FK_CustomerInteractions_Branches_BranchId
        FOREIGN KEY (BranchId) REFERENCES dbo.Branches (Id)
        ON DELETE SET NULL;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20261122130100_AddBranchIdToCustomerInteractions'
)
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20261122130100_AddBranchIdToCustomerInteractions', N'10.0.14');

COMMIT TRANSACTION;
