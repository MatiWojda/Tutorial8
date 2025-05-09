using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [ApiController]
    [Route("api/clients")]
    public class ClientsController : ControllerBase
    {
        private readonly IClientService _service;

        public ClientsController(IClientService service)
        {
            _service = service;
        }

        [HttpGet("{id}/trips")]
        public async Task<IActionResult> GetClientTrips(int id)
        {
            if (id <= 0)
                return BadRequest("Client ID must be greater than zero.");

            if (!await _service.DoesClientExist(id))
                return NotFound($"No client found with ID {id}.");

            var trips = await _service.GetTrip(id);
            if (trips == null || !trips.Any())
                return NotFound($"Client with ID {id} has no trips assigned.");

            return Ok(trips);
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewClient([FromBody] ClientDTO clientDto)
        {
            if (clientDto is null)
                return BadRequest("Client data is required.");

            if (!await _service.ValidateClient(clientDto))
                return BadRequest("Client data failed validation.");

            var clientId = await _service.CreateClient(clientDto);
            if (clientId <= 0)
                return BadRequest("Unable to create client.");

            return CreatedAtAction(nameof(GetClientTrips), new { id = clientId }, new { clientId });
        }

        [HttpPut("{id}/trips/{tripId}")]
        public async Task<IActionResult> AddClientToTrip(int id, int tripId)
        {
            if (id <= 0 || tripId <= 0)
                return BadRequest("Client and Trip IDs must be greater than zero.");

            if (!await _service.DoesClientExist(id))
                return NotFound($"Client with ID {id} does not exist.");

            if (!await _service.DoesTripExist(tripId))
                return NotFound($"Trip with ID {tripId} does not exist.");

            try
            {
                await _service.RegisterClientOnTrip(id, tripId);
                return Ok("Client successfully registered for the trip.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}/trips/{tripId}")]
        public async Task<IActionResult> RemoveClientFromTrip(int id, int tripId)
        {
            if (id <= 0 || tripId <= 0)
                return BadRequest("Client and Trip IDs must be positive values.");

            if (!await _service.DoesClientExist(id))
                return NotFound($"Client with ID {id} does not exist.");

            if (!await _service.DoesTripExist(tripId))
                return NotFound($"Trip with ID {tripId} does not exist.");

            try
            {
                await _service.UnregisterClientFromTrip(id, tripId);
                return Ok("Client successfully unregistered from the trip.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
