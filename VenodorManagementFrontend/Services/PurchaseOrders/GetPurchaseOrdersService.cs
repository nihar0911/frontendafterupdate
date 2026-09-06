using System.Collections.Generic; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.PurchaseOrders
{
    public class GetPurchaseOrdersService
    {
        private readonly ApiClient _api;
        public GetPurchaseOrdersService(ApiClient api) => _api = api;
        public async Task<List<PurchaseOrderDto>?> GetAsync() { _api.SetAuthHeader(); try { var r = await _api.Http.GetFromJsonAsync<GetPurchaseOrdersResponse>("api/purchase-orders"); return r?.PurchaseOrders; } catch { return new(); } }
        public async Task<List<PurchaseOrderDto>> GetPendingByVendorAsync(int vendorId) { _api.SetAuthHeader(); try { var r = await _api.Http.GetFromJsonAsync<GetPurchaseOrdersResponse>($"api/purchase-orders/vendor/{vendorId}/pending"); return r?.PurchaseOrders ?? new(); } catch { return new(); } }
    }
}
