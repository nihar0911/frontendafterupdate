using System.Collections.Generic; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.PurchaseRequests
{
    public class GetPurchaseRequestsService
    {
        private readonly ApiClient _api;
        public GetPurchaseRequestsService(ApiClient api) => _api = api;
        public async Task<List<PurchaseRequestDto>> GetAsync() { _api.SetAuthHeader(); try { var r = await _api.Http.GetFromJsonAsync<GetPurchaseRequestsResponse>("api/purchase-requests"); return r?.PurchaseRequests ?? new(); } catch { return new(); } }
    }
}
