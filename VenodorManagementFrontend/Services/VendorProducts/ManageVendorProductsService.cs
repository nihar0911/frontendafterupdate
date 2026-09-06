using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.VendorProducts
{
    public class ManageVendorProductsService
    {
        private readonly ApiClient _api;
        public ManageVendorProductsService(ApiClient api) => _api = api;
        public async Task<ApiResult<VendorProductDto>> CreateAsync(CreateVendorProductCommand c)
        {
            _api.SetAuthHeader();
            try
            {
                var resp = await _api.Http.PostAsJsonAsync("api/vendorproducts", c);
                if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<CreateVendorProductResponse>(); return new ApiResult<VendorProductDto> { Success = true, Data = r?.VendorProduct }; }
                return new ApiResult<VendorProductDto> { Success = false, ErrorMessage = await _api.ReadErrorMessageAsync(resp, "Unable to create vendor product mapping.") };
            }
            catch (Exception ex) { return new ApiResult<VendorProductDto> { Success = false, ErrorMessage = ex.Message }; }
        }
        public async Task<ApiResult<VendorProductDto>> UpdateAsync(int id, UpdateVendorProductCommand c)
        {
            _api.SetAuthHeader();
            try
            {
                var resp = await _api.Http.PutAsJsonAsync($"api/vendorproducts/{id}", c);
                if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<UpdateVendorProductResponse>(); return new ApiResult<VendorProductDto> { Success = true, Data = r?.VendorProduct }; }
                return new ApiResult<VendorProductDto> { Success = false, ErrorMessage = await _api.ReadErrorMessageAsync(resp, "Unable to update vendor product mapping.") };
            }
            catch (Exception ex) { return new ApiResult<VendorProductDto> { Success = false, ErrorMessage = ex.Message }; }
        }
        public async Task<ApiResult> DeleteAsync(int id)
        {
            _api.SetAuthHeader();
            try
            {
                var resp = await _api.Http.DeleteAsync($"api/vendorproducts/{id}");
                if (resp.IsSuccessStatusCode) return new ApiResult { Success = true };
                return new ApiResult { Success = false, ErrorMessage = await _api.ReadErrorMessageAsync(resp, "Unable to delete product.") };
            }
            catch (Exception ex) { return new ApiResult { Success = false, ErrorMessage = ex.Message }; }
        }
    }
}
