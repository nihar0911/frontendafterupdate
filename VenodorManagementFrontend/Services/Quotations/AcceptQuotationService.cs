using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VendorManagement.Web.Models; using VendorManagement.Web.Services.Infrastructure;
namespace VendorManagement.Web.Services.Quotations
{
    public class AcceptQuotationService
    {
        private readonly ApiClient _api;
        public AcceptQuotationService(ApiClient api) => _api = api;
        public async Task<QuotationDto?> ExecuteAsync(int id) { _api.SetAuthHeader(); try { var resp = await _api.Http.PostAsync($"api/quotations/{id}/accept", null); if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<AcceptQuotationResponse>(); return r?.Quotation; } return null; } catch { return null; } }
    }
}
