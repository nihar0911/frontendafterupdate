using System.Collections.Generic; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models.Payments.DTOs;
using VenodorManagementFrontend.Models.Payments.Responses;
using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Payments
{
    public class GetPaymentsService
    {
        private readonly ApiClient _api;
        public GetPaymentsService(ApiClient api) => _api = api;
        public async Task<List<PaymentDto>> GetAsync() { _api.SetAuthHeader(); try { var r = await _api.Http.GetFromJsonAsync<GetPaymentsResponse>("api/payments"); return r?.Payments ?? new(); } catch { return new(); } }
    }
}
