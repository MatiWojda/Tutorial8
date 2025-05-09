using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;


public interface IClientService
{
    public Task<List<ClientTripDTO>> GetTrip(int clientId);
    public Task<int> CreateClient(ClientDTO client);
    public Task RegisterClientOnTrip(int clientId, int tripId);
    public Task UnregisterClientFromTrip(int clientId, int tripId);
    public Task<Boolean> DoesTripExist(int id);
    public Task<Boolean> DoesClientExist(int id);
    public Task<Boolean> ValidateClient(ClientDTO client);

}