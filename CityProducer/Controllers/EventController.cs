using CityProducer.Context;
using CityProducer.Interfaces;
using CityProducer.Models;
using Confluent.Kafka;
using FluentValidation;
using FluentValidation.Results;
using Lombok.NET;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace CityProducer.Controllers
{
    [AllArgsConstructor]
    [ApiController]
    public partial class EventController : ControllerBase
    {
        private readonly IProducerService _producerService;
        private readonly IValidator<EventsRequest> _eventValidator;
        private readonly IValidator<List<EventsRequest>> _bulkEventValidator;
        private readonly ProducerContext  _context;

        [Route("/events")]
        [HttpPost]
        public async Task<IActionResult> Post(EventsRequest request)
        {
            try
            {
                var validationResult = await _eventValidator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    Response<List<ValidationFailure>> errorResponse = new Response<List<ValidationFailure>>
                    {
                        IsSuccess = false,
                        Message = "Error de validación",
                        Result = validationResult.Errors
                    };

                    return BadRequest(errorResponse);
                }

                var events = new Events
                {
                    EventVersion = request.EventVersion,
                    EventType = request.EventType,
                    EventId = request.EventId,
                    Producer = request.Producer,
                    Source = request.Source,
                    CorrelationId = request.CorrelationId,
                    TraceId = request.TraceId,
                    TsUtc = request.TimeStamp,
                    PartitionKey = request.PartitionKey,
                    GeoLat = request.Geo.Lat,
                    GeoLong = request.Geo.Lon,
                    Zone = request.Geo.Zone,
                    Severity = request.Severity,
                    Payload = request.Payload
                };

                var message = JsonSerializer.Serialize(events);

                var response = await _producerService.ProduceAsync(Constants.EVENTS_TOPIC, message);

                Response<DeliveryResult<string, string>> result = new Response<DeliveryResult<string, string>>
                {
                    Result = response.Result
                };

                if (response.IsSuccess)
                {
                    _context.Events.Add(events);
                    await _context.SaveChangesAsync();

                    result.IsSuccess = true;
                    result.Message = "Evento enviado a Kafka correctamente";
                    return Ok(result);
                }

                result.IsSuccess = false;
                result.Message = "Error al enviar el evento a Kafka";
                return BadRequest(result);
            }
            catch (Exception e)
            {
                Response<List<ValidationFailure>> errorResponse = new Response<List<ValidationFailure>>
                {
                    IsSuccess = false,
                    Message = "Error de validación",
                    Result = [new ValidationFailure("Exception",e.Message)]
                };

                return BadRequest(errorResponse);
            }
        }

        [Route("/events/bulk")]
        [HttpPost]
        public async Task<IActionResult> PostBulk(List<EventsRequest> bulkEvents)
        {
            try
            {
                var validationResult = await _bulkEventValidator.ValidateAsync(bulkEvents);
                if (!validationResult.IsValid)
                {
                    Response<List<ValidationFailure>> errorResponse = new Response<List<ValidationFailure>>
                    {
                        IsSuccess = false,
                        Message = "Error de validación",
                        Result = validationResult.Errors
                    };
                    return BadRequest(errorResponse);
                }

                List<Response<DeliveryResult<string, string>>> responses =
                    new List<Response<DeliveryResult<string, string>>>();

                foreach (var request in bulkEvents)
                {
                    var events = new Events
                    {
                        EventVersion = request.EventVersion,
                        EventType = request.EventType,
                        EventId = request.EventId,
                        Producer = request.Producer,
                        Source = request.Source,
                        CorrelationId = request.CorrelationId,
                        TraceId = request.TraceId,
                        TsUtc = request.TimeStamp,
                        PartitionKey = request.PartitionKey,
                        GeoLat = request.Geo.Lat,
                        GeoLong = request.Geo.Lon,
                        Zone = request.Geo.Zone,
                        Severity = request.Severity,
                        Payload = request.Payload
                    };

                    var message = JsonSerializer.Serialize(events);
                    var response = await _producerService.ProduceAsync(Constants.EVENTS_TOPIC, message);
                    responses.Add(response);
                }

                Response<List<Response<DeliveryResult<string, string>>>> result =
                    new Response<List<Response<DeliveryResult<string, string>>>>
                    {
                        Result = responses
                    };

                if (responses.All(r => r.IsSuccess))
                {
                    var events = bulkEvents.Select(request => new Events
                    {
                        EventVersion = request.EventVersion,
                        EventType = request.EventType,
                        EventId = request.EventId,
                        Producer = request.Producer,
                        Source = request.Source,
                        CorrelationId = request.CorrelationId,
                        TraceId = request.TraceId,
                        TsUtc = request.TimeStamp,
                        PartitionKey = request.PartitionKey,
                        GeoLat = request.Geo.Lat,
                        GeoLong = request.Geo.Lon,
                        Zone = request.Geo.Zone,
                        Severity = request.Severity,
                        Payload = request.Payload
                    });

                    _context.Events.AddRange(events);
                    await _context.SaveChangesAsync();

                    result.IsSuccess = true;
                    result.Message = "Todos los eventos fueron enviados a Kafka correctamente";
                    return Ok(result);
                }

                result.IsSuccess = false;
                result.Message = "Algunos eventos no pudieron ser enviados a Kafka";

                return BadRequest(result);
            }
            catch (Exception e)
            {
                Response<List<ValidationFailure>> errorResponse = new Response<List<ValidationFailure>>
                {
                    IsSuccess = false,
                    Message = "Error de validación",
                    Result = [new ValidationFailure("Exception", e.Message)]
                };

                return BadRequest(errorResponse);
            }
        }

        [HttpGet]
        [Route("/health")]
        public IActionResult Health()
        {
            return Ok(new { status = "Ok" });
        }

        [HttpGet]
        [Route("/schema")]
        public IActionResult Schema()
        {
            const string schemaJson = """
                                      {
                                        "$schema": "https://json-schema.org/draft/2020-12/schema",
                                        "$id": "https://example.edu/canonical-event/1-0.schema.json",
                                        "title": "CanonicalEventV1",
                                        "type": "object",
                                        "required": ["event_version","event_type","event_id","producer","source","timestamp","partition_key","geo","severity","payload"],
                                        "properties": {
                                          "event_version": {"type":"string","const":"1.0"},
                                          "event_type": {"type":"string","enum":["panic.button","sensor.lpr","sensor.speed","sensor.acoustic","citizen.report"]},
                                          "event_id": {"type":"string"},
                                          "producer": {"type":"string"},
                                          "source": {"type":"string","enum":["simulated"]},
                                          "correlation_id": {"type":"string"},
                                          "trace_id": {"type":"string"},
                                          "timestamp": {"type":"string","format":"date-time"},
                                          "partition_key": {"type":"string"},
                                          "geo": {
                                            "type":"object",
                                            "required":["zone"],
                                            "properties": {
                                              "zone": {"type":"string"},
                                              "lat": {"type":"number"},
                                              "lon": {"type":"number"}
                                            }
                                          },
                                          "severity": {"type":"string","enum":["info","warning","critical"]},
                                          "payload": {"type":"object"}
                                        },
                                        "additionalProperties": false
                                      }
                                      """;

            return Content(schemaJson, "application/schema+json", Encoding.UTF8);
        }
    }
}
