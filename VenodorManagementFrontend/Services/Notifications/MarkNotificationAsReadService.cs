using System; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.Notifications
{
    public class MarkNotificationAsReadService
    {
        private readonly ApiClient _api;
        public MarkNotificationAsReadService(ApiClient api) => _api = api;
        public async Task<bool> ExecuteAsync(int notificationId) { _api.SetAuthHeader(); try { var resp = await _api.Http.PutAsync($"api/notifications/{notificationId}/read", null); return resp.IsSuccessStatusCode; } catch { return false; } }
    }
}
