using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using VendorManagement.Web.Models;
using VendorManagement.Web.Services.Infrastructure;

namespace VendorManagement.Web.Services.Outlets
{
    public class CreateOutletService
    {
        private readonly ApiClient _api;
        public CreateOutletService(ApiClient api) => _api = api;
        public async Task<ApiResult<OutletDto>> ExecuteAsync(CreateOutletCommand command)
        {
            _api.SetAuthHeader();
            try
            {
                var response = await _api.Http.PostAsJsonAsync("api/outlets", command);
                if (response.IsSuccessStatusCode) { var r = await response.Content.ReadFromJsonAsync<CreateOutletResponse>(); return new ApiResult<OutletDto> { Success = true, Data = r?.Outlet }; }
                return new ApiResult<OutletDto> { Success = false, ErrorMessage = await _api.ReadErrorMessageAsync(response, "Unable to create outlet.") };
            }
            catch (Exception ex) { return new ApiResult<OutletDto> { Success = false, ErrorMessage = ex.Message }; }
        }
    }
}
