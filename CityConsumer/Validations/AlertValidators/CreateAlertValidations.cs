using CityConsumer.Models;
using FluentValidation;

namespace CityConsumer.Validations.AlertValidators
{
    public class CreateAlertValidations : AbstractValidator<Alerts>
    {
        public CreateAlertValidations()
        {
            RuleFor(x => x.AlertId)
                .NotEmpty().WithMessage("El id de la alerta es requerido")
                .MaximumLength(50).WithMessage("El id de la alerta no debe exceder los 50 caracteres")
                .Matches("^[a-zA-Z0-9-]+$").WithMessage("El id de la alerta solo debe contener caracteres alfanuméricos y guiones"); 

            RuleFor(x => x.CorrelationId)
                .NotEmpty().WithMessage("El id de correlación es requerido")
                .MaximumLength(50).WithMessage("El id de correlación no debe exceder los 50 caracteres")
                .Matches("^[a-zA-Z0-9-]+$").WithMessage("El id de correlación solo debe contener caracteres alfanuméricos y guiones");

            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("El tipo de alerta es requerido")
                .MaximumLength(100).WithMessage("El tipo de alerta no debe exceder los 100 caracteres")
                .Must(type => new[] { "possible_robbery", "accident" }.Contains(type))
                .WithMessage("El tipo de alerta debe ser 'possible_robbery' o 'accident'");

            RuleFor(x => x.Score)
                .InclusiveBetween(0, 100).WithMessage("El puntaje debe estar entre 0 y 100");

            RuleFor(x => x.Zone)
                .NotEmpty().WithMessage("La zona es requerida")
                .MaximumLength(100).WithMessage("La zona no debe exceder los 100 caracteres");

            RuleFor(x => x.WindowStart)
                .LessThan(x => x.WindowEnd).WithMessage("El inicio de la ventana debe ser anterior al final de la ventana");
            RuleFor(x => x.WindowEnd)
                .GreaterThan(x => x.WindowStart).WithMessage("El final de la ventana debe ser posterior al inicio de la ventana");

            RuleFor(x => x.CreatedAt)
                .LessThanOrEqualTo(DateTimeOffset.UtcNow).WithMessage("La fecha de creación no puede ser en el futuro");
        }
    }
}
