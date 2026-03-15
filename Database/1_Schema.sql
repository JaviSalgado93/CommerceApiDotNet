/****** 
Script: CommerceAPI - Complete Authentication & Commerce Database Setup
Creado: 2026-03-14
Descripcion: Fusiona autenticacion + tablas de comerciantes con Clean Architecture
Nota: Ejecutar en SQL Server Management Studio
Base de datos: CommerceApiDotNet (local) o CommerceApiDotNetAzure (Azure)
******/

-- =====================================================
-- 00. DATABASE CREATION
-- =====================================================
-- Para uso local
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'CommerceApiDotNet')
BEGIN
    CREATE DATABASE CommerceApiDotNet;
    PRINT 'Database CommerceApiDotNet created successfully';
END
ELSE
BEGIN
    PRINT 'Database CommerceApiDotNet already exists';
END

GO
-- //

USE CommerceApiDotNet;

-- =====================================================
-- 01. DROP TABLES (Si existen - para reintentos sin errores)
-- =====================================================
PRINT '========== DROPPING EXISTING TABLES (IF EXISTS) ==========';

IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Establishments')
    DROP TABLE [dbo].[Establishments];

IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Merchants')
    DROP TABLE [dbo].[Merchants];

IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='TokenBlacklist')
    DROP TABLE [dbo].[TokenBlacklist];

IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='RefreshTokens')
    DROP TABLE [dbo].[RefreshTokens];

IF EXISTS (SELECT * FROM sys.objects WHERE type='U' AND name='Users')
    DROP TABLE [dbo].[Users];

PRINT 'Previous tables dropped (if existed)';

-- =====================================================
-- 02. CREATE AUTHENTICATION TABLES
-- =====================================================
PRINT '========== CREATING AUTHENTICATION TABLES ==========';

-- Tabla: Users (Autenticación)
CREATE TABLE [dbo].[Users] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [Username] NVARCHAR(50) NOT NULL UNIQUE,
    [Email] NVARCHAR(100) NOT NULL UNIQUE,
    [PasswordHash] NVARCHAR(255) NOT NULL,
    [FirstName] NVARCHAR(50) NOT NULL,
    [LastName] NVARCHAR(50) NOT NULL,
    [Role] NVARCHAR(20) NOT NULL DEFAULT 'User', -- 'Admin' | 'Administrador' | 'Auxiliar de Registro' | 'User'
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedBy] NVARCHAR(100) NULL,
    [LastAccess] DATETIME2 NULL,
    [FailedAttempts] INT NOT NULL DEFAULT 0,
    [LockedUntil] DATETIME2 NULL
);
PRINT 'Table Users created';

-- Tabla: RefreshTokens
CREATE TABLE [dbo].[RefreshTokens] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [Token] NVARCHAR(500) NOT NULL UNIQUE,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ExpiresAt] DATETIME2 NOT NULL,
    [IsRevoked] BIT NOT NULL DEFAULT 0,
    [RevokedAt] DATETIME2 NULL,
    [ReplacedBy] NVARCHAR(500) NULL,
    FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
);
PRINT 'Table RefreshTokens created';

-- Tabla: TokenBlacklist (Revocación de tokens)
CREATE TABLE [dbo].[TokenBlacklist] (
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [TokenHash] NVARCHAR(512) NOT NULL UNIQUE,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [RevokedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ExpiresAt] DATETIME2 NOT NULL,
    [Reason] NVARCHAR(50) NOT NULL DEFAULT 'Manual revocation',
    FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
);
PRINT 'Table TokenBlacklist created';

-- =====================================================
-- 03. CREATE COMMERCE TABLES
-- =====================================================
PRINT '========== CREATING COMMERCE TABLES ==========';

-- Tabla: Merchants (Comerciantes)
CREATE TABLE [dbo].[Merchants] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [Name] NVARCHAR(200) NOT NULL,
    [Municipality] NVARCHAR(100) NOT NULL,
    [Phone] NVARCHAR(20),
    [Email] NVARCHAR(100),
    [Status] NVARCHAR(20) DEFAULT 'Activo', -- 'Activo' | 'Inactivo'
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedBy] NVARCHAR(100) NULL,
    [CreatedByUserId] UNIQUEIDENTIFIER NULL, -- Referencia al usuario que creó el registro
    FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users]([Id])
);
PRINT 'Table Merchants created';

-- Tabla: Establishments (Establecimientos)
CREATE TABLE [dbo].[Establishments] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [MerchantId] INT NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Revenue] DECIMAL(15, 2) NOT NULL,
    [EmployeeCount] INT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedBy] NVARCHAR(100) NULL,
    FOREIGN KEY ([MerchantId]) REFERENCES [dbo].[Merchants]([Id]) ON DELETE CASCADE
);
PRINT 'Table Establishments created';

-- =====================================================
-- 04. CREATE INDEXES FOR PERFORMANCE
-- =====================================================
PRINT '========== CREATING INDEXES ==========';

