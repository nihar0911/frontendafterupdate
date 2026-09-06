using System.Collections.Generic; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Users
{
    public class GetUsersService
    {
        private readonly ApiClient _api;
        public GetUsersService(ApiClient api) => _api = api;
        public async Task<List<UserDto>> GetAsync() { _api.SetAuthHeader(); try { var r = await _api.Http.GetFromJsonAsync<GetUsersResponse>("api/users"); return r?.Users ?? new(); } catch { return new(); } }
    }
}
