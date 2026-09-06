using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Products
{
    public class CreateProductService
    {
        private readonly ApiClient _api;
        public CreateProductService(ApiClient api) => _api = api;
        public async Task<ApiResult<ProductDto>> ExecuteAsync(CreateProductCommand c) { _api.SetAuthHeader(); try { var resp = await _api.Http.PostAsJsonAsync("api/products", c); if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<CreateProductResponse>(); return new ApiResult<ProductDto> { Success = true, Data = r?.Product }; } return new ApiResult<ProductDto> { Success = false, ErrorMessage = await _api.ReadErrorMessageAsync(resp, "Unable to create product.") }; } catch (Exception ex) { return new ApiResult<ProductDto> { Success = false, ErrorMessage = ex.Message }; } }
    }
}
