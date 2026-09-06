using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.TaxRates
{
    public class CreateTaxRateService
    {
        private readonly ApiClient _api;
        public CreateTaxRateService(ApiClient api) => _api = api;
        public async Task<TaxRateDto?> ExecuteAsync(CreateTaxRateCommand c) { _api.SetAuthHeader(); try { var resp = await _api.Http.PostAsJsonAsync("api/tax-rates", c); if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<CreateTaxRateResponse>(); return r?.TaxRate; } return null; } catch { return null; } }
    }
}
