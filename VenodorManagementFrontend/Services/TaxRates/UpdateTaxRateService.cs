using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.TaxRates
{
    public class UpdateTaxRateService
    {
        private readonly ApiClient _api;
        public UpdateTaxRateService(ApiClient api) => _api = api;
        public async Task<TaxRateDto?> ExecuteAsync(int id, UpdateTaxRateCommand c) { _api.SetAuthHeader(); try { var resp = await _api.Http.PutAsJsonAsync($"api/tax-rates/{id}", c); if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<UpdateTaxRateResponse>(); return r?.TaxRate; } return null; } catch { return null; } }
    }
}
