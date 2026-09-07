using System; using System.Collections.Generic; using System.Net.Http.Json; using System.Threading.Tasks;
using VenodorManagementFrontend.Models; using VenodorManagementFrontend.Services.Infrastructure;
namespace VenodorManagementFrontend.Services.PurchaseRequests
{
    public class DispatchPurchaseRequestService
    {
        private readonly ApiClient _api;
        public DispatchPurchaseRequestService(ApiClient api) => _api = api;
        public async Task<DispatchPurchaseRequestResponse?> ExecuteAsync(int requestId, List<int> selectedVendorIds) { _api.SetAuthHeader(); try { var cmd = new DispatchPurchaseRequestCommand { RequestID = requestId, SelectedVendorIDs = selectedVendorIds }; var resp = await _api.Http.PostAsJsonAsync($"api/purchase-requests/{requestId}/dispatch", cmd); if (resp.IsSuccessStatusCode) return await resp.Content.ReadFromJsonAsync<DispatchPurchaseRequestResponse>(); return null; } catch { return null; } }
        public async Task<DispatchPurchaseRequestResponse?> ExecuteAsync(int requestId, List<ItemVendorAssignmentDto> itemVendorAssignments) { _api.SetAuthHeader(); try { var cmd = new DispatchPurchaseRequestCommand { RequestID = requestId, ItemVendorAssignments = itemVendorAssignments, SelectedVendorIDs = itemVendorAssignments.Select(a => a.VendorID).Distinct().ToList() }; var resp = await _api.Http.PostAsJsonAsync($"api/purchase-requests/{requestId}/dispatch", cmd); if (resp.IsSuccessStatusCode) return await resp.Content.ReadFromJsonAsync<DispatchPurchaseRequestResponse>(); return null; } catch { return null; } }
    }
}
