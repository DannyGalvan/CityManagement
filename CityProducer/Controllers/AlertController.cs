using CityProducer.Interfaces;
using CityProducer.Models;
using Confluent.Kafka;
using FluentValidation;
using FluentValidation.Results;
using Lombok.NET;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CityProducer.Controllers
{
    [AllArgsConstructor]
    [Route("api/v1/[controller]")]
    [ApiController]
    public partial class AlertController : ControllerBase
    {
        private readonly IProducerService _producerService;
        private readonly IValidator<Alerts> _eventValidator;

        [HttpPost]
        public async Task<IActionResult> Post(Alerts alert)
        {
            var validationResult = await _eventValidator.ValidateAsync(alert);

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

            var message = JsonSerializer.Serialize(alert);

            var response = await _producerService.ProduceAsync(Constants.ALERTS_TOPIC, message);

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
    }
}
