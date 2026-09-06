using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Quotations
{
    public class RejectQuotationService
    {
        private readonly ApiClient _api;
        public RejectQuotationService(ApiClient api) => _api = api;
        public async Task<QuotationDto?> ExecuteAsync(int id) { _api.SetAuthHeader(); try { var resp = await _api.Http.PostAsync($"api/quotations/{id}/reject", null); if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<RejectQuotationResponse>(); return r?.Quotation; } return null; } catch { return null; } }
    }
}
