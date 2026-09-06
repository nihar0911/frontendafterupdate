using System.Collections.Generic; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models.Invoices.DTOs;
using VenodorManagementFrontend.Models.Invoices.Responses;
using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Invoices
{
    public class GetInvoicesService
    {
        private readonly ApiClient _api;
        public GetInvoicesService(ApiClient api) => _api = api;
        public async Task<List<InvoiceDto>> GetAsync() { _api.SetAuthHeader(); try { var r = await _api.Http.GetFromJsonAsync<GetInvoicesResponse>("api/invoices"); return r?.Invoices ?? new(); } catch { return new(); } }
        public async Task<InvoiceDto?> GetByIdAsync(int id) { _api.SetAuthHeader(); try { var r = await _api.Http.GetFromJsonAsync<GetInvoiceByIdResponse>($"api/invoices/{id}"); return r?.Invoice; } catch { return null; } }
        public async Task<byte[]?> DownloadPdfAsync(int invoiceId) { _api.SetAuthHeader(); try { var resp = await _api.Http.GetAsync($"api/invoices/{invoiceId}/pdf"); if (resp.IsSuccessStatusCode) return await resp.Content.ReadAsByteArrayAsync(); return null; } catch { return null; } }
    }
}
