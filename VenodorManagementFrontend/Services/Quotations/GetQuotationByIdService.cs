using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Quotations
{
    public class GetQuotationByIdService
    {
        private readonly ApiClient _api;
        public GetQuotationByIdService(ApiClient api) => _api = api;
        public async Task<QuotationDto?> GetAsync(int id) { _api.SetAuthHeader(); try { var r = await _api.Http.GetFromJsonAsync<GetQuotationByIdResponse>($"api/quotations/{id}"); return r?.Quotation; } catch { return null; } }
    }
}
