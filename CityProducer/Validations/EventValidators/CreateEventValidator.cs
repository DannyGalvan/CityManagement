using System.Text.Json;
using CityProducer.Models;
using FluentValidation;

namespace CityProducer.Validations.EventValidators
{
    public class CreateEventValidator : AbstractValidator<EventsRequest>
    {
        public CreateEventValidator()
        {
            RuleFor(x => x.EventId)
                .NotEmpty().WithMessage("El ID del evento es obligatorio.")
                .MaximumLength(50).WithMessage("El ID del evento no debe exceder los 50 caracteres.")
                .Matches("^[a-zA-Z0-9-]+$").WithMessage("El ID del evento solo debe contener caracteres alfanuméricos y guiones.");

            RuleFor(x => x.CorrelationId)
                .NotEmpty().WithMessage("El ID de correlación es obligatorio.")
                .MaximumLength(50).WithMessage("El ID de correlación no debe exceder los 50 caracteres.")
                .Matches("^[a-zA-Z0-9-]+$").WithMessage("El ID de correlación solo debe contener caracteres alfanuméricos y guiones.");

            RuleFor(x => x.Producer)
                .NotEmpty().WithMessage("El productor es obligatorio.")
                .MaximumLength(100).WithMessage("El productor no debe exceder los 100 caracteres.");

            RuleFor(x => x.EventType)
                .NotEmpty().WithMessage("El tipo de evento es obligatorio.")
                .MaximumLength(100).WithMessage("El tipo de evento no debe exceder los 100 caracteres.")
                .Must(eventType => new[] { "panic.button", "sensor.lpr", "sensor.speed", "sensor.acoustic" , "citizen.report" }.Contains(eventType))
                .WithMessage("El tipo de evento debe ser uno de los siguientes: panic.button, sensor.lpr, sensor.speed, sensor.acoustic, citizen.report");

            RuleFor(x => x.EventVersion)
                .NotEmpty().WithMessage("La versión del evento es obligatoria.")
                .Matches(@"^\d+\.\d+$").WithMessage("La versión del evento debe seguir el formato semántico (e.g., 1.0).");

            RuleFor(x => x.TimeStamp)
                .LessThanOrEqualTo(DateTimeOffset.UtcNow).WithMessage("La marca de tiempo no puede ser en el futuro.");

            RuleFor(x => x.PartitionKey)
                .NotEmpty().WithMessage("La clave de partición es obligatoria.")
                .MaximumLength(100).WithMessage("La clave de partición no debe exceder los 100 caracteres."); 
            
            RuleFor(x => x.Geo)
                .NotNull().WithMessage("La información geográfica es obligatoria.")
                .DependentRules(() =>
                {
                    RuleFor(x => x.Geo.Lat)
                        .InclusiveBetween(-90, 90).WithMessage("La latitud debe estar entre -90 y 90.");
                    RuleFor(x => x.Geo.Lon)
                        .InclusiveBetween(-180, 180).WithMessage("La longitud debe estar entre -180 y 180.");
                    RuleFor(x => x.Geo.Zone)
                        .NotEmpty().WithMessage("La zona es obligatoria.")
                        .MaximumLength(100).WithMessage("La zona no debe exceder los 100 caracteres.");
                });

            RuleFor(x => x.Severity)
                .NotEmpty().WithMessage("La severidad es obligatoria.")
                .Must(severity => new[] { "info", "warning", "critical" }.Contains(severity))
                .WithMessage("La severidad debe ser uno de los siguientes: info, warning, critical.");

            RuleFor(x => x.Payload)
                 .NotNull().WithMessage("El payload no puede ser nulo.")
                 .DependentRules(() =>
                 {
                     RuleFor(x => x)
                         .Must(x =>
                         {
                             // x.Payload es JsonDocument
                             var doc = x.Payload!;
                             var payload = doc.RootElement;

                             if (payload.ValueKind != JsonValueKind.Object)
                                 return false;

                             if (x.EventType == "panic.button")
                             {
                                 return payload.TryGetProperty("tipo_de_alerta", out var tipoDeAlerta) &&
                                        payload.TryGetProperty("identificador_dispositivo", out var identificadorDispositivo) &&
                                        payload.TryGetProperty("user_context", out var userContext) &&
                                        !string.IsNullOrEmpty(tipoDeAlerta.GetString()) &&
                                        !string.IsNullOrEmpty(identificadorDispositivo.GetString()) &&
                                        !string.IsNullOrEmpty(userContext.GetString()) &&
                                        new[] { "panico", "emergencia", "incendio" }.Contains(tipoDeAlerta.GetString()) &&
                                        new[] { "movil", "quiosco", "web" }.Contains(userContext.GetString());
                             }
                             else if (x.EventType == "sensor.lpr")
                             {
                                 return payload.TryGetProperty("placa_vehicular", out var placaVehicular) &&
                                        payload.TryGetProperty("velocidad_estimada", out var velocidadEstimada) &&
                                        payload.TryGetProperty("modelo_vehiculo", out var modeloVehiculo) &&
                                        payload.TryGetProperty("color_vehiculo", out var colorVehiculo) &&
                                        payload.TryGetProperty("ubicacion_sensor", out var ubicacionSensor) &&
                                        !string.IsNullOrEmpty(placaVehicular.GetString()) &&
                                        (velocidadEstimada.TryGetInt32(out var vel) ? vel > 0 : false) &&
                                        !string.IsNullOrEmpty(modeloVehiculo.GetString()) &&
                                        !string.IsNullOrEmpty(colorVehiculo.GetString()) &&
                                        !string.IsNullOrEmpty(ubicacionSensor.GetString());
                             }
                             else if (x.EventType == "sensor.speed")
                             {
                                 return payload.TryGetProperty("velocidad_detectada", out var velocidadDetectada) &&
                                        payload.TryGetProperty("sensor_id", out var sensorId) &&
                                        payload.TryGetProperty("direccion", out var direccion) &&
                                        (velocidadDetectada.TryGetInt32(out var vel) ? vel > 0 : false) &&
                                        !string.IsNullOrEmpty(sensorId.GetString()) &&
                                        !string.IsNullOrEmpty(direccion.GetString()) &&
                                        new[] { "NORTE", "SUR", "ESTE", "OESTE" }.Contains(direccion.GetString());
                             }
                             else if (x.EventType == "sensor.acoustic")
                             {
                                 return payload.TryGetProperty("tipo_sonido_detectado", out var tipoSonidoDetectado) &&
                                        payload.TryGetProperty("nivel_decibeles", out var nivelDecibeles) &&
                                        payload.TryGetProperty("probabilidad_evento_critico", out var probEvento) &&
                                        !string.IsNullOrEmpty(tipoSonidoDetectado.GetString()) &&
                                        (nivelDecibeles.TryGetInt32(out var db) ? db > 0 : false) &&
                                        (probEvento.TryGetSingle(out var p) ? p > 0 : false);
                             }
                             else if (x.EventType == "citizen.report")
                             {
                                 return payload.TryGetProperty("tipo_evento", out var tipoEvento) &&
                                        payload.TryGetProperty("mensaje_descriptivo", out var mensajeDescriptivo) &&
                                        payload.TryGetProperty("ubicacion_aproximada", out var ubicacionAproximada) &&
                                        payload.TryGetProperty("origen", out var origen) &&
                                        !string.IsNullOrEmpty(tipoEvento.GetString()) &&
                                        !string.IsNullOrEmpty(mensajeDescriptivo.GetString()) &&
                                        !string.IsNullOrEmpty(ubicacionAproximada.GetString()) &&
                                        !string.IsNullOrEmpty(origen.GetString()) &&
                                        new[] { "usuario", "app", "punto_fisico" }.Contains(origen.GetString());
                             }

                             return false;
                         })
                         .WithMessage("El payload del evento es inválido.");
                 });

        }
    }
}