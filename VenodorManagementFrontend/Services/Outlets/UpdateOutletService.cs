using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services.Infrastructure;

namespace VenodorManagementFrontend.Services.Outlets
{
    public class UpdateOutletService
    {
        private readonly ApiClient _api;
        public UpdateOutletService(ApiClient api) => _api = api;
        public async Task<ApiResult<OutletDto>> ExecuteAsync(int id, UpdateOutletCommand command)
        {
            _api.SetAuthHeader();
            try
            {
                var response = await _api.Http.PutAsJsonAsync($"api/outlets/{id}", command);
                if (response.IsSuccessStatusCode) { var r = await response.Content.ReadFromJsonAsync<UpdateOutletResponse>(); return new ApiResult<OutletDto> { Success = true, Data = r?.Outlet }; }
                return new ApiResult<OutletDto> { Success = false, ErrorMessage = await _api.ReadErrorMessageAsync(response, "Unable to update outlet.") };
            }
            catch (Exception ex) { return new ApiResult<OutletDto> { Success = false, ErrorMessage = ex.Message }; }
        }
    }
}
