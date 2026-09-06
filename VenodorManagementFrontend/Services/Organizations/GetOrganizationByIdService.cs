using System.Net.Http.Json;
using System.Threading.Tasks;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services.Infrastructure;

namespace VenodorManagementFrontend.Services.Organizations
{
    public class GetOrganizationByIdService
    {
        private readonly ApiClient _api;
        public GetOrganizationByIdService(ApiClient api) => _api = api;
        public async Task<OrganizationDto?> GetAsync(int id)
        {
            _api.SetAuthHeader();
            try { var r = await _api.Http.GetFromJsonAsync<GetOrganizationByIdResponse>($"api/organizations/{id}"); return r?.Organization; }
            catch { return null; }
        }
    }
}
