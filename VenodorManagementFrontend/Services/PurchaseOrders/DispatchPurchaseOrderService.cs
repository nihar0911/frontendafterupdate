using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VendorManagement.Web.Models; using VendorManagement.Web.Services.Infrastructure;
namespace VendorManagement.Web.Services.PurchaseOrders
{
    public class DispatchPurchaseOrderService
    {
        private readonly ApiClient _api;
        public DispatchPurchaseOrderService(ApiClient api) => _api = api;
        public async Task<PurchaseOrderDto?> ExecuteAsync(DispatchPurchaseOrderCommand c) { _api.SetAuthHeader(); try { var resp = await _api.Http.PostAsJsonAsync("api/purchase-orders/dispatch", c); if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<DispatchPurchaseOrderResponse>(); return r?.PurchaseOrder; } return null; } catch { return null; } }
    }
}
