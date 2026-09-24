using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using VendorManagement.Web.Models;
using VendorManagement.Web.Services.Infrastructure;

namespace VendorManagement.Web.Services.Outlets
{
    public class GetOutletsByOrganizationService
    {
        private readonly ApiClient _api;
        public GetOutletsByOrganizationService(ApiClient api) => _api = api;
        public async Task<List<OutletDto>> GetAsync(int orgId)
        {
            _api.SetAuthHeader();
            try { var r = await _api.Http.GetFromJsonAsync<GetOutletsResponse>($"api/outlets/organization/{orgId}"); return r?.Outlets ?? new(); }
            catch { return new(); }
        }
    }
}
