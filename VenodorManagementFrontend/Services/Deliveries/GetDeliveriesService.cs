using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using VendorManagement.Web.Models;
using VendorManagement.Web.Models.Deliveries.DTOs;
using VendorManagement.Web.Models.Deliveries.Responses;
using VendorManagement.Web.Services.Infrastructure;

namespace VendorManagement.Web.Services.Deliveries
{
    public class GetDeliveriesService
    {
        private readonly ApiClient _api;
        public GetDeliveriesService(ApiClient api) => _api = api;
        public async Task<List<DeliveryRecordDto>> GetByPurchaseOrderAsync(int purchaseOrderId) { _api.SetAuthHeader(); try { var r = await _api.Http.GetFromJsonAsync<GetDeliveryRecordsResponse>($"api/delivery-records/po/{purchaseOrderId}"); return r?.Deliveries ?? new(); } catch { return new(); } }
        public async Task<GetMyDeliveriesResponse?> GetMyDeliveriesAsync() { _api.SetAuthHeader(); try { var r = await _api.Http.GetFromJsonAsync<GetMyDeliveriesResponse>("api/deliveryrecords/vendor/my"); return r; } catch { return null; } }
    }
}