-- Users Indexes
CREATE INDEX IX_Users_Username ON [dbo].[Users] ([Username]);
CREATE INDEX IX_Users_Email ON [dbo].[Users] ([Email]);
CREATE INDEX IX_Users_IsActive ON [dbo].[Users] ([IsActive]);
PRINT 'Users indexes created';

-- RefreshTokens Indexes
CREATE INDEX IX_RefreshTokens_Token ON [dbo].[RefreshTokens] ([Token]);
CREATE INDEX IX_RefreshTokens_UserId ON [dbo].[RefreshTokens] ([UserId]);
CREATE INDEX IX_RefreshTokens_ExpiresAt ON [dbo].[RefreshTokens] ([ExpiresAt]);
PRINT 'RefreshTokens indexes created';

-- TokenBlacklist Indexes
CREATE INDEX IX_TokenBlacklist_TokenHash ON [dbo].[TokenBlacklist] ([TokenHash]);
CREATE INDEX IX_TokenBlacklist_UserId ON [dbo].[TokenBlacklist] ([UserId]);
CREATE INDEX IX_TokenBlacklist_ExpiresAt ON [dbo].[TokenBlacklist] ([ExpiresAt]);
PRINT 'TokenBlacklist indexes created';

-- Merchants Indexes
CREATE INDEX IX_Merchants_Status ON [dbo].[Merchants] ([Status]);
CREATE INDEX IX_Merchants_Municipality ON [dbo].[Merchants] ([Municipality]);
CREATE INDEX IX_Merchants_CreatedByUserId ON [dbo].[Merchants] ([CreatedByUserId]);
PRINT 'Merchants indexes created';

-- Establishments Indexes
CREATE INDEX IX_Establishments_MerchantId ON [dbo].[Establishments] ([MerchantId]);
PRINT 'Establishments indexes created';

-- =====================================================
-- 05. CREATE TRIGGERS FOR AUDIT
-- =====================================================
PRINT '========== CREATING TRIGGERS ==========';

GO
-- //

-- Trigger para Users (INSTEAD OF UPDATE)
CREATE TRIGGER [dbo].[tr_Users_Update]
ON [dbo].[Users]
INSTEAD OF UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE [dbo].[Users]
    SET 
        [Username] = inserted.[Username],
        [Email] = inserted.[Email],
        [PasswordHash] = inserted.[PasswordHash],
        [FirstName] = inserted.[FirstName],
        [LastName] = inserted.[LastName],
        [Role] = inserted.[Role],
        [IsActive] = inserted.[IsActive],
        [UpdatedAt] = GETUTCDATE(),
        [UpdatedBy] = inserted.[UpdatedBy],
        [LastAccess] = inserted.[LastAccess],
        [FailedAttempts] = inserted.[FailedAttempts],
        [LockedUntil] = inserted.[LockedUntil]
    FROM [dbo].[Users]
    INNER JOIN inserted ON [dbo].[Users].[Id] = inserted.[Id];
END;

PRINT 'Trigger [dbo].[tr_Users_Update] created';

GO

-- Trigger para Merchants (INSTEAD OF INSERT)
CREATE TRIGGER [dbo].[tr_Merchants_Insert]
ON [dbo].[Merchants]
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [dbo].[Merchants] 
    ([Name], [Municipality], [Phone], [Email], [Status], [CreatedAt], [UpdatedAt], [CreatedByUserId], [UpdatedBy])
    SELECT 
        [Name], [Municipality], [Phone], [Email], [Status], 
        GETUTCDATE(), GETUTCDATE(), [CreatedByUserId], [UpdatedBy]
    FROM inserted;
END;

PRINT 'Trigger [dbo].[tr_Merchants_Insert] created';

GO

-- Trigger para Merchants (INSTEAD OF UPDATE)
CREATE TRIGGER [dbo].[tr_Merchants_Update]
ON [dbo].[Merchants]
INSTEAD OF UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE [dbo].[Merchants]
    SET 
        [Name] = inserted.[Name],
        [Municipality] = inserted.[Municipality],
        [Phone] = inserted.[Phone],
        [Email] = inserted.[Email],
        [Status] = inserted.[Status],
        [UpdatedAt] = GETUTCDATE(),
        [UpdatedBy] = inserted.[UpdatedBy]
    FROM [dbo].[Merchants]
    INNER JOIN inserted ON [dbo].[Merchants].[Id] = inserted.[Id];
END;

PRINT 'Trigger [dbo].[tr_Merchants_Update] created';

GO

-- Trigger para Establishments (INSTEAD OF INSERT)
CREATE TRIGGER [dbo].[tr_Establishments_Insert]
ON [dbo].[Establishments]
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [dbo].[Establishments] 
    ([MerchantId], [Name], [Revenue], [EmployeeCount], [CreatedAt], [UpdatedAt], [UpdatedBy])
    SELECT 
        [MerchantId], [Name], [Revenue], [EmployeeCount], 
        GETUTCDATE(), GETUTCDATE(), [UpdatedBy]
    FROM inserted;
