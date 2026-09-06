using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services.Infrastructure;

namespace VenodorManagementFrontend.Services.Organizations
{
    public class GetOrganizationsService
    {
        private readonly ApiClient _api;
        public GetOrganizationsService(ApiClient api) => _api = api;
        public async Task<List<OrganizationDto>> GetAsync()
        {
            _api.SetAuthHeader();
            try { var r = await _api.Http.GetFromJsonAsync<GetOrganizationsResponse>("api/organizations"); return r?.Organizations ?? new(); }
            catch { return new(); }
        }
    }
}
