using System; using System.Collections.Generic; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.VendorProcurement
{
    public class GetVendorProcurementService
    {
        private readonly ApiClient _api;
        public GetVendorProcurementService(ApiClient api) => _api = api;
        public async Task<List<VendorProcurementOpportunityDto>> GetOpportunitiesAsync()
        {
            _api.SetAuthHeader();
            try { var r = await _api.Http.GetFromJsonAsync<GetVendorProcurementOpportunitiesResponse>("api/purchase-requests/vendor/opportunities"); return r?.Opportunities ?? new(); }
            catch { return new(); }
        }
        public async Task<ApiResult<RespondToOpportunityResponse>> RespondToOpportunityAsync(RespondToOpportunityCommand command)
        {
            _api.SetAuthHeader();
            try
            {
                var resp = await _api.Http.PostAsJsonAsync("api/purchase-requests/vendor/opportunities/respond", command);
                if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<RespondToOpportunityResponse>(); return new ApiResult<RespondToOpportunityResponse> { Success = true, Data = r }; }
                return new ApiResult<RespondToOpportunityResponse> { Success = false, ErrorMessage = await _api.ReadErrorMessageAsync(resp, "Unable to respond to opportunity.") };
            }
            catch (Exception ex) { return new ApiResult<RespondToOpportunityResponse> { Success = false, ErrorMessage = ex.Message }; }
        }
    }
}
