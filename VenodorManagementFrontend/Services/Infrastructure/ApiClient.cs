using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace VenodorManagementFrontend.Services.Infrastructure
{
    public class ApiClient
    {
        public HttpClient Http { get; }
        private readonly AuthService _auth;

        public ApiClient(HttpClient http, AuthService auth)
        {
            Http = http;
            _auth = auth;
        }

        public void SetAuthHeader()
        {
            if (_auth.IsAuthenticated && !string.IsNullOrEmpty(_auth.Token))
                Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.Token);
            else
                Http.DefaultRequestHeaders.Authorization = null;
        }

        public async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, string fallback)
        {
            try
            {
                var errObj = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                if (errObj != null && errObj.TryGetValue("message", out var msg) && !string.IsNullOrWhiteSpace(msg))
                    return msg;
            }
            catch { }
            return fallback;
        }
    }
}
