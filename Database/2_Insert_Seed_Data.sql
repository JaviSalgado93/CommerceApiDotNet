/****** 
Script: CommerceAPI - Seed Data
Creado: 2026-03-14
Descripción: Inserta datos de prueba con nuevos roles
Base de datos: CommerceApiDotNet
Nota: Ejecutar DESPUÉS de schema.sql
ACTUALIZACIÓN: Roles agregados (Administrador, Auxiliar de Registro)
Advertencia: Los passwords están en plain para desarrollo. En producción deben estar hasheados con BCrypt.
******/

USE CommerceApiDotNet;

PRINT '========== INSERTING SEED DATA ==========';

-- =====================================================
-- 01. INSERT ROLES (Catálogo base)
-- =====================================================
PRINT '========== INSERTING ROLES ==========';

IF NOT EXISTS (SELECT * FROM [dbo].[Roles] WHERE [Name] = 'Administrador')
BEGIN
    INSERT INTO [dbo].[Roles] ([Name], [Description], [IsActive])
    VALUES ('Administrador', 'Acceso total al sistema y gestión de usuarios', 1);
    PRINT 'Role "Administrador" created';
END
ELSE
BEGIN
    PRINT 'Role "Administrador" already exists';
END

IF NOT EXISTS (SELECT * FROM [dbo].[Roles] WHERE [Name] = 'Auxiliar de Registro')
BEGIN
    INSERT INTO [dbo].[Roles] ([Name], [Description], [IsActive])
    VALUES ('Auxiliar de Registro', 'Acceso para registro y gestión básica de comerciantes', 1);
    PRINT 'Role "Auxiliar de Registro" created';
END
ELSE
BEGIN
    PRINT 'Role "Auxiliar de Registro" already exists';
END

PRINT 'Roles verification:';
SELECT [Id], [Name], [Description], [IsActive] FROM [dbo].[Roles];

-- =====================================================
-- 02. INSERT ADMIN USER (con nuevo Rol)
-- =====================================================
PRINT '========== INSERTING ADMIN USER ==========';

DECLARE @AdminRoleId INT;
SELECT @AdminRoleId = [Id] FROM [dbo].[Roles] WHERE [Name] = 'Administrador';

IF NOT EXISTS (SELECT * FROM [dbo].[Users] WHERE [Username] = 'admin')
BEGIN
    INSERT INTO [dbo].[Users]
    ([Id], [Username], [Email], [PasswordHash], [FirstName], [LastName], [RoleId], [IsActive])
    VALUES
    (NEWID(), 'admin', 'admin@commerce-api.com',
    '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewKyNvreop4XUPR2', -- Password: Admin123! (BCrypt)
    'Administrador', 'Sistema', @AdminRoleId, 1);
    
    PRINT 'User "admin" created with Administrador role';
END
ELSE
BEGIN
    PRINT 'User "admin" already exists';
END

-- =====================================================
-- 03. INSERT ADDITIONAL USERS (Auxiliar de Registro)
-- =====================================================
PRINT '========== INSERTING AUXILIARY USER ==========';

DECLARE @AuxiliarRoleId INT;
SELECT @AuxiliarRoleId = [Id] FROM [dbo].[Roles] WHERE [Name] = 'Auxiliar de Registro';

IF NOT EXISTS (SELECT * FROM [dbo].[Users] WHERE [Username] = 'auxiliar')
BEGIN
    INSERT INTO [dbo].[Users]
    ([Id], [Username], [Email], [PasswordHash], [FirstName], [LastName], [RoleId], [IsActive])
    VALUES
    (NEWID(), 'auxiliar', 'auxiliar@commerce-api.com',
    '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewKyNvreop4XUPR2', -- Password: Admin123! (BCrypt)
    'Auxiliar', 'Registro', @AuxiliarRoleId, 1);
    
    PRINT 'User "auxiliar" created with Auxiliar de Registro role';
END
ELSE
BEGIN
    PRINT 'User "auxiliar" already exists';
END

