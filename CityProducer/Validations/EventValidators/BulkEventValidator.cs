using CityProducer.Models;
using FluentValidation;

namespace CityProducer.Validations.EventValidators
{
    public class BulkEventValidator : AbstractValidator<List<EventsRequest>>
    {
        public BulkEventValidator(IValidator<EventsRequest> createValidator)
        {
            RuleFor(x => x)
                .NotNull().WithMessage("La lista de eventos no puede ser nula.")
                .NotEmpty().WithMessage("La lista de eventos no puede estar vacía.")
                .Must(x => x is { Count: <= 100 }).WithMessage("Se pueden enviar un máximo de 100 eventos a la vez.")
                .When(x => x != null);

            RuleForEach(x => x).SetValidator(createValidator);
        }
    }
}
