# Base de Datos - CommerceAPI

Guía completa para la arquitectura y ejecución de scripts de base de datos.

## Arquitectura de Base de Datos

La base de datos está organizada en **3 módulos principales**: Autenticación, Geografía y Comercio. En total contiene **8 tablas** con relaciones cuidadosamente diseñadas.

### Tablas de Autenticación y Seguridad

**1. Roles** (Catálogo de Roles)
- `Id` (PK): INT
- `Name`: Nombre único del rol ('Administrador', 'Auxiliar de Registro', etc.)
- `Description`: Descripción del rol y permisos
- `IsActive`: Indica si el rol está habilitado
- Auditoría: `CreatedAt`, `UpdatedAt`, `UpdatedBy`
- Propósito: Define los roles disponibles en el sistema

**2. Users** (Autenticación de usuarios)
- `Id` (PK): UNIQUEIDENTIFIER
- `Username`, `Email`: Únicos en el sistema
- `PasswordHash`: Contraseña encriptada con BCrypt (nunca en texto plano)
- `FirstName`, `LastName`: Nombre y apellido del usuario
- `RoleId` (FK): Referencia a tabla Roles
- Auditoría: `CreatedAt`, `UpdatedAt`, `UpdatedBy`
- Seguridad: `IsActive`, `FailedAttempts`, `LockedUntil` (bloqueo por intentos fallidos)
- Acceso: `LastAccess` (último acceso registrado)
- Propósito: Almacena usuarios y sus credenciales

**3. RefreshTokens** (Gestión de tokens OAuth 2.0)
- `Id` (PK): UNIQUEIDENTIFIER
- `UserId` (FK): Referencia a tabla Users
- `Token`: JWT refresh token único
- Control: `ExpiresAt` (expiración), `IsRevoked`, `RevokedAt`
- Rotación: `ReplacedBy` (para rotación segura de tokens)
- Propósito: Permite refrescar access tokens sin re-autenticarse

**4. TokenBlacklist** (Revocación y bloqueo de tokens)
- `Id` (PK): UNIQUEIDENTIFIER
- `UserId` (FK): Usuario propietario del token
- `TokenHash`: Hash del JWT (para búsquedas rápidas)
- Control: `RevokedAt`, `ExpiresAt`, `Reason` (motivo de revocación)
- Propósito: Blacklist de tokens revocados para logout y control de sesiones

### Tablas de Geografía

**5. Departments** (Departamentos de Colombia)
- `Id` (PK): INT
- `Code`: Código del departamento único
- `Name`: Nombre del departamento único
- `Region`: Región geográfica ('ANDINA', 'PAC?FICA', 'CARIBE', etc.)
- `CreatedAt`: Fecha de registro
- Propósito: Catálogo de departamentos colombianos

**6. Municipalities** (Municipios)
- `Id` (PK): INT
- `Code`: Código único del municipio
- `Name`: Nombre del municipio
- `DepartmentId` (FK): Referencia a tabla Departments
- Constraint: Nombre y Departamento únicos (no se repiten municipios por departamento)
- Propósito: Catálogo de municipios agrupados por departamento

### Tablas de Comercio

**7. Merchants** (Comerciantes/Empresas)
- `Id` (PK): INT
- `Name`: Razón social o nombre comercial
- `MunicipalityId` (FK): Ubicación del comerciante (referencia a Municipalities)
- `Phone`: Teléfono de contacto
- `Email`: Correo electrónico
- `Status`: Estado ('Activo' | 'Inactivo')
- Auditoría: `CreatedAt`, `UpdatedAt`, `UpdatedBy`
- Relación: `CreatedByUserId` (FK -> Users) - usuario que registró el comerciante
- Propósito: Almacena información de comerciantes/empresas registradas

**8. Establishments** (Establecimientos/Sucursales)
- `Id` (PK): INT
- `MerchantId` (FK): Referencia a tabla Merchants
- `Name`: Nombre de la sucursal o establecimiento
- `Revenue`: Ingresos (DECIMAL 15,2)
- `EmployeeCount`: Número de empleados
- Auditoría: `CreatedAt`, `UpdatedAt`, `UpdatedBy`
- Propósito: Detalla las sucursales/establecimientos de cada comerciante

---

## Relaciones Entre Tablas

```
Roles (1:N) Users
?? Un rol puede tener múltiples usuarios

Users (1:N) RefreshTokens
?? Un usuario puede tener múltiples refresh tokens activos

Users (1:N) TokenBlacklist
?? Un usuario puede tener múltiples tokens revocados

Departments (1:N) Municipalities
?? Un departamento contiene múltiples municipios

Municipalities (1:N) Merchants
?? Un municipio puede tener múltiples comerciantes

Merchants (1:N) Establishments
?? Un comerciante puede operar múltiples establecimientos

Users (1:N) Merchants
?? Un usuario administrador puede crear múltiples comerciantes
```

---

## Orden de Ejecución de Scripts (IMPORTANTE)

### ?? PASO 1: Ejecutar `1_Schema.sql` (OBLIGATORIO PRIMERO)

**Este script es fundamental y DEBE ejecutarse primero.** Crea:
- ? Base de datos `CommerceApiDotNet`
- ? Todas las tablas (Roles, Users, RefreshTokens, TokenBlacklist, Departments, Municipalities, Merchants, Establishments)
- ? Índices para optimizar búsquedas y performance
- ? Triggers para auditoría automática (CreatedAt, UpdatedAt)
- ? Función `fn_GetActiveMerchantsReport()` para reportes de comerciantes activos

