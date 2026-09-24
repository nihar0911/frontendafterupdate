using System.Collections.Generic; using System.Net.Http.Json; using System.Threading.Tasks;
using VendorManagement.Web.Models; using VendorManagement.Web.Services.Infrastructure;
namespace VendorManagement.Web.Services.Vendors
{
    public class GetVendorsService
    {
        private readonly ApiClient _api;
        public GetVendorsService(ApiClient api) => _api = api;
        public async Task<List<VendorDto>> GetAsync() { _api.SetAuthHeader(); try { var r = await _api.Http.GetFromJsonAsync<GetVendorsResponse>("api/vendors"); return r?.Vendors ?? new(); } catch { return new(); } }
    }
}
