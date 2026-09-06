using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Users
{
    public class CreateUserService
    {
        private readonly ApiClient _api;
        public CreateUserService(ApiClient api) => _api = api;
        public async Task<ApiResult<UserDto>> ExecuteAsync(CreateUserCommand c) { _api.SetAuthHeader(); try { var resp = await _api.Http.PostAsJsonAsync("api/users", c); if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<CreateUserResponse>(); return new ApiResult<UserDto> { Success = true, Data = r?.User }; } return new ApiResult<UserDto> { Success = false, ErrorMessage = await _api.ReadErrorMessageAsync(resp, "Unable to create user.") }; } catch (Exception ex) { return new ApiResult<UserDto> { Success = false, ErrorMessage = ex.Message }; } }
    }
}
