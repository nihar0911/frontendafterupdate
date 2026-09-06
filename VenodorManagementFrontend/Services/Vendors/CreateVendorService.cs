using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Vendors
{
    public class CreateVendorService
    {
        private readonly ApiClient _api;
        public CreateVendorService(ApiClient api) => _api = api;
        public async Task<ApiResult<VendorDto>> ExecuteAsync(CreateVendorCommand c) { _api.SetAuthHeader(); try { var resp = await _api.Http.PostAsJsonAsync("api/vendors", c); if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<CreateVendorResponse>(); return new ApiResult<VendorDto> { Success = true, Data = r?.Vendor }; } return new ApiResult<VendorDto> { Success = false, ErrorMessage = await _api.ReadErrorMessageAsync(resp, "Unable to create vendor.") }; } catch (Exception ex) { return new ApiResult<VendorDto> { Success = false, ErrorMessage = ex.Message }; } }
    }
}
