using Application.DTOs.Merchants;
using FluentValidation;

namespace Application.Validators.Merchants
{
    /// <summary>
    /// Validador para UpdateMerchantDto.
    /// Se ejecuta antes de actualizar un comerciante existente.
    /// </summary>
    public class UpdateMerchantValidator : AbstractValidator<UpdateMerchantDto>
    {
        public UpdateMerchantValidator()
        {
            // Validar Name
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("El nombre o razón social es requerido.")
                .Length(3, 200)
                .WithMessage("El nombre debe tener entre 3 y 200 caracteres.")
                .Matches(@"^[a-zA-Z0-9\s\-&áéíóúñÁÉÍÓÚÑ]+$")
                .WithMessage("El nombre contiene caracteres no permitidos.");

            // Validar MunicipalityId
            RuleFor(x => x.MunicipalityId)
                .GreaterThan(0)
                .WithMessage("El ID del municipio es requerido y debe ser válido.");

            // Validar Phone (opcional pero si se proporciona, debe ser válido)
            RuleFor(x => x.Phone)
                .Matches(@"^\+?[0-9]{7,15}$")
                .When(x => !string.IsNullOrEmpty(x.Phone))
                .WithMessage("El teléfono debe tener entre 7 y 15 dígitos.");

            // Validar Email (opcional pero si se proporciona, debe ser válido)
            RuleFor(x => x.Email)
                .EmailAddress()
                .When(x => !string.IsNullOrEmpty(x.Email))
                .WithMessage("El correo electrónico no tiene un formato válido.")
                .Length(5, 100)
                .When(x => !string.IsNullOrEmpty(x.Email))
                .WithMessage("El email debe tener entre 5 y 100 caracteres.");

            // Validar Status
            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("El estado es requerido.")
                .Must(x => x == "Activo" || x == "Inactivo")
                .WithMessage("El estado debe ser 'Activo' o 'Inactivo'.");
        }
    }
}
