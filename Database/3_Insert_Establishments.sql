-- =============================================================
-- Script: Insertar Establecimientos para Comerciantes
-- Base de datos: CommerceApiDotNet
-- Descripción: Inserta datos de establecimientos (sucursales) 
--              para cada comerciante
-- =============================================================

USE CommerceApiDotNet;

BEGIN TRANSACTION;

BEGIN TRY
    PRINT '========== INSERTING ESTABLISHMENTS ==========';

    -- Verificar si ya existen establecimientos
    IF (SELECT COUNT(*) FROM [dbo].[Establishments]) = 0
    BEGIN
        PRINT '--- No hay establecimientos. Insertando...';
        
        INSERT INTO [dbo].[Establishments] ([MerchantId], [Name], [Revenue], [EmployeeCount], [CreatedAt])
        VALUES
        -- Merchant 1 (Id=1): Empresa Comercial 1 - 3 sucursales
        (1, 'Sucursal Centro Bogotá', 150000.50, 25, GETUTCDATE()),
        (1, 'Sucursal Suba Bogotá', 120000.75, 18, GETUTCDATE()),
        (1, 'Sucursal Chapinero Bogotá', 95000.25, 12, GETUTCDATE()),
        
        -- Merchant 2 (Id=2): Empresa Comercial 2 - 2 sucursales
        (2, 'Sucursal Centro Medellín', 200000.00, 35, GETUTCDATE()),
        (2, 'Sucursal Laureles Medellín', 175000.50, 28, GETUTCDATE()),
        
        -- Merchant 3 (Id=3): Empresa Comercial 3 - 3 sucursales
        (3, 'Sucursal Centro Cali', 180000.25, 30, GETUTCDATE()),
        (3, 'Sucursal San Fernando Cali', 165000.75, 25, GETUTCDATE()),
        (3, 'Sucursal Puerto Cali', 155000.00, 22, GETUTCDATE()),
        
        -- Merchant 4 (Id=4): Empresa Comercial 4 - 2 sucursales
        (4, 'Sucursal Centro Barranquilla', 190000.50, 32, GETUTCDATE()),
        (4, 'Sucursal Riomar Barranquilla', 170000.25, 27, GETUTCDATE());
        
        PRINT '10 Establecimientos insertados correctamente';
    END
    ELSE
    BEGIN
        PRINT 'Ya existen establecimientos en la BD. Saltando inserción.';
    END

    -- Verificación final
    PRINT '';
    PRINT '========== VERIFICATION ==========';
    
    SELECT 'Total Establishments' AS Info, COUNT(*) AS Count FROM [dbo].[Establishments]
    UNION ALL
    SELECT 'Merchants with Establishments', COUNT(DISTINCT [MerchantId]) FROM [dbo].[Establishments];

    PRINT '';
    PRINT 'Establishments by Merchant:';
    SELECT 
        m.[Id] AS MerchantId,
        m.[Name] AS MerchantName,
        COUNT(e.[Id]) AS EstablishmentCount,
        ISNULL(SUM(e.[Revenue]), 0) AS TotalRevenue,
        ISNULL(SUM(e.[EmployeeCount]), 0) AS TotalEmployees
    FROM [dbo].[Merchants] m
    LEFT JOIN [dbo].[Establishments] e ON m.[Id] = e.[MerchantId]
    GROUP BY m.[Id], m.[Name]
    ORDER BY m.[Id];

    COMMIT TRANSACTION;
    PRINT '';
    PRINT '========== ESTABLISHMENTS INSERTION COMPLETED SUCCESSFULLY ==========';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR:';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;
