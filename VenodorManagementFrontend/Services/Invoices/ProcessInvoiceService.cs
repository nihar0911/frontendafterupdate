using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VendorManagement.Web.Models;
using VendorManagement.Web.Models.Invoices.DTOs;
using VendorManagement.Web.Models.Invoices.Requests;
using VendorManagement.Web.Services.Infrastructure;
namespace VendorManagement.Web.Services.Invoices
{
    public class ProcessInvoiceService
    {
        private readonly ApiClient _api;
        public ProcessInvoiceService(ApiClient api) => _api = api;
        public async Task<ApiResult<InvoiceDto>> ApproveAsync(int invoiceId)
        {
            _api.SetAuthHeader();
            try
            {
                var resp = await _api.Http.PostAsync($"api/invoices/{invoiceId}/approve", null);
                if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<InvoiceDto>(); return new ApiResult<InvoiceDto> { Success = true, Data = r }; }
                return new ApiResult<InvoiceDto> { Success = false, ErrorMessage = await _api.ReadErrorMessageAsync(resp, "Unable to approve invoice.") };
            }
            catch (Exception ex) { return new ApiResult<InvoiceDto> { Success = false, ErrorMessage = ex.Message }; }
        }
        public async Task<ApiResult<InvoiceDto>> RejectAsync(int invoiceId, string reason)
        {
            _api.SetAuthHeader();
            try
            {
                var req = new RejectInvoiceRequest { Reason = reason };
                var resp = await _api.Http.PostAsJsonAsync($"api/invoices/{invoiceId}/reject", req);
                if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<InvoiceDto>(); return new ApiResult<InvoiceDto> { Success = true, Data = r }; }
                return new ApiResult<InvoiceDto> { Success = false, ErrorMessage = await _api.ReadErrorMessageAsync(resp, "Unable to reject invoice.") };
            }
            catch (Exception ex) { return new ApiResult<InvoiceDto> { Success = false, ErrorMessage = ex.Message }; }
        }
    }
}
