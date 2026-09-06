using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Quotations
{
    public class CreateQuotationService
    {
        private readonly ApiClient _api;
        public CreateQuotationService(ApiClient api) => _api = api;
        public async Task<QuotationDto?> ExecuteAsync(CreateQuotationCommand c) { _api.SetAuthHeader(); try { var resp = await _api.Http.PostAsJsonAsync("api/quotations", c); if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<CreateQuotationResponse>(); return r?.Quotation; } return null; } catch { return null; } }
    }
}