END;

PRINT 'Trigger [dbo].[tr_Establishments_Insert] created';

GO

-- Trigger para Establishments (INSTEAD OF UPDATE)
CREATE TRIGGER [dbo].[tr_Establishments_Update]
ON [dbo].[Establishments]
INSTEAD OF UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE [dbo].[Establishments]
    SET 
        [MerchantId] = inserted.[MerchantId],
        [Name] = inserted.[Name],
        [Revenue] = inserted.[Revenue],
        [EmployeeCount] = inserted.[EmployeeCount],
        [UpdatedAt] = GETUTCDATE(),
        [UpdatedBy] = inserted.[UpdatedBy]
    FROM [dbo].[Establishments]
    INNER JOIN inserted ON [dbo].[Establishments].[Id] = inserted.[Id];
END;

PRINT 'Trigger [dbo].[tr_Establishments_Update] created';
GO

PRINT '========== ALL TRIGGERS RECREATED SUCCESSFULLY ==========';
GO

-- =====================================================
-- 06. CREATE FUNCTIONS / STORED PROCEDURES
-- =====================================================
PRINT '========== CREATING FUNCTIONS ==========';

GO
-- //

-- Function: Get Active Merchants Report (RETO 04)
CREATE FUNCTION [dbo].[fn_GetActiveMerchantsReport]()
RETURNS TABLE
AS
RETURN (
    SELECT 
        m.[Id],
        m.[Name] AS 'Nombre o Razón Social',
        m.[Municipality] AS Municipio,
        m.[Phone] AS Teléfono,
        m.[Email] AS 'Correo Electrónico',
        m.[CreatedAt] AS 'Fecha de Registro',
        m.[Status] AS Estado,
        COUNT(e.[Id]) AS 'Cantidad de Establecimientos',
        ISNULL(SUM(e.[Revenue]), 0) AS 'Total Ingresos',
        ISNULL(SUM(e.[EmployeeCount]), 0) AS 'Cantidad de Empleados'
    FROM [dbo].[Merchants] m
    LEFT JOIN [dbo].[Establishments] e ON m.[Id] = e.[MerchantId]
    WHERE m.[Status] = 'Activo'
    GROUP BY m.[Id], m.[Name], m.[Municipality], m.[Phone], m.[Email], m.[CreatedAt], m.[Status]
);
GO

PRINT 'Function fn_GetActiveMerchantsReport created';

GO
-- //

-- =====================================================
-- 07. INSERT DEFAULT ADMIN USER
-- =====================================================
PRINT '========== INSERTING DEFAULT DATA ==========';

IF NOT EXISTS (SELECT * FROM [dbo].[Users] WHERE [Username] = 'admin')
BEGIN
    INSERT INTO [dbo].[Users] 
    ([Id], [Username], [Email], [PasswordHash], [FirstName], [LastName], [Role], [IsActive])
    VALUES 
    (NEWID(), 'admin', 'admin@commerce-api.com', 
    '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewKyNvreop4XUPR2', -- Password: Admin123! (BCrypt)
    'Administrador', 'Sistema', 'Administrador', 1);
    
    PRINT 'Admin user created successfully';
END
ELSE
BEGIN
    PRINT 'Admin user already exists';
END

GO
-- //

-- =====================================================
-- 08. VERIFICATION & FINAL VALIDATION
-- =====================================================
PRINT '';
PRINT '========== VERIFICATION RESULTS ==========';
PRINT '';

PRINT 'Database Objects Created:';
SELECT 
    TABLE_NAME,
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = t.TABLE_NAME) AS ColumnCount
FROM INFORMATION_SCHEMA.TABLES t
WHERE TABLE_SCHEMA = 'dbo'
ORDER BY TABLE_NAME;

PRINT '';
PRINT 'Row Count per Table:';
SELECT 'Users' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[Users]
UNION ALL
SELECT 'RefreshTokens' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[RefreshTokens]
UNION ALL
SELECT 'TokenBlacklist' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[TokenBlacklist]
UNION ALL
SELECT 'Merchants' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[Merchants]
UNION ALL
SELECT 'Establishments' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[Establishments];

PRINT '';
PRINT 'Admin User Details:';
SELECT 
    [Id],
    [Username],
    [Email],
    [FirstName],
    [LastName],
    [Role],
    [IsActive],
    [CreatedAt]
FROM [dbo].[Users]
WHERE [Username] = 'admin';

PRINT '';
PRINT '========== DATABASE SETUP COMPLETED SUCCESSFULLY ==========';
PRINT '';
PRINT 'Next Steps:';
PRINT '1. Run: 1_Insert_Seed_Data.sql (to populate test data)';
PRINT '2. Verify data with: SELECT * FROM fn_GetActiveMerchantsReport();';
PRINT '3. Connect from your .NET application using Entity Framework Core';
PRINT '';