using System.Collections.Generic; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.VendorProducts
{
    public class GetVendorProductsService
    {
        private readonly ApiClient _api;
        public GetVendorProductsService(ApiClient api) => _api = api;
        public async Task<List<VendorProductDto>> GetAsync() { _api.SetAuthHeader(); try { var r = await _api.Http.GetFromJsonAsync<GetVendorProductsResponse>("api/vendorproducts"); return r?.VendorProducts ?? new(); } catch { return new(); } }
    }
}
