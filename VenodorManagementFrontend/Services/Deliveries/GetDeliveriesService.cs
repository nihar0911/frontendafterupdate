using System.Collections.Generic; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Deliveries
{
    public class GetDeliveriesService
    {
        private readonly ApiClient _api;
        public GetDeliveriesService(ApiClient api) => _api = api;
        public async Task<List<DeliveryRecordDto>> GetByPurchaseOrderAsync(int purchaseOrderId) { _api.SetAuthHeader(); try { var r = await _api.Http.GetFromJsonAsync<GetDeliveryRecordsResponse>($"api/delivery-records/po/{purchaseOrderId}"); return r?.Deliveries ?? new(); } catch { return new(); } }
    }
}
