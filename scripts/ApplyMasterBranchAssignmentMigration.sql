-- Run this while ThreadFlow_Master is selected in SSMS.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.AspNetUsers', N'BranchId') IS NULL
    ALTER TABLE dbo.AspNetUsers ADD BranchId uniqueidentifier NULL;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20261122130000_AddPlatformUserBranchAssignment'
)
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20261122130000_AddPlatformUserBranchAssignment', N'10.0.14');

COMMIT TRANSACTION;
