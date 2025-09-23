using CityProducer.Models;
using FluentValidation;

namespace CityProducer.Validations.EventValidators
{
    public class BulkEventValidator : AbstractValidator<BulkEvents>
    {
        public BulkEventValidator()
        {
            RuleFor(x => x.Events)
                .NotNull().WithMessage("La lista de eventos no puede ser nula.")
                .NotEmpty().WithMessage("La lista de eventos no puede estar vacía.")
                .Must(x => x.Count <= 100).WithMessage("Se pueden enviar un máximo de 100 eventos a la vez.")
                .When(x => x.Events != null);

            RuleForEach(x => x.Events).SetValidator(new CreateEventValidator());
        }
    }
}
