using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.PurchaseRequests
{
    public class GetPurchaseRequestByIdService
    {
        private readonly ApiClient _api;
        public GetPurchaseRequestByIdService(ApiClient api) => _api = api;
        public async Task<PurchaseRequestDto?> GetAsync(int id) { _api.SetAuthHeader(); try { return await _api.Http.GetFromJsonAsync<PurchaseRequestDto>($"api/purchase-requests/{id}"); } catch { return null; } }
    }
}
