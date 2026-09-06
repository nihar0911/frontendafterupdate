using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Models.Invoices.DTOs;
using VenodorManagementFrontend.Models.Invoices.Requests;
using VenodorManagementFrontend.Models.Invoices.Responses;
using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Invoices
{
    public class CreateInvoiceService
    {
        private readonly ApiClient _api;
        public CreateInvoiceService(ApiClient api) => _api = api;
        public async Task<ApiResult<InvoiceDto>> ExecuteAsync(CreateInvoiceRequest request)
        {
            _api.SetAuthHeader();
            try
            {
                var resp = await _api.Http.PostAsJsonAsync("api/invoices", request);
                if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<InvoiceDto>(); return new ApiResult<InvoiceDto> { Success = true, Data = r }; }
                return new ApiResult<InvoiceDto> { Success = false, ErrorMessage = await _api.ReadErrorMessageAsync(resp, "Unable to create invoice.") };
            }
            catch (Exception ex) { return new ApiResult<InvoiceDto> { Success = false, ErrorMessage = ex.Message }; }
        }
    }
}
