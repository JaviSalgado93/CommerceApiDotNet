/****** 
Script: CommerceAPI - Seed Data
Creado: 2026-03-14
Descripción: Inserta datos de prueba: Departamentos, Municipios, Roles, Usuarios, Comerciantes y Establecimientos
Base de datos: CommerceApiDotNet
Nota: Ejecutar DESPUÉS de 1_Schema.sql
******/

USE CommerceApiDotNet;

BEGIN TRANSACTION;

BEGIN TRY
    PRINT '========== INICIANDO INSERCIÓN DE DATOS ==========';

    -- =====================================================
    -- 01. INSERT DEPARTMENTS (5 departamentos)
    -- =====================================================
    PRINT '--- INSERTING DEPARTMENTS ---';

    INSERT INTO [dbo].[Departments] ([Code], [Name], [Region])
    VALUES
    ('25', 'CUNDINAMARCA', 'ANDINA'),
    ('05', 'ANTIOQUIA', 'NOROCCIDENTE'),
    ('76', 'VALLE DEL CAUCA', 'PACÍFICA'),
    ('08', 'ATLÁNTICO', 'CARIBE'),
    ('13', 'BOLÍVAR', 'CARIBE');

    PRINT '5 Departamentos insertados correctamente';

    -- =====================================================
    -- 02. INSERT MUNICIPALITIES (5 municipios)
    -- =====================================================
    PRINT '--- INSERTING MUNICIPALITIES ---';

    INSERT INTO [dbo].[Municipalities] ([Code], [Name], [DepartmentId])
    SELECT '25001', 'BOGOTÁ', [Id] FROM [dbo].[Departments] WHERE [Code] = '25'
    UNION ALL
    SELECT '05001', 'MEDELLÍN', [Id] FROM [dbo].[Departments] WHERE [Code] = '05'
    UNION ALL
    SELECT '76001', 'CALI', [Id] FROM [dbo].[Departments] WHERE [Code] = '76'
    UNION ALL
    SELECT '08001', 'BARRANQUILLA', [Id] FROM [dbo].[Departments] WHERE [Code] = '08'
    UNION ALL
    SELECT '13001', 'CARTAGENA', [Id] FROM [dbo].[Departments] WHERE [Code] = '13';

    PRINT '5 Municipios insertados correctamente';

    -- =====================================================
    -- 03. INSERT ROLES
    -- =====================================================
    PRINT '--- INSERTING ROLES ---';

    IF NOT EXISTS (SELECT * FROM [dbo].[Roles] WHERE [Name] = 'Administrador')
    BEGIN
        INSERT INTO [dbo].[Roles] ([Name], [Description], [IsActive])
        VALUES ('Administrador', 'Acceso total al sistema y gestión de usuarios', 1);
        PRINT 'Role "Administrador" created';
    END

    IF NOT EXISTS (SELECT * FROM [dbo].[Roles] WHERE [Name] = 'Auxiliar de Registro')
    BEGIN
        INSERT INTO [dbo].[Roles] ([Name], [Description], [IsActive])
        VALUES ('Auxiliar de Registro', 'Acceso para registro y gestión básica de comerciantes', 1);
        PRINT 'Role "Auxiliar de Registro" created';
    END

    -- =====================================================
    -- 04. INSERT USERS
    -- =====================================================
    PRINT '--- INSERTING USERS ---';

    DECLARE @AdminRoleId INT;
    SELECT @AdminRoleId = [Id] FROM [dbo].[Roles] WHERE [Name] = 'Administrador';

    IF NOT EXISTS (SELECT * FROM [dbo].[Users] WHERE [Username] = 'admin')
    BEGIN
        INSERT INTO [dbo].[Users]
        ([Id], [Username], [Email], [PasswordHash], [FirstName], [LastName], [RoleId], [IsActive])
        VALUES
        (NEWID(), 'admin', 'admin@commerce-api.com',
        '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewKyNvreop4XUPR2',
        'Administrador', 'Sistema', @AdminRoleId, 1);
        PRINT 'User "admin" created';
    END

    DECLARE @AuxiliarRoleId INT;
    SELECT @AuxiliarRoleId = [Id] FROM [dbo].[Roles] WHERE [Name] = 'Auxiliar de Registro';

    IF NOT EXISTS (SELECT * FROM [dbo].[Users] WHERE [Username] = 'auxiliar')
    BEGIN
        INSERT INTO [dbo].[Users]
        ([Id], [Username], [Email], [PasswordHash], [FirstName], [LastName], [RoleId], [IsActive])
        VALUES
        (NEWID(), 'auxiliar', 'auxiliar@commerce-api.com',
        '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewKyNvreop4XUPR2',
        'Auxiliar', 'Registro', @AuxiliarRoleId, 1);
        PRINT 'User "auxiliar" created';
    END

    -- =====================================================
    -- 05. INSERT MERCHANTS (con MunicipalityId)
    -- =====================================================
    PRINT '--- INSERTING MERCHANTS ---';

    DECLARE @AdminUserId UNIQUEIDENTIFIER;
    SELECT @AdminUserId = [Id] FROM [dbo].[Users] WHERE [Username] = 'admin';

    DECLARE @BogotaId INT, @MedellinId INT, @CaliId INT, @BarranquillaId INT, @CartagenaId INT;
    
    SELECT @BogotaId = [Id] FROM [dbo].[Municipalities] WHERE [Name] = 'BOGOTÁ';
    SELECT @MedellinId = [Id] FROM [dbo].[Municipalities] WHERE [Name] = 'MEDELLÍN';
    SELECT @CaliId = [Id] FROM [dbo].[Municipalities] WHERE [Name] = 'CALI';
    SELECT @BarranquillaId = [Id] FROM [dbo].[Municipalities] WHERE [Name] = 'BARRANQUILLA';
    SELECT @CartagenaId = [Id] FROM [dbo].[Municipalities] WHERE [Name] = 'CARTAGENA';

    INSERT INTO [dbo].[Merchants] ([Name], [MunicipalityId], [Phone], [Email], [Status], [CreatedByUserId])
    VALUES
    ('Empresa Comercial 1', @BogotaId, '+573001234567', 'empresa1@example.com', 'Activo', @AdminUserId),
    ('Empresa Comercial 2', @MedellinId, '+573007654321', 'empresa2@example.com', 'Activo', @AdminUserId),
    ('Empresa Comercial 3', @CaliId, '+573002468135', 'empresa3@example.com', 'Activo', @AdminUserId),
    ('Empresa Comercial 4', @BarranquillaId, '+573009753124', 'empresa4@example.com', 'Activo', @AdminUserId),
    ('Empresa Comercial 5', @CartagenaId, '+573005555555', 'empresa5@example.com', 'Inactivo', @AdminUserId);

    PRINT '5 Merchants inserted';

    -- =====================================================
    -- 06. INSERT ESTABLISHMENTS
    -- =====================================================
    PRINT '--- INSERTING ESTABLISHMENTS ---';

    INSERT INTO [dbo].[Establishments] ([MerchantId], [Name], [Revenue], [EmployeeCount])
    VALUES
    (1, 'Sucursal Centro Bogotá', 150000.50, 25),
    (1, 'Sucursal Suba Bogotá', 120000.75, 18),
    (1, 'Sucursal Chapinero Bogotá', 95000.25, 12),
    (2, 'Sucursal Centro Medellín', 200000.00, 35),
    (2, 'Sucursal Laureles Medellín', 175000.50, 28),
    (3, 'Sucursal Centro Cali', 180000.25, 30),
    (3, 'Sucursal San Fernando Cali', 165000.75, 25),
    (3, 'Sucursal Puerto Cali', 155000.00, 22),
    (4, 'Sucursal Centro Barranquilla', 190000.50, 32),
    (4, 'Sucursal Riomar Barranquilla', 170000.25, 27);

    PRINT '10 Establishments inserted';

    -- =====================================================
    -- 07. FINAL VERIFICATION
    -- =====================================================
    PRINT '';
    PRINT '========== SEED DATA VERIFICATION ==========';

    SELECT 'Departments' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[Departments]
    UNION ALL
    SELECT 'Municipalities', COUNT(*) FROM [dbo].[Municipalities]
    UNION ALL
    SELECT 'Roles', COUNT(*) FROM [dbo].[Roles]
    UNION ALL
    SELECT 'Users', COUNT(*) FROM [dbo].[Users]
    UNION ALL
    SELECT 'Merchants', COUNT(*) FROM [dbo].[Merchants]
    UNION ALL
    SELECT 'Establishments', COUNT(*) FROM [dbo].[Establishments];

    PRINT '';
    PRINT 'Users with Roles:';
    SELECT u.[Id], u.[Username], u.[Email], r.[Name] AS [Role]
    FROM [dbo].[Users] u
    INNER JOIN [dbo].[Roles] r ON u.[RoleId] = r.[Id];

    PRINT '';
    PRINT 'Merchants with Municipalities:';
    SELECT m.[Id], m.[Name], mu.[Name] AS Municipality, d.[Name] AS Department
    FROM [dbo].[Merchants] m
    INNER JOIN [dbo].[Municipalities] mu ON m.[MunicipalityId] = mu.[Id]
    INNER JOIN [dbo].[Departments] d ON mu.[DepartmentId] = d.[Id];

    COMMIT TRANSACTION;
    PRINT '';
    PRINT '========== SEED DATA INSERTION COMPLETED SUCCESSFULLY ==========';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR:';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;