using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services
{
    public class TripsService : ITripsService
    {
        private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=APBD;Integrated Security=True;";

        public async Task<List<TripDTO>> GetAllTripsAsync()
        {
            var list = new List<TripDTO>();
			//Zapytanie SQL pobierające dane wycieczek oraz powiązane z nimi kraje.
            //Każdy wiersz wyniku reprezentuje parę (wycieczka, jeden z jej krajów).
            //Jeśli wycieczka ma wiele krajów, pojawi się w wielu wierszach.
            const string command = @"SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.IdCountry, c.Name
                                 FROM Trip t LEFT JOIN Country_Trip ct ON t.IdTrip=ct.IdTrip
                                 LEFT JOIN Country c ON ct.IdCountry=c.IdCountry ORDER BY t.IdTrip";
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(command, conn);
            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
        
            while (await rdr.ReadAsync())
            {
                var id = rdr.GetInt32(0);
                var trip = list.FirstOrDefault(e => e.IdTrip == id);
                if (trip is null)
                {
                    trip = new TripDTO
                    {
                        IdTrip = id,
                        Name = rdr.GetString(1),
                        Description = rdr.IsDBNull(2)?null:rdr.GetString(2),
                        DateFrom = rdr.GetDateTime(3),
                        DateTo = rdr.GetDateTime(4),
                        MaxPeople = rdr.GetInt32(5),
                        Countries = new List<CountryDTO>()
                    };
                    list.Add(trip);
                }
                trip.Countries.Add(new CountryDTO { IdCountry = rdr.GetInt32(6), Name = rdr.GetString(7) });
            }
            return list;
        }
    }
}