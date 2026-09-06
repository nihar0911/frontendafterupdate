using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Models.Invoices.DTOs;
using VenodorManagementFrontend.Models.Payments.Commands;
using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Payments
{
    public class PayInvoiceService
    {
        private readonly ApiClient _api;
        public PayInvoiceService(ApiClient api) => _api = api;
        public async Task<ApiResult<InvoiceDto>> ExecuteAsync(MarkInvoicePaidCommand command)
        {
            _api.SetAuthHeader();
            try
            {
                var resp = await _api.Http.PostAsJsonAsync("api/payments/pay", command);
                if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<InvoiceDto>(); return new ApiResult<InvoiceDto> { Success = true, Data = r }; }
                return new ApiResult<InvoiceDto> { Success = false, ErrorMessage = await _api.ReadErrorMessageAsync(resp, "Unable to process payment.") };
            }
            catch (Exception ex) { return new ApiResult<InvoiceDto> { Success = false, ErrorMessage = ex.Message }; }
        }
    }
}
