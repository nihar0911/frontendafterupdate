using System.Collections.Generic; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.TaxRates
{
    public class GetTaxRatesService
    {
        private readonly ApiClient _api;
        public GetTaxRatesService(ApiClient api) => _api = api;
        public async Task<List<TaxRateDto>> GetAsync() { _api.SetAuthHeader(); try { var r = await _api.Http.GetFromJsonAsync<GetTaxRatesResponse>("api/tax-rates"); return r?.TaxRates ?? new(); } catch { return new(); } }
    }
}