**Dependencias de tablas en `1_Schema.sql`:**
```
1. Roles (sin dependencias)
2. Users ? depende de Roles
3. RefreshTokens ? depende de Users
4. TokenBlacklist ? depende de Users
5. Departments (sin dependencias)
6. Municipalities ? depende de Departments
7. Merchants ? depende de Municipalities y Users
8. Establishments ? depende de Merchants
```

**En SQL Server Management Studio (SSMS):**
1. Abre SQL Server Management Studio
2. Conecta a tu SQL Server (local o Azure)
3. Click en "New Query"
4. Copia TODO el contenido de `1_Schema.sql`
5. Presiona **F5** o click "Execute"
6. Verás: `========== DATABASE SETUP COMPLETED SUCCESSFULLY ==========`

---

### ? PASO 2: Ejecutar `2_Insert_Seed_Data.sql` (DESPUÉS de Paso 1)

**Este script carga datos de prueba y SOLO funciona si `1_Schema.sql` se ejecutó antes.** Inserta:
- ? 5 Departamentos (CUNDINAMARCA, ANTIOQUIA, VALLE DEL CAUCA, ATLÁNTICO, BOLÍVAR)
- ? 5 Municipios (BOGOTÁ, MEDELLÍN, CALI, BARRANQUILLA, CARTAGENA)
- ? 2 Roles predefinidos (Administrador, Auxiliar de Registro)
- ? 2 Usuarios iniciales (admin, auxiliar)
- ? 5 Comerciantes de prueba (algunos activos, algunos inactivos)
- ? 10 Establecimientos distribuidos entre los comerciantes

**En SQL Server Management Studio (SSMS):**
1. Abre una nueva Query (o limpia la anterior)
2. Copia TODO el contenido de `2_Insert_Seed_Data.sql`
3. Presiona **F5** o click "Execute"
4. Verás: `========== SEED DATA INSERTION COMPLETED SUCCESSFULLY ==========`

---

## Relaciones Entre Tablas
Users (1:N) RefreshTokens 
-> Un usuario puede tener múltiples refresh tokens

Users (1:N) TokenBlacklist 
-> Un usuario puede tener múltiples tokens revocados

Users (1:N) Merchants 
-> Un admin puede crear múltiples comerciantes

Merchants (1:N) Establishments 
-> Un comerciante puede tener múltiples establecimientos

---

## Validación Después de Ejecutar los Scripts

### Después de `1_Schema.sql` (Paso 1)
Ejecuta esta query para verificar que todas las tablas se crearon:

```sql
-- Verificar todas las tablas creadas
SELECT 
    TABLE_NAME,
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = t.TABLE_NAME) AS [Columnas]
FROM INFORMATION_SCHEMA.TABLES t
WHERE TABLE_SCHEMA = 'dbo'
ORDER BY TABLE_NAME;

-- Resultado esperado: 8 tablas
-- (Roles, Users, RefreshTokens, TokenBlacklist, Departments, Municipalities, Merchants, Establishments)
```

### Después de `2_Insert_Seed_Data.sql` (Paso 2)
Ejecuta estas queries para verificar que los datos se cargaron:

```sql
-- 1. Contar registros en cada tabla
SELECT 'Departments' AS [Tabla], COUNT(*) AS [Total] FROM Departments
UNION ALL
SELECT 'Municipalities', COUNT(*) FROM Municipalities
UNION ALL
SELECT 'Roles', COUNT(*) FROM Roles
UNION ALL
SELECT 'Users', COUNT(*) FROM Users
UNION ALL
SELECT 'Merchants', COUNT(*) FROM Merchants
UNION ALL
SELECT 'Establishments', COUNT(*) FROM Establishments;

-- Resultado esperado:
-- Departments: 5
-- Municipalities: 5
-- Roles: 2
-- Users: 2
-- Merchants: 5
-- Establishments: 10

-- 2. Ver usuarios creados con sus roles
SELECT u.[Id], u.[Username], u.[Email], r.[Name] AS [Rol]
FROM [dbo].[Users] u
INNER JOIN [dbo].[Roles] r ON u.[RoleId] = r.[Id];

-- 3. Ver comerciantes con sus municipios y departamentos
SELECT m.[Id], m.[Name] AS [Comerciante], mu.[Name] AS [Municipio], d.[Name] AS [Departamento], m.[Status]
FROM [dbo].[Merchants] m
INNER JOIN [dbo].[Municipalities] mu ON m.[MunicipalityId] = mu.[Id]
INNER JOIN [dbo].[Departments] d ON mu.[DepartmentId] = d.[Id]
ORDER BY m.[Status] DESC, m.[Name];

-- 4. Probar función de reporte (Comerciantes activos)
SELECT * FROM fn_GetActiveMerchantsReport();
```

---

## Usuarios Predeterminados
Después de ejecutar ambos scripts, tendrás 2 usuarios disponibles:

| Username | Email                     | Contraseña (Hashed)     | Rol                  | Estado  |
|----------|---------------------------|-------------------------|----------------------|---------|
| admin    | admin@commerce-api.com    | BCrypt (Admin123!)      | Administrador        | Activo  |
| auxiliar | auxiliar@commerce-api.com | BCrypt (Admin123!)      | Auxiliar de Registro | Activo  |

?? **Nota**: Las contraseñas están hasheadas con BCrypt. Para cambiarlas en producción, usa los endpoints de autenticación de la API.

---