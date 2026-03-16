using Application.DTOs.Merchants;
using Application.Ports;
using Application.Services;
using Domain.Entities;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Application.Services;

/// <summary>
/// Test suite para MerchantService
/// Prueba la lógica de negocio de comerciantes: obtener, crear, actualizar
/// Utiliza Moq para simular el repositorio
/// </summary>
public class MerchantServiceTests
{
    private readonly Mock<IMerchantRepository> _mockRepository;
    private readonly Mock<IValidator<CreateMerchantDto>> _mockCreateValidator;
    private readonly Mock<IValidator<UpdateMerchantDto>> _mockUpdateValidator;
    private readonly Mock<ILogger<MerchantService>> _mockLogger;
    private readonly MerchantService _service;

    public MerchantServiceTests()
    {
        _mockRepository = new Mock<IMerchantRepository>();
        _mockCreateValidator = new Mock<IValidator<CreateMerchantDto>>();
        _mockUpdateValidator = new Mock<IValidator<UpdateMerchantDto>>();
        _mockLogger = new Mock<ILogger<MerchantService>>();
        _service = new MerchantService(
            _mockRepository.Object,
            _mockCreateValidator.Object,
            _mockUpdateValidator.Object,
            _mockLogger.Object
        );
    }

    #region GetAllAsync Tests

    /// <summary>
    /// ? PRUEBA 1: Obtener lista paginada de comerciantes con servicio
    /// Objetivo: Verificar que el servicio retorna DTO con información agregada
    /// Resultado esperado: Respuesta paginada con comerciantes convertidos a DTO
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ShouldReturnPaginatedMerchants()
    {
        // ARRANGE
        var municipality = new Municipality
        {
            Id = 1,
            Code = "25001",
            Name = "BOGOTÁ",
            DepartmentId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var merchants = new List<Merchant>
        {
            new Merchant
            {
                Id = 1,
                Name = "Empresa 1",
                MunicipalityId = 1,
                Municipality = municipality,
                Phone = "+573001234567",
                Email = "empresa1@test.com",
                Status = "Activo",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Establishments = new List<Establishment>()
            }
        };

        _mockRepository.Setup(r => r.GetAllAsync(1, 5, null, null, default))
            .ReturnsAsync((merchants, 1));

        // ACT
        var result = await _service.GetAllAsync(1, 5, null, null, default);

        // ASSERT
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal(1, result.TotalRecords);
        Assert.Equal("Empresa 1", result.Data.First().Name);
    }

    #endregion

    #region GetByIdAsync Tests

    /// <summary>
    /// ? PRUEBA 2: Obtener comerciante por ID con servicio
    /// Objetivo: Verificar que retorna DTO con información del municipio y departamento
    /// Resultado esperado: MerchantDto con municipio y departamento cargados
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnMerchantDTO()
    {
        // ARRANGE
        var department = new Department
        {
            Id = 1,
            Code = "25",
            Name = "CUNDINAMARCA",
            Region = "ANDINA",
            CreatedAt = DateTime.UtcNow
        };

        var municipality = new Municipality
        {
            Id = 1,
            Code = "25001",
            Name = "BOGOTÁ",
            DepartmentId = 1,
            CreatedAt = DateTime.UtcNow,
            Department = department
        };

        var merchant = new Merchant
        {
            Id = 1,
            Name = "Empresa Test",
            MunicipalityId = 1,
            Municipality = municipality,
            Phone = "+573001234567",
            Email = "empresa@test.com",
            Status = "Activo",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Establishments = new List<Establishment>()
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, default))
            .ReturnsAsync(merchant);

        // ACT
        var result = await _service.GetByIdAsync(1, default);

        // ASSERT
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Empresa Test", result.Name);
        Assert.Equal("BOGOTÁ", result.MunicipalityName);
        Assert.Equal("CUNDINAMARCA", result.DepartmentName);
    }

    /// <summary>
    /// ? PRUEBA 3: Obtener comerciante inexistente
    /// Objetivo: Verificar que retorna null cuando no existe
    /// Resultado esperado: null
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // ARRANGE
        _mockRepository.Setup(r => r.GetByIdAsync(999, default))
            .ReturnsAsync((Merchant?)null);

        // ACT
        var result = await _service.GetByIdAsync(999, default);

        // ASSERT
        Assert.Null(result);
    }

    #endregion

    #region CreateAsync Tests

    /// <summary>
    /// ? PRUEBA 4: Crear un nuevo comerciante exitosamente
    /// Objetivo: Verificar que el servicio valida y crea el comerciante
    /// Resultado esperado: MerchantDto con ID asignado y datos correctos
    /// </summary>
    [Fact]
    public async Task CreateAsync_WithValidDTO_ShouldCreateMerchant()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var dto = new CreateMerchantDto
        {
            Name = "Nueva Empresa",
            MunicipalityId = 1,
            Phone = "+573001234567",
            Email = "nueva@test.com"
        };

        var municipality = new Municipality
        {
            Id = 1,
            Code = "25001",
            Name = "BOGOTÁ",
            DepartmentId = 1,
            CreatedAt = DateTime.UtcNow
        };

        var createdMerchant = new Merchant
        {
            Id = 1,
            Name = dto.Name,
            MunicipalityId = dto.MunicipalityId,
            Municipality = municipality,
            Phone = dto.Phone,
            Email = dto.Email,
            Status = "Activo",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            Establishments = new List<Establishment>()
        };

        // Mock validator - retorna validación exitosa
        _mockCreateValidator.Setup(v => v.ValidateAsync(It.IsAny<CreateMerchantDto>(), default))
            .ReturnsAsync(new ValidationResult());

        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<Merchant>(), default))
            .ReturnsAsync(createdMerchant);

        // ACT
        var result = await _service.CreateAsync(dto, userId, default);

        // ASSERT
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Nueva Empresa", result.Name);
        Assert.Equal(1, result.MunicipalityId);
        Assert.Equal("BOGOTÁ", result.MunicipalityName);
    }

    #endregion
}
