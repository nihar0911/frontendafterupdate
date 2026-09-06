using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Contracts
{
    public class GetContractByIdService
    {
        private readonly ApiClient _api;
        public GetContractByIdService(ApiClient api) => _api = api;
        public async Task<ContractDto?> GetAsync(int id) { _api.SetAuthHeader(); try { var r = await _api.Http.GetFromJsonAsync<GetContractByIdResponse>($"api/contracts/{id}"); return r?.Contract; } catch { return null; } }
    }
}
