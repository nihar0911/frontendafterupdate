using System; using System.Collections.Generic; using System.Net.Http.Json; using System.Threading.Tasks;
using VendorManagement.Web.Models;
using VendorManagement.Web.Models.Contracts.Commands;
using VendorManagement.Web.Models.Contracts.Responses;
using VendorManagement.Web.Services.Infrastructure;
namespace VendorManagement.Web.Services.Contracts
{
    public class ResetContractService
    {
        private readonly ApiClient _api;
        public ResetContractService(ApiClient api) => _api = api;
        public async Task<ApiResult<ContractDto>> ExecuteAsync(int contractId)
        {
            _api.SetAuthHeader();
            try
            {
                var cmd = new ResetContractCommand { ContractID = contractId };
                var resp = await _api.Http.PostAsJsonAsync("api/contract/reset", cmd);
                if (resp.IsSuccessStatusCode) { var r = await resp.Content.ReadFromJsonAsync<ResetContractResponse>(); return new ApiResult<ContractDto> { Success = true, Data = r?.Contract }; }
                return new ApiResult<ContractDto> { Success = false, ErrorMessage = await _api.ReadErrorMessageAsync(resp, "Unable to reset contract.") };
            }
            catch (Exception ex) { return new ApiResult<ContractDto> { Success = false, ErrorMessage = ex.Message }; }
        }
    }
}
