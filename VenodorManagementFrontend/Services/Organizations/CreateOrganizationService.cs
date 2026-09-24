using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using VendorManagement.Web.Models;
using VendorManagement.Web.Services.Infrastructure;

namespace VendorManagement.Web.Services.Organizations
{
    public class CreateOrganizationService
    {
        private readonly ApiClient _api;
        public CreateOrganizationService(ApiClient api) => _api = api;
        public async Task<ApiResult<OrganizationDto>> ExecuteAsync(CreateOrganizationCommand command)
        {
            _api.SetAuthHeader();
            try
            {
                var response = await _api.Http.PostAsJsonAsync("api/organizations", command);
                if (response.IsSuccessStatusCode) { var r = await response.Content.ReadFromJsonAsync<CreateOrganizationResponse>(); return new ApiResult<OrganizationDto> { Success = true, Data = r?.Organization }; }
                return new ApiResult<OrganizationDto> { Success = false, ErrorMessage = await _api.ReadErrorMessageAsync(response, "Unable to create organization.") };
            }
            catch (Exception ex) { return new ApiResult<OrganizationDto> { Success = false, ErrorMessage = ex.Message }; }
        }
    }
}
