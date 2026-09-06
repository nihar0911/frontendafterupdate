using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.VendorRecommendations
{
    public class GetVendorRecommendationsService
    {
        private readonly ApiClient _api;
        public GetVendorRecommendationsService(ApiClient api) => _api = api;
        public async Task<VendorRecommendationResponse?> GetAsync(int purchaseRequestId) { _api.SetAuthHeader(); try { return await _api.Http.GetFromJsonAsync<VendorRecommendationResponse>($"api/vendor-recommendations/{purchaseRequestId}"); } catch { return null; } }
    }
}
