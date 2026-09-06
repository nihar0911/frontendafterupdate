using System.Collections.Generic; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models.VendorPerformance;
using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.VendorPerformance
{
    public class GetVendorPerformanceService
    {
        private readonly ApiClient _api;
        public GetVendorPerformanceService(ApiClient api) => _api = api;
        public async Task<List<VendorPerformanceSummaryDto>> GetSummariesAsync()
        {
            _api.SetAuthHeader();
            try { return await _api.Http.GetFromJsonAsync<List<VendorPerformanceSummaryDto>>("api/vendor-performance/summary") ?? new(); }
            catch { return new(); }
        }
        public async Task<VendorPerformanceSummaryDto?> GetByIdAsync(int vendorId)
        {
            _api.SetAuthHeader();
            try { return await _api.Http.GetFromJsonAsync<VendorPerformanceSummaryDto>($"api/vendor-performance/{vendorId}"); }
            catch { return null; }
        }
        public async Task<VendorAiInsightsDto?> GetAiInsightsAsync(int vendorId)
        {
            _api.SetAuthHeader();
            try { return await _api.Http.GetFromJsonAsync<VendorAiInsightsDto>($"api/vendor-performance/{vendorId}/ai-insights"); }
            catch { return null; }
        }
    }
}
