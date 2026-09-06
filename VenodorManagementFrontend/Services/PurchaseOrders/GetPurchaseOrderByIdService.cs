using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.PurchaseOrders
{
    public class GetPurchaseOrderByIdService
    {
        private readonly ApiClient _api;
        public GetPurchaseOrderByIdService(ApiClient api) => _api = api;
        public async Task<PurchaseOrderDto?> GetAsync(int id) { _api.SetAuthHeader(); try { var r = await _api.Http.GetFromJsonAsync<GetPurchaseOrderByIdResponse>($"api/purchase-orders/{id}"); return r?.PurchaseOrder; } catch { return null; } }
    }
}
