using System.Net.Http.Json;
using System.Threading.Tasks;
using VenodorManagementFrontend.Models;

namespace VenodorManagementFrontend.Services.Authentication
{
    public class LoginService
    {
        private readonly Services.Infrastructure.ApiClient _api;
        public LoginService(Services.Infrastructure.ApiClient api) => _api = api;

        public async Task<LoginResponseWrapper?> ExecuteAsync(string email, string password)
        {
            var request = new LoginRequest { Email = email, Password = password };
            var response = await _api.Http.PostAsJsonAsync("api/auth/login", request);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<LoginResponseWrapper>();
            return null;
        }
    }
}
