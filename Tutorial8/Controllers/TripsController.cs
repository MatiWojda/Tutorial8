using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tutorial8.Services;
using Microsoft.Data.SqlClient;

namespace Tutorial8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly ITripsService _tripsService;

        public TripsController(ITripsService tripsService)
        {
            _tripsService = tripsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTrips()
        {
            try
            {
                var trips = await _tripsService.GetAllTripsAsync();
                return Ok(trips);
            }
            // Łapanie specyficznych wyjątków SQL
            catch (SqlException ex)
            {
                Console.WriteLine($"Database error occurred in GetTrips: {ex.Message}");
                // Zwrócenie błędu 500 Internal Server Error
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd podczas komunikacji z bazą danych.");
            }
            // Łapanie innych, nieoczekiwanych wyjątków
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred in GetTrips: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił nieoczekiwany błąd serwera.");
            }
        }
    }
}
