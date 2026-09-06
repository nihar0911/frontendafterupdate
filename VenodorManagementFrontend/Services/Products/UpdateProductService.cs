using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Products
{
    public class UpdateProductService
    {
        private readonly ApiClient _api;
        public UpdateProductService(ApiClient api) => _api = api;
        public async Task<ApiResult<ProductDto>> ExecuteAsync(int id, UpdateProductCommand c) { _api.SetAuthHeader(); try { var resp = await _api.Http.PutAsJsonAsync($"api/products/{id}", c); if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<UpdateProductResponse>(); return new ApiResult<ProductDto> { Success = true, Data = r?.Product }; } return new ApiResult<ProductDto> { Success = false, ErrorMessage = await _api.ReadErrorMessageAsync(resp, "Unable to update product.") }; } catch (Exception ex) { return new ApiResult<ProductDto> { Success = false, ErrorMessage = ex.Message }; } }
    }
}
