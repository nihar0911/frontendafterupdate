using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Users
{
    public class UpdateUserService
    {
        private readonly ApiClient _api;
        public UpdateUserService(ApiClient api) => _api = api;
        public async Task<ApiResult<UserDto>> ExecuteAsync(int id, UpdateUserCommand c) { _api.SetAuthHeader(); try { var resp = await _api.Http.PutAsJsonAsync($"api/users/{id}", c); if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<UpdateUserResponse>(); return new ApiResult<UserDto> { Success = true, Data = r?.User }; } return new ApiResult<UserDto> { Success = false, ErrorMessage = await _api.ReadErrorMessageAsync(resp, "Unable to update user.") }; } catch (Exception ex) { return new ApiResult<UserDto> { Success = false, ErrorMessage = ex.Message }; } }
    }
}
