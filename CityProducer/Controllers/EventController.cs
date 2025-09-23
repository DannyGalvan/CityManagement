using System.Text.Json;
using CityProducer.Interfaces;
using CityProducer.Models;
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

        [HttpPost]
        public async Task<IActionResult> Post(Events events)
        {
            var message = JsonSerializer.Serialize(events);

            await _producerService.ProduceAsync("events.standardized", message);

            return Ok("evento enviado a kafka correctamente");
        }
    }
}