-- =====================================================
-- 04. INSERT MERCHANTS (5 registros)
-- =====================================================
DECLARE @AdminUserId UNIQUEIDENTIFIER;
SELECT @AdminUserId = [Id] FROM [dbo].[Users] WHERE [Username] = 'admin';

PRINT '========== INSERTING MERCHANTS ==========';

INSERT INTO [dbo].[Merchants] ([Name], [Municipality], [Phone], [Email], [Status], [CreatedByUserId])
VALUES
('Empresa Comercial 1', 'Bogotá', '+573001234567', 'empresa1@example.com', 'Activo', @AdminUserId),
('Empresa Comercial 2', 'Medellín', '+573007654321', 'empresa2@example.com', 'Activo', @AdminUserId),
('Empresa Comercial 3', 'Cali', '+573002468135', 'empresa3@example.com', 'Activo', @AdminUserId),
('Empresa Comercial 4', 'Barranquilla', '+573009753124', 'empresa4@example.com', 'Activo', @AdminUserId),
('Empresa Comercial 5', 'Cartagena', '+573005555555', 'empresa5@example.com', 'Inactivo', @AdminUserId);

PRINT 'Merchants inserted: 5 records';

-- =====================================================
-- 05. INSERT ESTABLISHMENTS (10 registros distribuidos)
-- =====================================================
PRINT '========== INSERTING ESTABLISHMENTS ==========';

-- Comerciante 1 (3 establecimientos)
INSERT INTO [dbo].[Establishments] ([MerchantId], [Name], [Revenue], [EmployeeCount])
VALUES
(1, 'Sucursal Centro Bogotá', 150000.50, 25),
(1, 'Sucursal Suba Bogotá', 120000.75, 18),
(1, 'Sucursal Chapinero Bogotá', 95000.25, 12);

-- Comerciante 2 (2 establecimientos)
INSERT INTO [dbo].[Establishments] ([MerchantId], [Name], [Revenue], [EmployeeCount])
VALUES
(2, 'Sucursal Centro Medellín', 200000.00, 35),
(2, 'Sucursal Laureles Medellín', 175000.50, 28);

-- Comerciante 3 (3 establecimientos)
INSERT INTO [dbo].[Establishments] ([MerchantId], [Name], [Revenue], [EmployeeCount])
VALUES
(3, 'Sucursal Centro Cali', 180000.25, 30),
(3, 'Sucursal San Fernando Cali', 165000.75, 25),
(3, 'Sucursal Puerto Cali', 155000.00, 22);

-- Comerciante 4 (2 establecimientos)
INSERT INTO [dbo].[Establishments] ([MerchantId], [Name], [Revenue], [EmployeeCount])
VALUES
(4, 'Sucursal Centro Barranquilla', 190000.50, 32),
(4, 'Sucursal Riomar Barranquilla', 170000.25, 27);

-- Comerciante 5 (0 establecimientos - está inactivo)

PRINT 'Establishments inserted: 10 records';

-- =====================================================
-- 06. FINAL VERIFICATION
-- =====================================================
PRINT '';
PRINT '========== SEED DATA VERIFICATION ==========';

SELECT 'Roles' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[Roles]
UNION ALL
SELECT 'Users', COUNT(*) FROM [dbo].[Users]
UNION ALL
SELECT 'Merchants', COUNT(*) FROM [dbo].[Merchants]
UNION ALL
SELECT 'Establishments', COUNT(*) FROM [dbo].[Establishments];

PRINT '';
PRINT 'Users with Roles:';
SELECT u.[Id], u.[Username], u.[Email], u.[FirstName], u.[LastName], r.[Name] AS [Role], u.[IsActive]
FROM [dbo].[Users] u
INNER JOIN [dbo].[Roles] r ON u.[RoleId] = r.[Id];

PRINT '';
PRINT 'Sample - Active Merchants Report:';
SELECT * FROM [dbo].[fn_GetActiveMerchantsReport]();

PRINT '';
PRINT '========== SEED DATA INSERTION COMPLETED ==========';