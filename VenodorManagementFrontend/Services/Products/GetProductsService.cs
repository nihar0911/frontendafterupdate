using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Products
{
    public class GetProductsService
    {
        private readonly ApiClient _api;
        public GetProductsService(ApiClient api) => _api = api;
        public async Task<List<ProductDto>> GetAsync() { _api.SetAuthHeader(); try { var r = await _api.Http.GetFromJsonAsync<GetProductsResponse>("api/products"); return r?.Products ?? new(); } catch { return new(); } }
    }
}
