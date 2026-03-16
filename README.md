# 🏗️ Commerce API .NET - Prueba Técnica

Una **API REST para gestión de comerciantes** desarrollada con **.NET 8** que resuelve una prueba técnica fullstack. Implementa **Arquitectura Hexagonal (Puertos y Adaptadores)** con autenticación JWT, gestión de comerciantes y reportes de datos.

---

## 📑 Tabla de Contenidos

- [Sobre esta Prueba](#sobre-esta-prueba)
- [Retos Implementados](#retos-implementados)
- [Base de Datos](#base-de-datos)
- [Arquitectura](#arquitectura)
- [Requisitos](#requisitos)
- [Instalación](#instalación)
- [Configuración](#configuración)
- [API Endpoints](#api-endpoints)
- [Pruebas](#pruebas)
- [Estructura del Proyecto](#estructura-del-proyecto)

---

## 🎯 Sobre esta Prueba

Esta es una **evaluación técnica fullstack .NET** que simula un escenario real donde:

> *"En el creciente mundo empresarial y comercial, las empresas son los nodos medulares de la economía. Por eso es imprescindible lograr disponer de los datos suficientes para analizar el patrimonio fluctuante del mercado, razón por la cual el arenal de comercio desea tener a su disposición una herramienta que le permita conocer de forma rápida y centralizada la información de los comerciantes y sus respectivos establecimientos."*

**Objetivo**: Construir una API que condense la información de comerciantes y establecimientos con el objetivo de apoyar los procesos operativos esenciales de la agregación nacional de comercio.

**Duración**: 1 día de desarrollo

---

## ✅ Retos Implementados

### RETO 05: Web API - Seguridad
**Autenticación y Autorización con JWT**

- ✅ **Endpoint de Login** (público, sin requiere JWT)
  - Recibe: Email y Contraseña
  - Genera: JWT con expiración de 1 hora
  - Respuesta: Access Token, Refresh Token, Datos Usuario

- ✅ **Autorización por Roles** (Administrador y Auxiliar de Registro)
  - Control de acceso a endpoints por rol
  - Validaciones de seguridad

- ✅ **CORS Configurado** para consumo controlado de APIs

- ✅ **Entity Framework ORM** para gestión de entidades

**Tecnología**: JWT Bearer, BCrypt, Roles RBAC

```bash
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@commerce-api.com",
  "password": "Admin123!"
}

# Respuesta: { accessToken, refreshToken, user }
```

---

### RETO 06: Web API - Listas de Valores
**Endpoint de Municipios con Caché en Memoria**

- ✅ **Endpoint para Municipios** (privado, requiere JWT)
  - Retorna lista de municipios para CRUD de Comerciantes
  - Paginación: 5 registros por página por defecto
  - Campos: Id, Nombre, Código, Departamento

- ✅ **Caché en Memoria** (Opcional) para evitar accesos a BD

- ✅ **Estandarización de respuestas HTTP** de endpoints

- ✅ **Entity Framework ORM** para mapeo de datos

**Tecnología**: Memory Cache, IMemoryCache, EF Core

```bash
GET /api/municipalities?pageNumber=1&pageSize=5
Authorization: Bearer {token}

# Respuesta: { success, data: [...], pagination }
```

---

### RETO 07: Web API - CRUD Comerciante
**Gestión Completa de Comerciantes**

- ✅ **Endpoints CRUD Completos**:
  - **GET** (Paginado): Listar comerciantes con filtros
  - **GET by Id**: Obtener comerciante específico
  - **POST**: Crear nuevo comerciante
  - **PUT**: Actualizar comerciante
  - **PATCH**: Cambiar estado (Activo/Inactivo) - Solo Administrador
  - **DELETE**: Eliminar comerciante - Solo Administrador

- ✅ **Filtrado y Búsqueda**:
  - Por Nombre o Razón Social
  - Por Fecha de Registro
  - Por Estado (Activo/Inactivo)

- ✅ **Auditoría Automática**:
  - Campos `UpdatedAt` y `UpdatedBy` se actualizan automáticamente
  - Base de datos calcula con triggers

- ✅ **Validaciones de Datos**:
  - Tipos de datos, obligatoriedad
  - Email válido
  - Teléfono formateado

- ✅ **Entity Framework ORM** para gestión de entidades

**Tecnología**: Repositories, DTOs, AutoMapper, EF Core

```bash
# Listar paginado
GET /api/merchants?pageNumber=1&pageSize=10&status=Activo
Authorization: Bearer {token}

# Crear
POST /api/merchants
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Empresa X",
  "municipalityId": 1,
  "phone": "+573001234567",
  "email": "empresa@example.com"
}

# Cambiar estado (PATCH - Solo Administrador)
PATCH /api/merchants/{id}/status
Authorization: Bearer {admin-token}
Content-Type: application/json

{ "status": "Inactivo" }
```

---

### RETO 08: Web API - Reporte Comerciantes
**Generación de Archivo CSV con Reportes**

- ✅ **Endpoint de Reporte** (privado, Solo Administrador)
  - Genera archivo **CSV plano** con información de comerciantes activos
  - Incluye datos agregados de establecimientos

- ✅ **Estructura del CSV**:
  ```
  Nombre|Municipio|Teléfono|Correo|Fecha Registro|Estado|Cantidad Establecimientos|Total Ingresos|Cantidad Empleados
  Empresa 1|Bogotá|+573001234567|empresa1@example.com|2025-01-15|Activo|3|450000.50|75
  ```

- ✅ **Cálculos Agregados**:
  - Cantidad de Establecimientos (por comerciante)
  - Total Ingresos (suma de Revenue de establecimientos)
  - Cantidad de Empleados (suma de EmployeeCount)

- ✅ **Usa Función SQL** creada en el Reto 4 (fn_GetActiveMerchantsReport)

- ✅ **Estandarización de Respuestas HTTP**

**Tecnología**: SQL Functions, LINQ, Streaming CSV

```bash
# Descargar reporte CSV
GET /api/merchants/export/csv
Authorization: Bearer {admin-token}

# Respuesta: Archivo CSV descargable
Content-Type: text/csv
Content-Disposition: attachment; filename="merchants-report.csv"
```

---

## 🗄️ Base de Datos

Se utilizó una base de datos SQL Server con **8 tablas relacionadas** que soportan la solución:

### Tablas Principales para esta Prueba

| Tabla | Propósito | Relación |
|-------|-----------|----------|
| **Users** | Autenticación y autorización | PK: UNIQUEIDENTIFIER |
| **Roles** | Catálogo de roles (Administrador, Auxiliar) | 1:N con Users |
| **Departments** | Departamentos de Colombia | 1:N con Municipalities |
| **Municipalities** | Municipios agrupados por departamento | 1:N con Merchants |
| **Merchants** | Comerciantes/Empresas | 1:N con Establishments |
| **Establishments** | Sucursales/Establecimientos por comerciante | N:1 con Merchants |
| **RefreshTokens** | Gestión de tokens de refresco | 1:N con Users |
| **TokenBlacklist** | Revocación de tokens | 1:N con Users |

### Ejecución de Scripts BD

**⚠️ Importante**: Los scripts deben ejecutarse en orden:

1. **`1_Schema.sql`** - Crea las tablas, índices, triggers y función de reporte
2. **`2_Insert_Seed_Data.sql`** - Inserta datos de prueba (5 departamentos, 5 municipios, 2 usuarios, 5 comerciantes, 10 establecimientos)
3. **`3_Insert_Establishments.sql`** - Script adicional para más establecimientos (opcional)

**Documentación completa**: Ver [Database/README.md](./Database/README.md)

---

## 🏛️ Arquitectura

**Hexagonal Architecture - 5 Proyectos Organizados en Capas:**

```
???????????????????????????????????????????
?  API Layer (Controllers, Middleware)     ?
?         Api.csproj                      ?
???????????????????????????????????????????
               ?
???????????????????????????????????????????
?  Application Layer (Servicios, Puertos)  ?
?      Application.csproj                 ?
???????????????????????????????????????????
               ?
???????????????????????????????????????????
?   Domain Layer (Entidades)               ?
?        Domain.csproj                    ?
???????????????????????????????????????????
               ?
???????????????????????????????????????????
? Infrastructure Layer (BD, Email, etc.)   ?
?     Infrastructure.csproj               ?
???????????????????????????????????????????

Tests.csproj - Pruebas automatizadas
```

### **Responsabilidades por Capa:**

| Capa | Proyecto | Responsabilidad |
|------|----------|---|
| **Domain** | Domain.csproj | Entidades: User, Merchant, Establishment, Municipality, Department |
| **Application** | Application.csproj | Servicios: AuthService, MerchantService, MunicipalityService |
| **Infrastructure** | Infrastructure.csproj | Repositorios, BD (EF Core), Mapping (AutoMapper) |
| **API** | Api.csproj | Controllers: AuthController, MerchantsController, MunicipalitiesController |
| **Tests** | Tests.csproj | Pruebas automatizadas |

---

## 💻 Requisitos

- **.NET 8 SDK** ([descargar](https://dotnet.microsoft.com/download))
- **SQL Server 2019+** o **SQL Server Express**
- **Visual Studio 2022** o **VS Code**

---

## 🔧 Instalación

### 1. Clonar el repositorio
```bash
git clone https://github.com/JaviSalgado93/CommerceApiDotNet.git
cd CommerceApiDotNet
```

### 2. Restaurar dependencias
```bash
dotnet restore
```

### 3. Configurar base de datos

#### Opción A: Scripts SQL (Recomendado)
```bash
# Ejecutar en SQL Server Management Studio (SSMS):
# 1. Abrir Database/1_Schema.sql y ejecutar (F5)
# 2. Abrir Database/2_Insert_Seed_Data.sql y ejecutar (F5)
```

#### Opción B: Entity Framework Migrations
```bash
dotnet ef database update --project Infrastructure --startup-project Api
```

### 4. Configurar appsettings

Edita `Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "dbContext": "Server=localhost;Database=CommerceApiDotNet;User Id=sa;Password=tu-password;"
  },
  "Authentication": {
    "SecretKey": "tu-llave-secreta-minimo-64-caracteres-aqui",
    "Issuer": "CommerceApiDotNet",
    "Audience": "CommerceApiDotNet-Users",
    "AccessTokenExpiration": "60",
    "RefreshTokenExpiration": "7"
  }
}
```

### 5. Compilar y ejecutar
```bash
dotnet build
cd Api
dotnet run
```

✅ **API disponible en**: `https://localhost:7085`  
✅ **Swagger UI en**: `https://localhost:7085/swagger`

---

## ⚙️ Configuración

### Usuarios de Prueba

Después de ejecutar los scripts SQL, tienes 2 usuarios disponibles:

| Usuario | Contraseña | Rol | Email |
|---------|-----------|-----|-------|
| **admin** | Admin123! | Administrador | admin@commerce-api.com |
| **auxiliar** | Admin123! | Auxiliar de Registro | auxiliar@commerce-api.com |

### JWT Configuration
```json
"Authentication": {
  "SecretKey": "LLAVE-MUY-SEGURA-MINIMO-64-CARACTERES",
  "Issuer": "CommerceApiDotNet",
  "Audience": "CommerceApiDotNet-Users",
  "AccessTokenExpiration": "60",        // Minutos
  "RefreshTokenExpiration": "7"         // Días
}
```

### CORS
```json
"CorsPolicies": [
  {
    "Origin": "https://localhost:3000",
    "Methods": ["GET", "POST", "PUT", "DELETE", "PATCH"],
    "Headers": ["Content-Type", "Authorization"]
  }
]
```

---

## 📡 API Endpoints

### Autenticación (RETO 05)

| Método | Endpoint | Descripción | Auth |
|--------|----------|---|---|
| POST | `/api/auth/login` | Login con email y contraseña | ❌ |
| POST | `/api/auth/logout` | Cerrar sesión | ✅ |
| POST | `/api/auth/refresh-token` | Renovar access token | ❌ |

### Municipios - Listas de Valores (RETO 06)

| Método | Endpoint | Descripción | Auth |
|--------|----------|---|---|
| GET | `/api/municipalities` | Listar municipios (paginado) | ✅ |
| GET | `/api/municipalities?pageNumber=1&pageSize=5` | Con paginación | ✅ |

### Comerciantes - CRUD (RETO 07)

| Método | Endpoint | Descripción | Auth | Rol |
|--------|----------|---|---|---|
| GET | `/api/merchants` | Listar (paginado) | ✅ | Cualquiera |
| GET | `/api/merchants/{id}` | Obtener por ID | ✅ | Cualquiera |
| POST | `/api/merchants` | Crear | ✅ | Auxiliar+ |
| PUT | `/api/merchants/{id}` | Actualizar | ✅ | Auxiliar+ |
| PATCH | `/api/merchants/{id}/status` | Cambiar estado | ✅ | Admin |
| DELETE | `/api/merchants/{id}` | Eliminar | ✅ | Admin |

### Reporte Comerciantes - CSV (RETO 08)

| Método | Endpoint | Descripción | Auth | Rol |
|--------|----------|---|---|---|
| GET | `/api/merchants/export/csv` | Descargar CSV | ✅ | Admin |

---

## 📡 Ejemplos de Uso

### 1. Login (RETO 05)
```bash
curl -X POST https://localhost:7085/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@commerce-api.com",
    "password": "Admin123!"
  }'

# Respuesta:
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "kX9mZ2pL...",
    "user": {
      "id": "550e8400-e29b-41d4",
      "username": "admin",
      "email": "admin@commerce-api.com",
      "role": "Administrador"
    }
  }
}
```

### 2. Obtener Municipios (RETO 06)
```bash
curl https://localhost:7085/api/municipalities?pageNumber=1&pageSize=5 \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."

# Respuesta:
{
  "success": true,
  "data": [
    { "id": 1, "name": "BOGOTA", "code": "25001", "department": "CUNDINAMARCA" },
    { "id": 2, "name": "MEDELLIN", "code": "05001", "department": "ANTIOQUIA" }
  ],
  "pagination": {
    "pageNumber": 1,
    "pageSize": 5,
    "totalRecords": 5,
    "totalPages": 1
  }
}
```

### 3. Listar Comerciantes (RETO 07)
```bash
curl https://localhost:7085/api/merchants?pageNumber=1&pageSize=10&status=Activo \
  -H "Authorization: Bearer {token}"
```

### 4. Crear Comerciante (RETO 07)
```bash
curl -X POST https://localhost:7085/api/merchants \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "name": "Empresa Nueva",
    "municipalityId": 1,
    "phone": "+573001234567",
    "email": "empresa@example.com"
  }'
```

### 5. Descargar Reporte CSV (RETO 08)
```bash
curl https://localhost:7085/api/merchants/export/csv \
  -H "Authorization: Bearer {admin-token}" \
  --output merchants-report.csv

# Archivo CSV con estructura:
# Nombre|Municipio|Telefono|Correo|Fecha Registro|Estado|Cantidad Establecimientos|Total Ingresos|Cantidad Empleados
```

---

## 🧪 Pruebas

### Ejecutar todas las pruebas
```bash
dotnet test
```

### Pruebas específicas
```bash
dotnet test --filter "AuthServiceTests"
dotnet test --filter "MerchantsControllerTests"
```

---

## 📁 Estructura del Proyecto

```
CommerceApiDotNet/
├── Domain/                          # Entidades
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── Merchant.cs
│   │   ├── Establishment.cs
│   │   ├── Municipality.cs
│   │   └── Department.cs
│   └── Domain.csproj
│
├── Application/                     # Servicios y DTOs
│   ├── Services/
│   │   ├── AuthService.cs
│   │   ├── MerchantService.cs
│   │   └── MunicipalityService.cs
│   ├── Ports/
│   │   ├── IAuthService.cs
│   │   ├── IMerchantService.cs
│   │   └── IMunicipalityService.cs
│   ├── DTOs/
│   │   ├── Auth/ (LoginRequestDTO, LoginResponseDTO)
│   │   └── Merchant/ (MerchantDTO, CreateMerchantDTO, UpdateMerchantDTO)
│   └── Application.csproj
│
├── Infrastructure/                  # BD, Repositorios
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   └── Repositories/
│   │       ├── UserRepository.cs
│   │       ├── MerchantRepository.cs
│   │       └── MunicipalityRepository.cs
│   ├── Mapping/
│   │   └── AutoMapperProfile.cs
│   └── Infrastructure.csproj
│
├── Api/                             # Controllers, Middleware
│   ├── Controllers/
│   │   ├── AuthController.cs        # RETO 05
│   │   ├── MerchantsController.cs   # RETO 07, 08
│   │   └── MunicipalitiesController.cs # RETO 06
│   ├── Program.cs
│   ├── appsettings.json
│   └── Api.csproj
│
├── Tests/                           # Pruebas automatizadas
│   ├── Application/Services/
│   ├── Api/Controllers/
│   └── Tests.csproj
│
├── Database/                        # Scripts SQL
│   ├── 1_Schema.sql                 # Creacion de tablas
│   ├── 2_Insert_Seed_Data.sql       # Datos iniciales
│   ├── 3_Insert_Establishments.sql  # Datos adicionales (opcional)
│   └── README.md                    # Documentacion BD
│
└── CommerceApiDotNet.sln
```

---

## 🚀 Resumen de Implementación

| Reto | Funcionalidad | Estado | Tecnología |
|------|---------------|--------|-----------|
| **RETO 05** | Login + JWT + Roles | ✅ Completo | JWT Bearer, BCrypt, RBAC |
| **RETO 06** | Endpoint Municipios + Cache | ✅ Completo | IMemoryCache, EF Core |
| **RETO 07** | CRUD Comerciantes | ✅ Completo | Repositories, DTOs, AutoMapper |
| **RETO 08** | Reporte CSV | ✅ Completo | SQL Functions, LINQ, Streaming |

---

**Creado con 🛠️ usando .NET 8, Arquitectura Hexagonal y SQL Server**

*Última actualización: 20265*
