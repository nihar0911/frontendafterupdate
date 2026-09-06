using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.PurchaseOrders
{
    public class CreatePurchaseOrderService
    {
        private readonly ApiClient _api;
        public CreatePurchaseOrderService(ApiClient api) => _api = api;
        public async Task<PurchaseOrderDto?> ExecuteAsync(CreatePurchaseOrderCommand c) { _api.SetAuthHeader(); try { var resp = await _api.Http.PostAsJsonAsync("api/purchase-orders", c); if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<CreatePurchaseOrderResponse>(); return r?.PurchaseOrder; } return null; } catch { return null; } }
    }
}
