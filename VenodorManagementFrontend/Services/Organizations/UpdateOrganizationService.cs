using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services.Infrastructure;

namespace VenodorManagementFrontend.Services.Organizations
{
    public class UpdateOrganizationService
    {
        private readonly ApiClient _api;
        public UpdateOrganizationService(ApiClient api) => _api = api;
        public async Task<ApiResult<OrganizationDto>> ExecuteAsync(int id, UpdateOrganizationCommand command)
        {
            _api.SetAuthHeader();
            try
            {
                var response = await _api.Http.PutAsJsonAsync($"api/organizations/{id}", command);
                if (response.IsSuccessStatusCode) { var r = await response.Content.ReadFromJsonAsync<UpdateOrganizationResponse>(); return new ApiResult<OrganizationDto> { Success = true, Data = r?.Organization }; }
                return new ApiResult<OrganizationDto> { Success = false, ErrorMessage = await _api.ReadErrorMessageAsync(response, "Unable to update organization.") };
            }
            catch (Exception ex) { return new ApiResult<OrganizationDto> { Success = false, ErrorMessage = ex.Message }; }
        }
    }
}
