using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using VendorManagement.Web.Models;
using VendorManagement.Web.Services.Infrastructure;

namespace VendorManagement.Web.Services.Notifications
{
    public class GetNotificationsService
    {
        private readonly ApiClient _api;
        public GetNotificationsService(ApiClient api) => _api = api;
        public async Task<List<NotificationDto>> GetMyNotificationsAsync()
        {
            _api.SetAuthHeader();
            try
            {
                var r = await _api.Http.GetFromJsonAsync<GetNotificationsResponse>("api/notifications");
                return r?.Notifications ?? new();
            }
            catch
            {
                return new();
            }
        }
    }
}
