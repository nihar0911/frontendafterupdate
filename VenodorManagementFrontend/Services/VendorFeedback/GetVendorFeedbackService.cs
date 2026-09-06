using System; using System.Collections.Generic; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Models.VendorPerformance;
using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.VendorFeedback
{
    public class GetVendorFeedbackService
    {
        private readonly ApiClient _api;
        public GetVendorFeedbackService(ApiClient api) => _api = api;
        public async Task<VendorReviewDto?> GetByIdAsync(int feedbackId)
        {
            _api.SetAuthHeader();
            try { return await _api.Http.GetFromJsonAsync<VendorReviewDto>($"api/vendor-feedback/{feedbackId}"); }
            catch { return null; }
        }
        public async Task<List<VendorReviewDto>> GetReviewsAsync(int vendorId)
        {
            _api.SetAuthHeader();
            try { return await _api.Http.GetFromJsonAsync<List<VendorReviewDto>>($"api/vendor-feedback/vendor/{vendorId}") ?? new(); }
            catch { return new(); }
        }
        public async Task<List<EligibleReviewOrderDto>> GetEligibleReviewOrdersAsync()
        {
            _api.SetAuthHeader();
            try { return await _api.Http.GetFromJsonAsync<List<EligibleReviewOrderDto>>("api/vendor-feedback/eligible-orders") ?? new(); }
            catch { return new(); }
        }
        public async Task<ApiResult> CreateReviewAsync(CreateVendorReviewRequest req)
        {
            _api.SetAuthHeader();
            try
            {
                var resp = await _api.Http.PostAsJsonAsync("api/vendor-feedback", req);
                if (resp.IsSuccessStatusCode) return new ApiResult { Success = true };
                return new ApiResult { Success = false, ErrorMessage = await _api.ReadErrorMessageAsync(resp, "Unable to create review.") };
            }
            catch (Exception ex) { return new ApiResult { Success = false, ErrorMessage = ex.Message }; }
        }
    }
}
