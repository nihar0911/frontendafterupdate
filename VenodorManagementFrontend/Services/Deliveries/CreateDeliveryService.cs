using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Deliveries
{
    public class CreateDeliveryService
    {
        private readonly ApiClient _api;
        public CreateDeliveryService(ApiClient api) => _api = api;
        public async Task<DeliveryRecordDto?> ExecuteAsync(CreateDeliveryRecordCommand c) { _api.SetAuthHeader(); try { var resp = await _api.Http.PostAsJsonAsync("api/deliveryrecords", c); if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<CreateDeliveryRecordResponse>(); return r?.DeliveryRecord; } return null; } catch { return null; } }
        public async Task<DeliveryRecordDto?> ConfirmAsync(ConfirmDeliveryRecordCommand c) { _api.SetAuthHeader(); try { var resp = await _api.Http.PostAsJsonAsync("api/deliveryrecords/confirm", c); if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<ConfirmDeliveryRecordResponse>(); return r?.DeliveryRecord; } return null; } catch { return null; } }
    }
}
