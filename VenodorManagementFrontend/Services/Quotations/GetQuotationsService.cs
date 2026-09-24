using System.Collections.Generic; using System.Net.Http.Json; using System.Threading.Tasks;
using VendorManagement.Web.Models; using VendorManagement.Web.Services.Infrastructure;
namespace VendorManagement.Web.Services.Quotations
{
    public class GetQuotationsService
    {
        private readonly ApiClient _api;
        public GetQuotationsService(ApiClient api) => _api = api;
        public async Task<List<QuotationDto>> GetAsync() { _api.SetAuthHeader(); try { var r = await _api.Http.GetFromJsonAsync<GetQuotationsResponse>("api/quotations"); return r?.Quotations ?? new(); } catch { return new(); } }
    }
}
