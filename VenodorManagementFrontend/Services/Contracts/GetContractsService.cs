using System.Collections.Generic; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Contracts
{
    public class GetContractsService
    {
        private readonly ApiClient _api;
        public GetContractsService(ApiClient api) => _api = api;
        public async Task<List<ContractDto>> GetAsync() { _api.SetAuthHeader(); try { var r = await _api.Http.GetFromJsonAsync<GetContractsResponse>("api/contracts"); return r?.Contracts ?? new(); } catch { return new(); } }
        public async Task<List<ContractDto>> GetByOutletAsync(int outletId) { _api.SetAuthHeader(); try { var r = await _api.Http.GetFromJsonAsync<GetContractsResponse>($"api/contracts/outlet/{outletId}"); return r?.Contracts ?? new(); } catch { return new(); } }
        public async Task<List<ContractDto>> GetByOrganizationAsync(int organizationId) { _api.SetAuthHeader(); try { var r = await _api.Http.GetFromJsonAsync<GetContractsResponse>($"api/contracts/organization/{organizationId}"); return r?.Contracts ?? new(); } catch { return new(); } }
    }
}
