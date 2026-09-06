using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Vendors
{
    public class UpdateVendorService
    {
        private readonly ApiClient _api;
        public UpdateVendorService(ApiClient api) => _api = api;
        public async Task<ApiResult<VendorDto>> ExecuteAsync(int id, UpdateVendorCommand c) { _api.SetAuthHeader(); try { var resp = await _api.Http.PutAsJsonAsync($"api/vendors/{id}", c); if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<UpdateVendorResponse>(); return new ApiResult<VendorDto> { Success = true, Data = r?.Vendor }; } return new ApiResult<VendorDto> { Success = false, ErrorMessage = await _api.ReadErrorMessageAsync(resp, "Unable to update vendor.") }; } catch (Exception ex) { return new ApiResult<VendorDto> { Success = false, ErrorMessage = ex.Message }; } }
    }
}
