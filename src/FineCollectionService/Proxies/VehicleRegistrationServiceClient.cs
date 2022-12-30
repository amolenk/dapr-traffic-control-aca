namespace FineCollectionService.Proxies;

public class VehicleRegistrationServiceClient
{
    private HttpClient _httpClient;

    public VehicleRegistrationServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<VehicleInfo> GetVehicleInfo(string licenseNumber) =>
        await _httpClient.GetFromJsonAsync<VehicleInfo>($"vehicleinfo/{licenseNumber}");
}
