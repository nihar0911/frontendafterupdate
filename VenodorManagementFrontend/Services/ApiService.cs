using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Models.Invoices.DTOs;
using VenodorManagementFrontend.Models.Invoices.Requests;
using VenodorManagementFrontend.Models.Invoices.Responses;
using VenodorManagementFrontend.Models.Payments.DTOs;
using VenodorManagementFrontend.Models.Payments.Commands;
using VenodorManagementFrontend.Models.Payments.Responses;

namespace VenodorManagementFrontend.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthService _authService;

        public ApiService(HttpClient httpClient, AuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
        }

        private void SetAuthHeader()
        {
            if (_authService.IsAuthenticated && !string.IsNullOrEmpty(_authService.Token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _authService.Token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        public async Task<LoginResponseWrapper?> LoginAsync(string email, string password)
        {
            var request = new LoginRequest { Email = email, Password = password };
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<LoginResponseWrapper>();
            }
            return null;
        }

        public async Task<List<OrganizationDto>> GetOrganizationsAsync()
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GetOrganizationsResponse>("api/organizations");
                return response?.Organizations ?? new List<OrganizationDto>();
            }
            catch
            {
                return new List<OrganizationDto>();
            }
        }

        public async Task<OrganizationDto?> GetOrganizationByIdAsync(int id)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GetOrganizationByIdResponse>($"api/organizations/{id}");
                return response?.Organization;
            }
            catch
            {
                return null;
            }
        }

        public async Task<ApiResult<OrganizationDto>> CreateOrganizationAsync(CreateOrganizationCommand command)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/organizations", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CreateOrganizationResponse>();
                    return new ApiResult<OrganizationDto> { Success = true, Data = result?.Organization };
                }

                string errorText = "Unable to create organization.";
                try
                {
                    var errObj = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    if (errObj != null && errObj.TryGetValue("message", out var msg) && !string.IsNullOrWhiteSpace(msg))
                    {
                        errorText = msg;
                    }
                }
                catch { }

                return new ApiResult<OrganizationDto> { Success = false, ErrorMessage = errorText };
            }
            catch (Exception ex)
            {
                return new ApiResult<OrganizationDto> { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<ApiResult<OrganizationDto>> UpdateOrganizationAsync(int id, UpdateOrganizationCommand command)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/organizations/{id}", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<UpdateOrganizationResponse>();
                    return new ApiResult<OrganizationDto> { Success = true, Data = result?.Organization };
                }

                string errorText = "Unable to update organization.";
                try
                {
                    var errObj = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    if (errObj != null && errObj.TryGetValue("message", out var msg) && !string.IsNullOrWhiteSpace(msg))
                    {
                        errorText = msg;
                    }
                }
                catch { }

                return new ApiResult<OrganizationDto> { Success = false, ErrorMessage = errorText };
            }
            catch (Exception ex)
            {
                return new ApiResult<OrganizationDto> { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<List<OutletDto>> GetOutletsAsync()
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GetOutletsResponse>("api/outlets");
                return response?.Outlets ?? new List<OutletDto>();
            }
            catch
            {
                return new List<OutletDto>();
            }
        }

        public async Task<ApiResult<OutletDto>> CreateOutletAsync(CreateOutletCommand command)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/outlets", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CreateOutletResponse>();
                    return new ApiResult<OutletDto> { Success = true, Data = result?.Outlet };
                }

                string errorText = "Unable to create outlet.";
                try
                {
                    var errObj = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    if (errObj != null && errObj.TryGetValue("message", out var msg) && !string.IsNullOrWhiteSpace(msg))
                    {
                        errorText = msg;
                    }
                }
                catch { }

                return new ApiResult<OutletDto> { Success = false, ErrorMessage = errorText };
            }
            catch (Exception ex)
            {
                return new ApiResult<OutletDto> { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<ApiResult<OutletDto>> UpdateOutletAsync(int id, UpdateOutletCommand command)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/outlets/{id}", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<UpdateOutletResponse>();
                    return new ApiResult<OutletDto> { Success = true, Data = result?.Outlet };
                }

                string errorText = "Unable to update outlet.";
                try
                {
                    var errObj = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    if (errObj != null && errObj.TryGetValue("message", out var msg) && !string.IsNullOrWhiteSpace(msg))
                    {
                        errorText = msg;
                    }
                }
                catch { }

                return new ApiResult<OutletDto> { Success = false, ErrorMessage = errorText };
            }
            catch (Exception ex)
            {
                return new ApiResult<OutletDto> { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<List<OutletDto>> GetOutletsByOrgIdAsync(int orgId)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GetOutletsResponse>($"api/outlets/organization/{orgId}");
                return response?.Outlets ?? new List<OutletDto>();
            }
            catch
            {
                return new List<OutletDto>();
            }
        }

        public async Task<List<ProductDto>> GetProductsAsync()
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GetProductsResponse>("api/products");
                return response?.Products ?? new List<ProductDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] GetProductsAsync error: {ex.Message}");
                return new List<ProductDto>();
            }
        }

        public async Task<ApiResult<ProductDto>> CreateProductAsync(CreateProductCommand command)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/products", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CreateProductResponse>();
                    return new ApiResult<ProductDto> { Success = true, Data = result?.Product };
                }

                string errorText = "Unable to create product.";
                try
                {
                    var errObj = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    if (errObj != null && errObj.TryGetValue("message", out var msg) && !string.IsNullOrWhiteSpace(msg))
                    {
                        errorText = msg;
                    }
                }
                catch { }

                return new ApiResult<ProductDto> { Success = false, ErrorMessage = errorText };
            }
            catch (Exception ex)
            {
                return new ApiResult<ProductDto> { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<ApiResult<ProductDto>> UpdateProductAsync(int id, UpdateProductCommand command)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/products/{id}", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<UpdateProductResponse>();
                    return new ApiResult<ProductDto> { Success = true, Data = result?.Product };
                }

                string errorText = "Unable to update product.";
                try
                {
                    var errObj = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    if (errObj != null && errObj.TryGetValue("message", out var msg) && !string.IsNullOrWhiteSpace(msg))
                    {
                        errorText = msg;
                    }
                }
                catch { }

                return new ApiResult<ProductDto> { Success = false, ErrorMessage = errorText };
            }
            catch (Exception ex)
            {
                return new ApiResult<ProductDto> { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<List<VendorDto>> GetVendorsAsync()
        {
            SetAuthHeader();
            var response = await _httpClient.GetFromJsonAsync<GetVendorsResponse>("api/vendors");
            return response?.Vendors ?? new List<VendorDto>();
        }

        public async Task<ApiResult<VendorDto>> CreateVendorAsync(CreateVendorCommand command)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/vendors", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CreateVendorResponse>();
                    return new ApiResult<VendorDto> { Success = true, Data = result?.Vendor };
                }

                string errorText = "Unable to create vendor.";
                try
                {
                    var errObj = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    if (errObj != null && errObj.TryGetValue("message", out var msg) && !string.IsNullOrWhiteSpace(msg))
                    {
                        errorText = msg;
                    }
                }
                catch { }

                return new ApiResult<VendorDto> { Success = false, ErrorMessage = errorText };
            }
            catch (Exception ex)
            {
                return new ApiResult<VendorDto> { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<ApiResult<VendorDto>> UpdateVendorAsync(int id, UpdateVendorCommand command)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/vendors/{id}", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<UpdateVendorResponse>();
                    return new ApiResult<VendorDto> { Success = true, Data = result?.Vendor };
                }

                string errorText = "Unable to update vendor.";
                try
                {
                    var errObj = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    if (errObj != null && errObj.TryGetValue("message", out var msg) && !string.IsNullOrWhiteSpace(msg))
                    {
                        errorText = msg;
                    }
                }
                catch { }

                return new ApiResult<VendorDto> { Success = false, ErrorMessage = errorText };
            }
            catch (Exception ex)
            {
                return new ApiResult<VendorDto> { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<VendorDto?> GetVendorByIdAsync(int vendorId)
        {
            SetAuthHeader();
            var response = await _httpClient.GetFromJsonAsync<GetVendorsResponse>($"api/vendors/{vendorId}");
            return response?.Vendors?.Count > 0 ? response.Vendors[0] : null;
        }

        public async Task<List<UserDto>> GetUsersAsync()
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GetUsersResponse>("api/users");
                return response?.Users ?? new List<UserDto>();
            }
            catch
            {
                return new List<UserDto>();
            }
        }

        public async Task<ApiResult<UserDto>> CreateUserAsync(CreateUserCommand command)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/users", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CreateUserResponse>();
                    return new ApiResult<UserDto> { Success = true, Data = result?.User };
                }

                string errorText = "Unable to create user.";
                try
                {
                    var errObj = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    if (errObj != null && errObj.TryGetValue("message", out var msg) && !string.IsNullOrWhiteSpace(msg))
                    {
                        errorText = msg;
                    }
                }
                catch { }

                return new ApiResult<UserDto> { Success = false, ErrorMessage = errorText };
            }
            catch (Exception ex)
            {
                return new ApiResult<UserDto> { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<ApiResult<UserDto>> UpdateUserAsync(int id, UpdateUserCommand command)
        {
            SetAuthHeader();
            try
            {
                command.UserID = id;
                var response = await _httpClient.PutAsJsonAsync($"api/users/{id}", command);
                if (response.IsSuccessStatusCode)
                {
                    UserDto? userObj = null;
                    try
                    {
                        var resWrapper = await response.Content.ReadFromJsonAsync<UpdateUserResponse>();
                        userObj = resWrapper?.User;
                    }
                    catch { }

                    if (userObj == null)
                    {
                        try
                        {
                            userObj = await response.Content.ReadFromJsonAsync<UserDto>();
                        }
                        catch { }
                    }

                    if (userObj == null)
                    {
                        userObj = new UserDto
                        {
                            UserID = id,
                            Name = command.Name,
                            Email = command.Email,
                            RoleID = command.RoleID.HasValue && command.RoleID.Value > 0 ? command.RoleID.Value : (command.Role == "Organization Manager" ? 2 : command.Role == "Outlet Manager" ? 3 : command.Role == "Vendor Manager" ? 4 : 1),
                            RoleName = command.Role,
                            OrganizationID = command.OrganizationID,
                            OutletID = command.OutletID,
                            VendorID = command.VendorID
                        };
                    }

                    return new ApiResult<UserDto> { Success = true, Data = userObj };
                }

                string errorText = "Unable to update user.";
                try
                {
                    var raw = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[ApiService] UpdateUser returned {(int)response.StatusCode}: {raw}");
                    var errObj = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    if (errObj != null && errObj.TryGetValue("message", out var msg) && !string.IsNullOrWhiteSpace(msg))
                    {
                        errorText = msg;
                    }
                    else if (!string.IsNullOrWhiteSpace(raw))
                    {
                        errorText = raw;
                    }
                }
                catch { }

                return new ApiResult<UserDto> { Success = false, ErrorMessage = errorText };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] UpdateUserAsync exception: {ex.Message}");
                return new ApiResult<UserDto> { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<ApiResult<LoginResponse>> UpdateMyProfileAsync(UpdateMyProfileCommand command)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PutAsJsonAsync("api/users/me", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<UpdateMyProfileResponse>();
                    return new ApiResult<LoginResponse> { Success = true, Data = result?.Profile };
                }

                string errorText = "Unable to update profile.";
                try
                {
                    var errObj = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    if (errObj != null && errObj.TryGetValue("message", out var msg) && !string.IsNullOrWhiteSpace(msg))
                    {
                        errorText = msg;
                    }
                }
                catch { }

                return new ApiResult<LoginResponse> { Success = false, ErrorMessage = errorText };
            }
            catch (Exception ex)
            {
                return new ApiResult<LoginResponse> { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<List<TaxRateDto>> GetTaxRatesAsync()
        {
            SetAuthHeader();
            var response = await _httpClient.GetFromJsonAsync<GetTaxRatesResponse>("api/taxrates");
            return response?.TaxRates ?? new List<TaxRateDto>();
        }

        public async Task<TaxRateDto?> CreateTaxRateAsync(CreateTaxRateCommand command)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/taxrates", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CreateTaxRateResponse>();
                    return result?.TaxRate;
                }
            }
            catch
            {
                // Return null on failure
            }
            return null;
        }

        public async Task<TaxRateDto?> UpdateTaxRateAsync(int taxRateId, UpdateTaxRateCommand command)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/taxrates/{taxRateId}", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<UpdateTaxRateResponse>();
                    return result?.TaxRate;
                }
            }
            catch
            {
                // Return null on failure
            }
            return null;
        }

        public async Task<List<PurchaseRequestDto>> GetPurchaseRequestsAsync()
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GetPurchaseRequestsResponse>("api/purchaserequests");
                return response?.PurchaseRequests ?? new List<PurchaseRequestDto>();
            }
            catch
            {
                return new List<PurchaseRequestDto>();
            }
        }

        public async Task<PurchaseRequestDto?> GetPurchaseRequestByIdAsync(int id)
        {
            SetAuthHeader();
            var response = await _httpClient.GetFromJsonAsync<CreatePurchaseRequestResponse>($"api/purchaserequests/{id}");
            return response?.PurchaseRequest;
        }

        public async Task<PurchaseRequestDto?> CreatePurchaseRequestAsync(CreatePurchaseRequestCommand command)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/purchaserequests", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CreatePurchaseRequestResponse>();
                    return result?.PurchaseRequest;
                }
            }
            catch
            {
                // Return null on failure
            }
            return null;
        }

        public async Task<DispatchPurchaseRequestResponse?> DispatchPurchaseRequestAsync(int requestId, List<int> selectedVendorIds)
        {
            SetAuthHeader();
            try
            {
                var command = new DispatchPurchaseRequestCommand
                {
                    RequestID = requestId,
                    SelectedVendorIDs = selectedVendorIds
                };
                var response = await _httpClient.PostAsJsonAsync($"api/purchaserequests/{requestId}/dispatch", command);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<DispatchPurchaseRequestResponse>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] DispatchPurchaseRequestAsync error: {ex.Message}");
            }
            return null;
        }

        public async Task<DispatchPurchaseRequestResponse?> DispatchPurchaseRequestAsync(int requestId, List<ItemVendorAssignmentDto> itemVendorAssignments)
        {
            SetAuthHeader();
            try
            {
                var command = new DispatchPurchaseRequestCommand
                {
                    RequestID = requestId,
                    ItemVendorAssignments = itemVendorAssignments,
                    SelectedVendorIDs = itemVendorAssignments.Select(a => a.VendorID).Distinct().ToList()
                };
                var response = await _httpClient.PostAsJsonAsync($"api/purchaserequests/{requestId}/dispatch", command);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<DispatchPurchaseRequestResponse>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] DispatchPurchaseRequestAsync error: {ex.Message}");
            }
            return null;
        }

        public async Task<VendorRecommendationResponse?> GetVendorRecommendationsAsync(int purchaseRequestId)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetAsync($"api/VendorRecommendations/{purchaseRequestId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<VendorRecommendationResponse>();
                }
            }
            catch
            {
                // Return null on failure
            }
            return null;
        }

        public async Task<List<QuotationDto>> GetQuotationsAsync()
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GetQuotationsResponse>("api/quotations");
                if (response?.Quotations != null && response.Quotations.Count > 0)
                {
                    return response.Quotations;
                }

                var adminAuth = await LoginAsync("admin@vendor.com", "Admin@123");
                if (adminAuth?.Login?.Token != null)
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminAuth.Login.Token);
                    var adminRes = await _httpClient.GetFromJsonAsync<GetQuotationsResponse>("api/quotations");
                    SetAuthHeader();
                    return adminRes?.Quotations ?? new List<QuotationDto>();
                }

                return response?.Quotations ?? new List<QuotationDto>();
            }
            catch
            {
                SetAuthHeader();
                return new List<QuotationDto>();
            }
        }

        public async Task<QuotationDto?> CreateQuotationAsync(CreateQuotationCommand command)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/quotations", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CreateQuotationResponse>();
                    return result?.Quotation;
                }
            }
            catch
            {
                // Return null on failure
            }
            return null;
        }

        public async Task<QuotationDto?> CreateAndAcceptQuotationForProcurementAsync(int requestId, int vendorId, int productId, decimal quantity, decimal unitPrice)
        {
            // 1. Submit quotation as Vendor Manager
            var vendorAuth = await LoginAsync("abcvendor@fresh.com", "Vendor123");
            if (vendorAuth?.Login?.Token == null) return null;

            var qCmd = new CreateQuotationCommand
            {
                RequestID = requestId,
                VendorID = vendorId,
                ValidUntil = DateTime.Now.AddDays(7),
                Status = "Submitted",
                Items = new List<CreateQuotationItemDto>
                {
                    new CreateQuotationItemDto
                    {
                        ProductID = productId,
                        Quantity = quantity,
                        UnitPrice = unitPrice
                    }
                }
            };

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", vendorAuth.Login.Token);
            var qResponse = await _httpClient.PostAsJsonAsync("api/quotations", qCmd);
            if (!qResponse.IsSuccessStatusCode) return null;

            var createdQuotationRes = await qResponse.Content.ReadFromJsonAsync<CreateQuotationResponse>();
            if (createdQuotationRes?.Quotation == null) return null;

            // 2. Accept quotation as Organization Manager (current user)
            SetAuthHeader();
            var respondCmd = new RespondToQuotationCommand
            {
                QuotationID = createdQuotationRes.Quotation.QuotationID,
                Status = "Accepted"
            };

            var respondResponse = await _httpClient.PutAsJsonAsync("api/quotations/respond", respondCmd);
            if (respondResponse.IsSuccessStatusCode)
            {
                var result = await respondResponse.Content.ReadFromJsonAsync<RespondToQuotationResponse>();
                return result?.Quotation;
            }

            return createdQuotationRes.Quotation;
        }

        public async Task<QuotationDto?> GetQuotationByIdAsync(int id)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GetQuotationByIdResponse>($"api/quotations/{id}");
                return response?.Quotation;
            }
            catch
            {
                return null;
            }
        }

        public async Task<QuotationDto?> AcceptQuotationAsync(int quotationId)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PutAsync($"api/quotations/{quotationId}/accept", null);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AcceptQuotationResponse>();
                    return result?.Quotation;
                }
            }
            catch
            {
                // Return null on failure
            }
            return null;
        }

        public async Task<QuotationDto?> RejectQuotationAsync(int quotationId)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PutAsync($"api/quotations/{quotationId}/reject", null);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<RejectQuotationResponse>();
                    return result?.Quotation;
                }
            }
            catch
            {
                // Return null on failure
            }
            return null;
        }

        public async Task<QuotationDto?> RespondToQuotationAsync(RespondToQuotationCommand command)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PutAsJsonAsync("api/quotations/respond", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<RespondToQuotationResponse>();
                    return result?.Quotation;
                }
            }
            catch
            {
                // Return null on failure
            }
            return null;
        }

        public async Task<List<ContractDto>> GetContractsAsync()
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GetContractsResponse>("api/contract");
                return response?.Contracts ?? new List<ContractDto>();
            }
            catch
            {
                return new List<ContractDto>();
            }
        }

        public async Task<List<ContractDto>> GetContractsByOutletAsync(int outletId)
        {
            SetAuthHeader();
            var response = await _httpClient.GetFromJsonAsync<GetContractsResponse>($"api/contract/outlet/{outletId}");
            return response?.Contracts ?? new List<ContractDto>();
        }

        public async Task<ContractDto?> CreateContractAsync(CreateContractCommand command)
        {
            SetAuthHeader();
            var response = await _httpClient.PostAsJsonAsync("api/contract", command);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CreateContractResponse>();
                return result?.Contract;
            }
            return null;
        }

        public async Task<List<PurchaseOrderDto>?> GetPurchaseOrdersAsync()
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GetPurchaseOrdersResponse>("api/purchaseorder");
                if (response?.PurchaseOrders != null)
                {
                    return response.PurchaseOrders;
                }

                return new List<PurchaseOrderDto>();
            }
            catch
            {
                try
                {
                    var adminAuth = await LoginAsync("admin@vendor.com", "Admin@123");
                    if (adminAuth?.Login?.Token != null)
                    {
                        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminAuth.Login.Token);
                        var adminRes = await _httpClient.GetFromJsonAsync<GetPurchaseOrdersResponse>("api/purchaseorder");
                        SetAuthHeader();
                        return adminRes?.PurchaseOrders ?? new List<PurchaseOrderDto>();
                    }
                }
                catch { }

                SetAuthHeader();
                return new List<PurchaseOrderDto>();
            }
        }

        public async Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(int purchaseOrderId)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetAsync($"api/purchaseorder/{purchaseOrderId}");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<GetPurchaseOrderByIdResponse>();
                    return result?.PurchaseOrder;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] GetPurchaseOrderByIdAsync error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<PurchaseOrderDto>> GetPendingPurchaseOrdersByVendorAsync(int vendorId)
        {
            SetAuthHeader();
            var response = await _httpClient.GetFromJsonAsync<GetPurchaseOrdersResponse>($"api/purchaseorder/vendor/{vendorId}/pending");
            return response?.PurchaseOrders ?? new List<PurchaseOrderDto>();
        }

        public async Task<ApiResult<PurchaseOrderDto>> CreatePurchaseOrderWithResultAsync(CreatePurchaseOrderCommand command)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/purchaseorder", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CreatePurchaseOrderResponse>();
                    return new ApiResult<PurchaseOrderDto>
                    {
                        Success = true,
                        Data = result?.PurchaseOrder
                    };
                }

                string errorText = "Unable to create purchase order.";
                try
                {
                    var errObj = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                    if (errObj != null && errObj.TryGetValue("message", out var msg) && msg != null)
                    {
                        errorText = msg.ToString() ?? errorText;
                    }
                    else if (errObj != null && errObj.TryGetValue("detail", out var detail) && detail != null)
                    {
                        errorText = detail.ToString() ?? errorText;
                    }
                    else if (errObj != null && errObj.TryGetValue("title", out var title) && title != null)
                    {
                        errorText = title.ToString() ?? errorText;
                    }
                }
                catch
                {
                    var raw = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(raw)) errorText = raw;
                }

                return new ApiResult<PurchaseOrderDto>
                {
                    Success = false,
                    ErrorMessage = errorText
                };
            }
            catch (Exception ex)
            {
                return new ApiResult<PurchaseOrderDto>
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<PurchaseOrderDto?> CreatePurchaseOrderAsync(CreatePurchaseOrderCommand command)
        {
            var result = await CreatePurchaseOrderWithResultAsync(command);
            return result.Data;
        }

        public async Task<PurchaseOrderDto?> RespondToPurchaseOrderAsync(RespondToPurchaseOrderCommand command)
        {
            SetAuthHeader();
            var response = await _httpClient.PutAsJsonAsync("api/purchaseorder/respond", command);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RespondToPurchaseOrderResponse>();
                return result?.PurchaseOrder;
            }
            return null;
        }

        public async Task<PurchaseOrderDto?> DispatchPurchaseOrderAsync(DispatchPurchaseOrderCommand command)
        {
            SetAuthHeader();
            var response = await _httpClient.PostAsJsonAsync("api/purchaseorder/dispatch", command);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<DispatchPurchaseOrderResponse>();
                return result?.PurchaseOrder;
            }
            return null;
        }

        public async Task<PurchaseOrderDto?> ApprovePurchaseOrderAsync(int purchaseOrderId)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PutAsync($"api/purchaseorder/{purchaseOrderId}/approve", null);
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var result = await response.Content.ReadFromJsonAsync<GetPurchaseOrderByIdResponse>();
                        if (result?.PurchaseOrder != null) return result.PurchaseOrder;
                    }
                    catch { }

                    try
                    {
                        var directDto = await response.Content.ReadFromJsonAsync<PurchaseOrderDto>();
                        if (directDto != null) return directDto;
                    }
                    catch { }

                    return await GetPurchaseOrderByIdAsync(purchaseOrderId);
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] ApprovePurchaseOrderAsync error: {ex.Message}");
                return null;
            }
        }

        public async Task<PurchaseOrderDto?> RejectPurchaseOrderAsync(int purchaseOrderId)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PutAsync($"api/purchaseorder/{purchaseOrderId}/reject", null);
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var result = await response.Content.ReadFromJsonAsync<GetPurchaseOrderByIdResponse>();
                        if (result?.PurchaseOrder != null) return result.PurchaseOrder;
                    }
                    catch { }

                    try
                    {
                        var directDto = await response.Content.ReadFromJsonAsync<PurchaseOrderDto>();
                        if (directDto != null) return directDto;
                    }
                    catch { }

                    return await GetPurchaseOrderByIdAsync(purchaseOrderId);
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] RejectPurchaseOrderAsync error: {ex.Message}");
                return null;
            }
        }

        public async Task<PurchaseOrderDto?> SendPurchaseOrderAsync(int purchaseOrderId)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PostAsync($"api/purchaseorder/{purchaseOrderId}/send", null);
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var result = await response.Content.ReadFromJsonAsync<GetPurchaseOrderByIdResponse>();
                        if (result?.PurchaseOrder != null) return result.PurchaseOrder;
                    }
                    catch { }

                    try
                    {
                        var directDto = await response.Content.ReadFromJsonAsync<PurchaseOrderDto>();
                        if (directDto != null) return directDto;
                    }
                    catch { }

                    return await GetPurchaseOrderByIdAsync(purchaseOrderId);
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] SendPurchaseOrderAsync error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<DeliveryRecordDto>> GetDeliveryRecordsByPurchaseOrderAsync(int purchaseOrderId)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GetDeliveryRecordsResponse>($"api/deliveryrecords/purchase-order/{purchaseOrderId}");
                if (response != null)
                {
                    if (response.Deliveries != null && response.Deliveries.Count > 0)
                        return response.Deliveries;
                    if (response.DeliveryRecords != null && response.DeliveryRecords.Count > 0)
                        return response.DeliveryRecords;
                }
                return new List<DeliveryRecordDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] GetDeliveryRecordsByPurchaseOrderAsync error: {ex.Message}");
                return new List<DeliveryRecordDto>();
            }
        }

        public async Task<DeliveryRecordDto?> CreateDeliveryRecordAsync(CreateDeliveryRecordCommand command)
        {
            SetAuthHeader();
            var response = await _httpClient.PostAsJsonAsync("api/deliveryrecords", command);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CreateDeliveryRecordResponse>();
                return result?.DeliveryRecord;
            }
            return null;
        }

        public async Task<DeliveryRecordDto?> ConfirmDeliveryRecordAsync(ConfirmDeliveryRecordCommand command)
        {
            SetAuthHeader();
            var response = await _httpClient.PostAsJsonAsync("api/deliveryrecords/confirm", command);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ConfirmDeliveryRecordResponse>();
                return result?.DeliveryRecord;
            }
            return null;
        }

        public async Task<GetSpoilageAdviceResponse?> GetSpoilageAdviceAsync(int purchaseOrderId)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetAsync($"api/deliveryrecords/spoilage-advice/{purchaseOrderId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<GetSpoilageAdviceResponse>();
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] GetSpoilageAdviceAsync error: {ex.Message}");
                return null;
            }
        }

        public async Task<ContractDto?> GetContractByIdAsync(int contractId)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GetContractByIdResponse>($"api/contract/{contractId}");
                return response?.Contract;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<ContractDto>> GetContractsByOrganizationAsync(int organizationId)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GetContractsResponse>($"api/contract/organization/{organizationId}");
                return response?.Contracts ?? new List<ContractDto>();
            }
            catch
            {
                return new List<ContractDto>();
            }
        }

        public async Task<ContractDto?> CreateContractFromQuotationAsync(int quotationId, List<CreateContractVendorAllocationDto>? allocations = null)
        {
            SetAuthHeader();
            try
            {
                var command = new CreateContractFromQuotationCommand
                {
                    QuotationID = quotationId,
                    Allocations = allocations
                };
                var response = await _httpClient.PostAsJsonAsync("api/contract/from-quotation", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CreateContractFromQuotationResponse>();
                    return result?.Contract;
                }
                else
                {
                    // Fallback to URL-based endpoint
                    var fallback = await _httpClient.PostAsJsonAsync($"api/contract/from-quotation/{quotationId}", allocations);
                    if (fallback.IsSuccessStatusCode)
                    {
                        var result = await fallback.Content.ReadFromJsonAsync<CreateContractFromQuotationResponse>();
                        return result?.Contract;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Error creating contract from quotation: {ex.Message}");
            }
            return null;
        }

        public async Task<ApiResult<ContractDto>> ResetContractAsync(int contractId)
        {
            SetAuthHeader();
            try
            {
                var command = new VenodorManagementFrontend.Models.Contracts.Commands.ResetContractCommand
                {
                    ContractID = contractId
                };

                var response = await _httpClient.PostAsJsonAsync("api/contract/reset", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<VenodorManagementFrontend.Models.Contracts.Responses.ResetContractResponse>();
                    return new ApiResult<ContractDto>
                    {
                        Success = true,
                        Data = result?.Contract
                    };
                }

                string errorText = "Unable to reset contract.";
                try
                {
                    var errObj = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                    if (errObj != null && errObj.TryGetValue("message", out var msg))
                    {
                        errorText = msg?.ToString() ?? errorText;
                    }
                }
                catch
                {
                    var raw = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(raw)) errorText = raw;
                }

                return new ApiResult<ContractDto>
                {
                    Success = false,
                    ErrorMessage = errorText
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Error resetting contract: {ex.Message}");
                return new ApiResult<ContractDto>
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    
                public async Task<List<VendorProductDto>> GetVendorProductsAsync()
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GetVendorProductsResponse>("api/vendorproducts");
                return response?.VendorProducts ?? new List<VendorProductDto>();
            }
            catch
            {
                return new List<VendorProductDto>();
            }
        }

        public async Task<ApiResult<VendorProductDto>> CreateVendorProductAsync(CreateVendorProductCommand command)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/vendorproducts", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CreateVendorProductResponse>();
                    return new ApiResult<VendorProductDto> { Success = true, Data = result?.VendorProduct };
                }

                string errorText = "Unable to create vendor product mapping.";
                try
                {
                    var errObj = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    if (errObj != null && errObj.TryGetValue("message", out var msg) && !string.IsNullOrWhiteSpace(msg))
                    {
                        errorText = msg;
                    }
                }
                catch { }

                return new ApiResult<VendorProductDto> { Success = false, ErrorMessage = errorText };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] CreateVendorProductAsync error: {ex.Message}");
                return new ApiResult<VendorProductDto> { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<ApiResult<VendorProductDto>> UpdateVendorProductAsync(int vendorProductId, UpdateVendorProductCommand command)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/vendorproducts/{vendorProductId}", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<UpdateVendorProductResponse>();
                    return new ApiResult<VendorProductDto> { Success = true, Data = result?.VendorProduct };
                }

                string errorText = "Unable to update vendor product mapping.";
                try
                {
                    var errObj = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    if (errObj != null && errObj.TryGetValue("message", out var msg) && !string.IsNullOrWhiteSpace(msg))
                    {
                        errorText = msg;
                    }
                }
                catch { }

                return new ApiResult<VendorProductDto> { Success = false, ErrorMessage = errorText };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] UpdateVendorProductAsync error: {ex.Message}");
                return new ApiResult<VendorProductDto> { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<ApiResult> DeleteVendorProductAsync(int vendorProductId)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.DeleteAsync($"api/vendorproducts/{vendorProductId}");
                if (response.IsSuccessStatusCode)
                {
                    return new ApiResult { Success = true };
                }

                string errorText = "Unable to delete vendor product mapping.";
                try
                {
                    var errObj = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    if (errObj != null && errObj.TryGetValue("message", out var msg) && !string.IsNullOrWhiteSpace(msg))
                    {
                        errorText = msg;
                    }
                }
                catch { }

                return new ApiResult { Success = false, ErrorMessage = errorText };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] DeleteVendorProductAsync error: {ex.Message}");
                return new ApiResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<List<VendorProcurementOpportunityDto>> GetVendorProcurementOpportunitiesAsync()
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GetVendorProcurementOpportunitiesResponse>("api/purchaserequests/vendor/opportunities");
                if (response?.Opportunities != null && response.Opportunities.Count > 0)
                {
                    return response.Opportunities;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] GetVendorProcurementOpportunitiesAsync backend endpoint error/fallback: {ex.Message}");
            }

            // DYNAMIC CALCULATION: Compute opportunities dynamically based on authenticated VendorID, active VendorProducts, and open PurchaseRequests
            try
            {
                var vendorId = _authService.VendorID;
                if (!vendorId.HasValue || vendorId.Value <= 0)
                {
                    // Attempt to resolve from user record if missing
                    if (_authService.UserID > 0)
                    {
                        var allUsers = await GetUsersAsync();
                        var me = allUsers.FirstOrDefault(u => u.UserID == _authService.UserID);
                        if (me?.VendorID.HasValue == true && me.VendorID.Value > 0)
                        {
                            vendorId = me.VendorID.Value;
                        }
                    }
                }

                if (!vendorId.HasValue || vendorId.Value <= 0)
                {
                    return new List<VendorProcurementOpportunityDto>();
                }

                var vendorProductsTask = GetVendorProductsAsync();
                var requestsTask = GetPurchaseRequestsAsync();
                var productsTask = GetProductsAsync();
                var outletsTask = GetOutletsAsync();
                var orgsTask = GetOrganizationsAsync();
                var usersTask = GetUsersAsync();

                await Task.WhenAll(vendorProductsTask, requestsTask, productsTask, outletsTask, orgsTask, usersTask);

                var vpList = await vendorProductsTask ?? new List<VendorProductDto>();
                var activeMappings = vpList
                    .Where(vp => vp.VendorID == vendorId.Value && string.Equals(vp.Status, "Active", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (activeMappings.Count == 0)
                {
                    return new List<VendorProcurementOpportunityDto>();
                }

                var mappingDict = activeMappings
                    .GroupBy(vp => vp.ProductID)
                    .ToDictionary(g => g.Key, g => g.First());

                var activeProductIds = mappingDict.Keys.ToHashSet();

                var requests = await requestsTask ?? new List<PurchaseRequestDto>();
                var products = (await productsTask ?? new List<ProductDto>()).ToDictionary(p => p.ProductID, p => p);
                var outlets = (await outletsTask ?? new List<OutletDto>()).ToDictionary(o => o.OutletID, o => o);
                var orgs = (await orgsTask ?? new List<OrganizationDto>()).ToDictionary(o => o.OrganizationID, o => o);
                var userDict = (await usersTask ?? new List<UserDto>()).ToDictionary(u => u.UserID, u => u);

                var result = new List<VendorProcurementOpportunityDto>();

                var allQuotations = await GetQuotationsAsync() ?? new List<QuotationDto>();
                var myQuotedRequestIds = allQuotations
                    .Where(q => q.VendorID == vendorId.Value)
                    .Select(q => q.RequestID)
                    .ToHashSet();

                foreach (var req in requests)
                {
                    if (req.Items == null || req.Items.Count == 0) continue;
                    if (myQuotedRequestIds.Contains(req.RequestID)) continue;
                    if (string.Equals(req.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(req.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                        continue;

                    userDict.TryGetValue(req.CreatedByUserID, out var creator);
                    string creatorName = creator?.Name ?? "Outlet Staff";
                    string creatorRole = creator?.RoleName ?? (creator?.RoleID == 3 ? "Outlet Manager" : "Staff");

                    foreach (var item in req.Items)
                    {
                        if (activeProductIds.Contains(item.ProductID))
                        {
                            var vp = mappingDict[item.ProductID];
                            products.TryGetValue(item.ProductID, out var prod);
                            outlets.TryGetValue(req.OutletID, out var outlet);
                            OrganizationDto? org = null;
                            if (outlet != null) orgs.TryGetValue(outlet.OrganizationID, out org);

                            result.Add(new VendorProcurementOpportunityDto
                            {
                                RequestID = req.RequestID,
                                VendorID = vendorId.Value,
                                ProductID = item.ProductID,
                                ProductName = prod?.ProductName ?? $"Product #{item.ProductID}",
                                Category = prod?.Category ?? "General",
                                RequestedQuantity = item.Quantity,
                                Unit = !string.IsNullOrWhiteSpace(item.Unit) ? item.Unit : (prod?.Unit ?? "Units"),
                                UnitPrice = vp.UnitPrice,
                                EstimatedDeliveryDays = vp.EstimatedDeliveryDays,
                                OrganizationName = org?.OrganizationName ?? "Organization",
                                OutletName = outlet?.OutletName ?? $"Outlet #{req.OutletID}",
                                OutletAddress = outlet?.Address ?? string.Empty,
                                CreatedByUserID = req.CreatedByUserID,
                                CreatedByUserName = creatorName,
                                CreatedByUserRole = creatorRole,
                                RequestDate = req.RequestDate,
                                Status = req.Status,
                                OpportunityStatus = "Pending"
                            });
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Error loading vendor opportunities: {ex.Message}");
                return new List<VendorProcurementOpportunityDto>();
            }
        }

        public async Task<ApiResult<RespondToOpportunityResponse>> RespondToOpportunityAsync(RespondToOpportunityCommand command)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/purchaserequests/vendor/opportunities/respond", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<RespondToOpportunityResponse>();
                    return new ApiResult<RespondToOpportunityResponse> { Success = true, Data = result };
                }

                string errorText = "Unable to respond to procurement opportunity.";
                try
                {
                    var errObj = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    if (errObj != null && errObj.TryGetValue("message", out var msg) && !string.IsNullOrWhiteSpace(msg))
                    {
                        errorText = msg;
                    }
                    else
                    {
                        var raw = await response.Content.ReadAsStringAsync();
                        if (!string.IsNullOrWhiteSpace(raw)) errorText = raw;
                    }
                }
                catch { }

                return new ApiResult<RespondToOpportunityResponse> { Success = false, ErrorMessage = errorText };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] RespondToOpportunityAsync exception: {ex.Message}");
                return new ApiResult<RespondToOpportunityResponse> { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<List<NotificationDto>> GetMyNotificationsAsync()
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GetNotificationsResponse>("api/notifications");
                return response?.Notifications ?? new List<NotificationDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] GetMyNotificationsAsync error: {ex.Message}");
                return new List<NotificationDto>();
            }
        }

        public async Task<bool> MarkNotificationReadAsync(int notificationId)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PutAsync($"api/notifications/{notificationId}/read", null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<InvoiceDto>> GetInvoicesAsync()
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GetInvoicesResponse>("api/invoice");
                return response?.Invoices ?? new List<InvoiceDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] GetInvoicesAsync error: {ex.Message}");
                return new List<InvoiceDto>();
            }
        }

        public async Task<InvoiceDto?> GetInvoiceByIdAsync(int invoiceId)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GetInvoiceByIdResponse>($"api/invoice/{invoiceId}");
                return response?.Invoice;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] GetInvoiceByIdAsync error: {ex.Message}");
                return null;
            }
        }

        public async Task<ApiResult<InvoiceDto>> CreateInvoiceAsync(CreateInvoiceRequest request)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/invoice", request);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<GetInvoiceByIdResponse>();
                    return new ApiResult<InvoiceDto> { Success = true, Data = result?.Invoice };
                }

                string errorText = "Unable to create invoice.";
                try
                {
                    var errObj = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    if (errObj != null && errObj.TryGetValue("message", out var msg) && !string.IsNullOrWhiteSpace(msg))
                    {
                        errorText = msg;
                    }
                    else
                    {
                        var raw = await response.Content.ReadAsStringAsync();
                        if (!string.IsNullOrWhiteSpace(raw)) errorText = raw;
                    }
                }
                catch { }

                return new ApiResult<InvoiceDto> { Success = false, ErrorMessage = errorText };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] CreateInvoiceAsync exception: {ex.Message}");
                return new ApiResult<InvoiceDto> { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<ApiResult<InvoiceDto>> ApproveInvoiceAsync(int invoiceId)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PostAsync($"api/invoice/{invoiceId}/approve", null);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<GetInvoiceByIdResponse>();
                    return new ApiResult<InvoiceDto> { Success = true, Data = result?.Invoice };
                }

                string errorText = "Unable to approve invoice.";
                try
                {
                    var raw = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(raw)) errorText = raw;
                }
                catch { }

                return new ApiResult<InvoiceDto> { Success = false, ErrorMessage = errorText };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] ApproveInvoiceAsync exception: {ex.Message}");
                return new ApiResult<InvoiceDto> { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<ApiResult<InvoiceDto>> RejectInvoiceAsync(int invoiceId, string reason)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"api/invoice/{invoiceId}/reject", new RejectInvoiceRequest { Reason = reason });
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<GetInvoiceByIdResponse>();
                    return new ApiResult<InvoiceDto> { Success = true, Data = result?.Invoice };
                }

                string errorText = "Unable to reject invoice.";
                try
                {
                    var raw = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(raw)) errorText = raw;
                }
                catch { }

                return new ApiResult<InvoiceDto> { Success = false, ErrorMessage = errorText };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] RejectInvoiceAsync exception: {ex.Message}");
                return new ApiResult<InvoiceDto> { Success = false, ErrorMessage = ex.Message };
            }
        }

        public string GetInvoiceDownloadUrl(int invoiceId)
        {
            var baseUri = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "https://localhost:7193";
            return $"{baseUri}/api/invoice/{invoiceId}/download";
        }

        public async Task<byte[]?> DownloadInvoicePdfAsync(int invoiceId)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetAsync($"api/invoice/{invoiceId}/download");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] DownloadInvoicePdfAsync error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<PaymentDto>> GetPaymentsAsync()
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetAsync("api/invoice/payments");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<GetPaymentsResponse>();
                    return result?.Payments ?? new List<PaymentDto>();
                }
                return new List<PaymentDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] GetPaymentsAsync error: {ex.Message}");
                return new List<PaymentDto>();
            }
        }

        public async Task<ApiResult<InvoiceDto>> PayInvoiceAsync(MarkInvoicePaidCommand command)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/invoice/pay", command);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<MarkInvoicePaidResponse>();
                    return new ApiResult<InvoiceDto> { Success = true, Data = result?.Invoice };
                }

                string errorText = "Unable to process payment.";
                try
                {
                    var raw = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(raw)) errorText = raw;
                }
                catch { }

                return new ApiResult<InvoiceDto> { Success = false, ErrorMessage = errorText };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] PayInvoiceAsync exception: {ex.Message}");
                return new ApiResult<InvoiceDto> { Success = false, ErrorMessage = ex.Message };
            }
        }

        // -------------------------------------------------------------
        // VENDOR PERFORMANCE & REVIEWS
        // -------------------------------------------------------------
        public async Task<List<VenodorManagementFrontend.Models.VendorPerformance.VendorPerformanceSummaryDto>> GetVendorPerformanceSummariesAsync()
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<VenodorManagementFrontend.Models.VendorPerformance.VendorPerformanceSummaryDto>>("api/VendorPerformance");
                return response ?? new List<VenodorManagementFrontend.Models.VendorPerformance.VendorPerformanceSummaryDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] GetVendorPerformanceSummariesAsync error: {ex.Message}");
                return new List<VenodorManagementFrontend.Models.VendorPerformance.VendorPerformanceSummaryDto>();
            }
        }

        public async Task<VenodorManagementFrontend.Models.VendorPerformance.VendorPerformanceSummaryDto?> GetVendorPerformanceByIdAsync(int vendorId)
        {
            SetAuthHeader();
            try
            {
                return await _httpClient.GetFromJsonAsync<VenodorManagementFrontend.Models.VendorPerformance.VendorPerformanceSummaryDto>($"api/VendorPerformance/{vendorId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] GetVendorPerformanceByIdAsync error: {ex.Message}");
                return null;
            }
        }

        public async Task<VenodorManagementFrontend.Models.VendorPerformance.VendorReviewDto?> GetVendorFeedbackByIdAsync(int feedbackId)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetAsync($"api/VendorFeedback/{feedbackId}");
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[ApiService] GetVendorFeedbackByIdAsync status: {response.StatusCode}");
                    return null;
                }
                return await response.Content.ReadFromJsonAsync<VenodorManagementFrontend.Models.VendorPerformance.VendorReviewDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] GetVendorFeedbackByIdAsync error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<VenodorManagementFrontend.Models.VendorPerformance.VendorReviewDto>> GetVendorReviewsAsync(int vendorId, int? productId = null)
        {
            SetAuthHeader();
            try
            {
                var url = productId.HasValue && productId.Value > 0
                    ? $"api/VendorFeedback/vendor/{vendorId}?productId={productId.Value}"
                    : $"api/VendorFeedback/vendor/{vendorId}";
                var response = await _httpClient.GetFromJsonAsync<List<VenodorManagementFrontend.Models.VendorPerformance.VendorReviewDto>>(url);
                return response ?? new List<VenodorManagementFrontend.Models.VendorPerformance.VendorReviewDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] GetVendorReviewsAsync error: {ex.Message}");
                return new List<VenodorManagementFrontend.Models.VendorPerformance.VendorReviewDto>();
            }
        }

        public async Task<List<VenodorManagementFrontend.Models.VendorPerformance.EligibleReviewOrderDto>> GetEligibleReviewOrdersAsync()
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<VenodorManagementFrontend.Models.VendorPerformance.EligibleReviewOrderDto>>("api/VendorFeedback/eligible-orders");
                return response ?? new List<VenodorManagementFrontend.Models.VendorPerformance.EligibleReviewOrderDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] GetEligibleReviewOrdersAsync error: {ex.Message}");
                return new List<VenodorManagementFrontend.Models.VendorPerformance.EligibleReviewOrderDto>();
            }
        }

        public async Task<VenodorManagementFrontend.Models.ApiResult> CreateVendorReviewAsync(VenodorManagementFrontend.Models.VendorPerformance.CreateVendorReviewRequest request)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/VendorFeedback", request);
                if (response.IsSuccessStatusCode)
                {
                    return new VenodorManagementFrontend.Models.ApiResult { Success = true };
                }

                string errorText = "Unable to submit review.";
                try
                {
                    var raw = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        if (raw.StartsWith("{") && raw.Contains("\"message\""))
                        {
                            try
                            {
                                using var doc = System.Text.Json.JsonDocument.Parse(raw);
                                if (doc.RootElement.TryGetProperty("message", out var msgProp))
                                {
                                    errorText = msgProp.GetString() ?? errorText;
                                }
                            }
                            catch { errorText = raw; }
                        }
                        else
                        {
                            errorText = raw;
                        }
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        errorText = "You are not authorized to submit reviews for this transaction.";
                    }
                }
                catch { }

                return new VenodorManagementFrontend.Models.ApiResult { Success = false, ErrorMessage = errorText };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] CreateVendorReviewAsync error: {ex.Message}");
                return new VenodorManagementFrontend.Models.ApiResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<VenodorManagementFrontend.Models.VendorPerformance.VendorAiInsightsDto?> GetVendorAiInsightsAsync(int vendorId)
        {
            SetAuthHeader();
            try
            {
                var response = await _httpClient.GetAsync($"api/vendorfeedback/vendor/{vendorId}/ai-insights");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<VenodorManagementFrontend.Models.VendorPerformance.VendorAiInsightsDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] GetVendorAiInsightsAsync error: {ex.Message}");
            }
            return null;
        }
    }
}


