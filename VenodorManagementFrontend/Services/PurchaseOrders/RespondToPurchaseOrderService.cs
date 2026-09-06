using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.PurchaseOrders
{
    public class RespondToPurchaseOrderService
    {
        private readonly ApiClient _api;
        public RespondToPurchaseOrderService(ApiClient api) => _api = api;
        public async Task<PurchaseOrderDto?> ExecuteAsync(RespondToPurchaseOrderCommand c) { _api.SetAuthHeader(); try { var resp = await _api.Http.PostAsJsonAsync("api/purchase-orders/respond", c); if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<RespondToPurchaseOrderResponse>(); return r?.PurchaseOrder; } return null; } catch { return null; } }
    }
}
