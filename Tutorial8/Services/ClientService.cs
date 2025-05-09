using Tutorial8.Models.DTOs;
using Microsoft.Data.SqlClient;

namespace Tutorial8.Services
{
    public class ClientService : IClientService
    {
        private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=APBD;Integrated Security=True;";

        public async Task<List<ClientTripDTO>> GetTrip(int clientId)
        {
            using var conn = new SqlConnection(_connectionString);
            var trips = new List<ClientTripDTO>();
			//Zapytanie pobierające dane wycieczek (nazwa, opis, daty) oraz dane rejestracji klienta (data rejestracji, data płatności)
            //dla konkretnego klienta, łącząc tabele Client_Trip i Trip.
            const string sql = "SELECT t.IdTrip,t.Name,t.Description,t.DateFrom,t.DateTo,ct.RegisteredAt,ct.PaymentDate FROM Client_Trip ct INNER JOIN Trip t ON ct.IdTrip=t.IdTrip WHERE ct.IdClient=@c";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@c", clientId);
            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                trips.Add(new ClientTripDTO
                {
                    IdTrip = rdr.GetInt32(0),
                    Name = rdr.GetString(1),
                    Description = rdr.IsDBNull(2)?null:rdr.GetString(2),
                    DateFrom = rdr.GetDateTime(3),
                    DateTo = rdr.GetDateTime(4),
                    RegisteredAt = rdr.GetInt32(5),
                    PaymentDate = rdr.IsDBNull(6)?(int?)null:rdr.GetInt32(6)
                });
            }
            return trips;
        }

        public async Task<int> CreateClient(ClientDTO client)
        {
			//Zapytanie wstawiające nowego klienta do tabeli Client.
            //OUTPUT INSERTED.IdClient zwraca ID nowo wstawionego rekordu.
            const string insertSql = @"
                INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
                OUTPUT INSERTED.IdClient
                VALUES (@first, @last, @email, @phone, @pesel)";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(insertSql, conn);
            cmd.Parameters.AddWithValue("@first", client.FirstName);
            cmd.Parameters.AddWithValue("@last", client.LastName);
            cmd.Parameters.AddWithValue("@email", client.Email);
            cmd.Parameters.AddWithValue("@phone", client.Telephone);
            cmd.Parameters.AddWithValue("@pesel", client.Pesel);

            await conn.OpenAsync();
            var id = (int)await cmd.ExecuteScalarAsync();
            return id;
        }

        public async Task RegisterClientOnTrip(int clientId, int tripId)
        {
			//SQL do zliczenia aktualnej liczby zapisanych osób na wycieczkę
            const string countSql = "SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @tripId";
			// SQL do pobrania maksymalnej liczby miejsc na wycieczce
            const string capSql = "SELECT MaxPeople FROM Trip WHERE IdTrip = @tripId";
			// SQL do sprawdzenia, czy klient jest już zapisany na tę konkretną wycieczkę
            const string existsSql = "SELECT COUNT(*) FROM Client_Trip WHERE IdClient = @clientId AND IdTrip = @tripId";
			// SQL do dodania nowego wpisu rejestracji klienta na wycieczkę
            const string addSql = "INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate) VALUES (@clientId, @tripId, @regAt, NULL)";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            var countCmd = new SqlCommand(countSql, conn, tx);
            var capCmd = new SqlCommand(capSql, conn, tx);
            var existsCmd = new SqlCommand(existsSql, conn, tx);
            countCmd.Parameters.AddWithValue("@tripId", tripId);
            capCmd.Parameters.AddWithValue("@tripId", tripId);
            existsCmd.Parameters.AddWithValue("@clientId", clientId);
            existsCmd.Parameters.AddWithValue("@tripId", tripId);

            var current = (int)await countCmd.ExecuteScalarAsync();
            var capacity = (int)await capCmd.ExecuteScalarAsync();
            var already = (int)await existsCmd.ExecuteScalarAsync();

            if (already > 0)
            {
                tx.Rollback();
                throw new InvalidOperationException("Client is already registered for this trip.");
            }
            if (current >= capacity)
            {
                tx.Rollback();
                throw new InvalidOperationException("Trip capacity reached.");
            }

            var addCmd = new SqlCommand(addSql, conn, tx);
            addCmd.Parameters.AddWithValue("@clientId", clientId);
            addCmd.Parameters.AddWithValue("@tripId", tripId);
            addCmd.Parameters.AddWithValue("@regAt", int.Parse(DateTime.Now.ToString("yyyyMMdd")));
            await addCmd.ExecuteNonQueryAsync();

            tx.Commit();
        }

        public async Task UnregisterClientFromTrip(int clientId, int tripId)
        {
			//SQL do sprawdzenia, czy klient jest zarejestrowany na daną wycieczkę (zwraca 1 jeśli tak, inaczej NULL lub nic)
            const string existsSql = "SELECT 1 FROM Client_Trip WHERE IdClient = @clientId AND IdTrip = @tripId";
			//SQL do usunięcia wpisu o rejestracji klienta na wycieczkę
            const string delSql = "DELETE FROM Client_Trip WHERE IdClient = @clientId AND IdTrip = @tripId";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            var existsCmd = new SqlCommand(existsSql, conn, tx);
            existsCmd.Parameters.AddWithValue("@clientId", clientId);
            existsCmd.Parameters.AddWithValue("@tripId", tripId);

            var found = await existsCmd.ExecuteScalarAsync();
            if (found == null)
            {
                tx.Rollback();
                throw new InvalidOperationException("Client is not registered for this trip.");
            }

            var delCmd = new SqlCommand(delSql, conn, tx);
            delCmd.Parameters.AddWithValue("@clientId", clientId);
            delCmd.Parameters.AddWithValue("@tripId", tripId);
            await delCmd.ExecuteNonQueryAsync();

            tx.Commit();
        }

        public async Task<bool> DoesClientExist(int idClient)
        {
			//Zapytanie zliczające klientów o podanym ID. Jeśli wynik > 0, klient istnieje.
            const string checkSql = "SELECT COUNT(*) FROM Client WHERE IdClient = @id";
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(checkSql, conn);
            cmd.Parameters.AddWithValue("@id", idClient);
            await conn.OpenAsync();
            return (int)await cmd.ExecuteScalarAsync() > 0;
        }

        public async Task<bool> DoesTripExist(int id)
        {
			//Zapytanie zliczające wycieczki o podanym ID. Jeśli wynik > 0, wycieczka istnieje.
            const string checkSql = "SELECT COUNT(*) FROM Trip WHERE IdTrip = @id";
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(checkSql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await conn.OpenAsync();
            return (int)await cmd.ExecuteScalarAsync() > 0;
        }

        public async Task<bool> ValidateClient(ClientDTO client)
        {
            if (string.IsNullOrWhiteSpace(client.FirstName)
                || string.IsNullOrWhiteSpace(client.LastName)
                || string.IsNullOrWhiteSpace(client.Email)
                || string.IsNullOrWhiteSpace(client.Telephone)
                || string.IsNullOrWhiteSpace(client.Pesel))
                return false;

            if (!client.Telephone.StartsWith("+")
                || client.Telephone.Length < 10
                || client.Telephone.Length > 15)
                return false;

            if (client.Pesel.Length != 11 || !long.TryParse(client.Pesel, out _))
                return false;

            if (!client.Email.Contains("@") || !client.Email.Contains("."))
                return false;

            return true;
        }
    }
}