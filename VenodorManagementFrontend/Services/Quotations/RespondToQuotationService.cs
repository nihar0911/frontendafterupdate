using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Quotations
{
    public class RespondToQuotationService
    {
        private readonly ApiClient _api;
        public RespondToQuotationService(ApiClient api) => _api = api;
        public async Task<QuotationDto?> ExecuteAsync(RespondToQuotationCommand c) { _api.SetAuthHeader(); try { var resp = await _api.Http.PostAsJsonAsync("api/quotations/respond", c); if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<RespondToQuotationResponse>(); return r?.Quotation; } return null; } catch { return null; } }
    }
}
