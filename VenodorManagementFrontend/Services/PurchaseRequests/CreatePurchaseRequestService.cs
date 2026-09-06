using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.PurchaseRequests
{
    public class CreatePurchaseRequestService
    {
        private readonly ApiClient _api;
        public CreatePurchaseRequestService(ApiClient api) => _api = api;
        public async Task<PurchaseRequestDto?> ExecuteAsync(CreatePurchaseRequestCommand c) { _api.SetAuthHeader(); try { var resp = await _api.Http.PostAsJsonAsync("api/purchase-requests", c); if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<CreatePurchaseRequestResponse>(); return r?.PurchaseRequest; } return null; } catch { return null; } }
    }
}
