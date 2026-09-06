using System; using System.Collections.Generic; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Contracts
{
    public class CreateContractService
    {
        private readonly ApiClient _api;
        public CreateContractService(ApiClient api) => _api = api;
        public async Task<ContractDto?> ExecuteAsync(CreateContractCommand c) { _api.SetAuthHeader(); try { var resp = await _api.Http.PostAsJsonAsync("api/contracts", c); if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<CreateContractResponse>(); return r?.Contract; } return null; } catch { return null; } }
        public async Task<ContractDto?> CreateFromQuotationAsync(int quotationId, List<CreateContractVendorAllocationDto>? allocations = null)
        {
            _api.SetAuthHeader();
            try
            {
                var cmd = new CreateContractFromQuotationCommand { QuotationID = quotationId, Allocations = allocations };
                var resp = await _api.Http.PostAsJsonAsync("api/contracts/from-quotation", cmd);
                if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<CreateContractFromQuotationResponse>(); return r?.Contract; }
                return null;
            }
            catch { return null; }
        }
    }
}
