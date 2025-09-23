using System.Text.Json;
using CityProducer.Interfaces;
using CityProducer.Models;
using Confluent.Kafka;
using FluentValidation;
using FluentValidation.Results;
using Lombok.NET;
using Microsoft.AspNetCore.Mvc;

namespace CityProducer.Controllers
{
    [AllArgsConstructor]
    [Route("api/v1/[controller]")]
    [ApiController]
    public partial class EventController : ControllerBase
    {
        private readonly IProducerService _producerService;
        private readonly IValidator<Events> _eventValidator;
        private readonly IValidator<BulkEvents> _bulkEventValidator;

        [HttpPost]
        public async Task<IActionResult> Post(Events events)
        {
            var validationResult = await _eventValidator.ValidateAsync(events);

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

            var message = JsonSerializer.Serialize(events);

            var response = await _producerService.ProduceAsync(Constants.EVENTS_TOPIC, message);

            Response<DeliveryResult<string, string>> result = new Response<DeliveryResult<string, string>>
            {
                Result = response.Result
            };

            if (response.IsSuccess)
            {
                result.IsSuccess = true;
                result.Message = "Evento enviado a Kafka correctamente";
                return Ok(result);
            }

            result.IsSuccess = false;
            result.Message = "Error al enviar el evento a Kafka";
            return BadRequest(result);
        }

        [HttpPost("Bulk")]
        public async Task<IActionResult> PostBulk(BulkEvents events)
        {
            var validationResult = await _bulkEventValidator.ValidateAsync(events);
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

            List<Response<DeliveryResult<string, string>>> responses = new List<Response<DeliveryResult<string, string>>>();

            foreach (var singleEvent in events.Events!)
            {
                var message = JsonSerializer.Serialize(singleEvent);
                var response = await _producerService.ProduceAsync(Constants.EVENTS_TOPIC, message);
                responses.Add(response);
            }

            Response<List<Response<DeliveryResult<string, string>>>> result = new Response<List<Response<DeliveryResult<string, string>>>>
            {
                Result = responses
            };

            if (responses.All(r => r.IsSuccess))
            {
                result.IsSuccess = true;
                result.Message = "Todos los eventos fueron enviados a Kafka correctamente";
                return Ok(result);
            }

            result.IsSuccess = false;
            result.Message = "Algunos eventos no pudieron ser enviados a Kafka";

            return BadRequest(result);
        }
    }
}
