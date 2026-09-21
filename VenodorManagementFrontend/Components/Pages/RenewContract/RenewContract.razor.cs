using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.RenewContract;

public partial class RenewContract : ComponentBase
{
    [Parameter]
    public int contractId { get; set; }

    [Inject]
    public ApiService Api { get; set; } = default!;

    [Inject]
    public AuthService Auth { get; set; } = default!;

    [Inject]
    public NavigationManager Nav { get; set; } = default!;

    private bool IsLoading { get; set; } = true;
    private bool IsProcessing { get; set; } = false;
    private bool HasError { get; set; } = false;
    private bool IsNotExpired { get; set; } = false;

    private string? ErrorMessage { get; set; }
    private string? ValidationMessage { get; set; }
    private string? SuccessMessage { get; set; }

    private ContractDto? ExpiredContract { get; set; }

    // Read-only contract context
    private string OrganizationName { get; set; } = "Organization";
    private string OutletName { get; set; } = "Outlet";
    private string VendorName { get; set; } = "Vendor";
    private string ContractNumberDisplay => $"Contract #{contractId}";

    // Editable renewal specifications
    private DateTime StartDate { get; set; } = DateTime.Today;
    private DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);
    private string PaymentMethod { get; set; } = "Bank Transfer";

    // Multi-product renewal items
    public class RenewalProductLineItem
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = "Kg";
        public decimal OldContractQuantity { get; set; }
        public decimal OldPurchasedQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal NewQuantity { get; set; }
        public string? QuantityInputString { get; set; }
        public decimal EstimatedLineTotal => Math.Max(0m, NewQuantity * UnitPrice);
    }

    private List<RenewalProductLineItem> ProductLines { get; set; } = new();

    // Review Modal / Step State
    private bool IsReviewModalOpen { get; set; } = false;

    // Sidebar & Profile
    private bool IsSidebarCollapsed { get; set; } = false;
    private bool IsProfileDropdownOpen { get; set; } = false;

    private decimal TotalReferenceQuantity => ProductLines.Sum(p => p.NewQuantity);
    private decimal TotalEstimatedValue => ProductLines.Sum(p => p.EstimatedLineTotal);
    private decimal TotalOldQuantity => ProductLines.Sum(p => p.OldContractQuantity);

    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    private void ToggleProfileDropdown()
    {
        IsProfileDropdownOpen = !IsProfileDropdownOpen;
    }

    private void HandleLogout()
    {
        Auth.Logout();
        Nav.NavigateTo("/login", true);
    }

    private string GetUserInitial()
    {
        if (!string.IsNullOrWhiteSpace(Auth.UserName) && Auth.UserName != "Guest")
        {
            return Auth.UserName[0].ToString().ToUpperInvariant();
        }
        return "O";
    }

    protected override async Task OnInitializedAsync()
    {
        if (Auth.IsAuthenticated)
        {
            await LoadExpiredContractData();
        }
        else
        {
            IsLoading = false;
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Auth.IsAuthenticated && ExpiredContract != null && ExpiredContract.ContractID != contractId)
        {
            await LoadExpiredContractData();
        }
    }

    private async Task LoadExpiredContractData()
    {
        if (contractId <= 0)
        {
            HasError = true;
            ErrorMessage = "Invalid Contract ID specified.";
            IsLoading = false;
            return;
        }

        IsLoading = true;
        HasError = false;
        IsNotExpired = false;
        ErrorMessage = null;
        ValidationMessage = null;
        SuccessMessage = null;
        StateHasChanged();

        try
        {
            var contract = await Api.GetContractByIdAsync(contractId);
            if (contract == null)
            {
                HasError = true;
                ErrorMessage = $"Contract #{contractId} could not be found.";
                return;
            }

            ExpiredContract = contract;

            // Security authorization check
            if (Auth.IsOrgManager && Auth.OrganizationID.HasValue && Auth.OrganizationID.Value > 0)
            {
                if (contract.OrganizationID > 0 && contract.OrganizationID != Auth.OrganizationID.Value)
                {
                    HasError = true;
                    ErrorMessage = "You are not authorized to renew contracts outside your organization.";
                    ExpiredContract = null;
                    return;
                }
            }

            // Expiry status check: Contract MUST be Expired
            if (!string.Equals(contract.Status, "Expired", StringComparison.OrdinalIgnoreCase))
            {
                IsNotExpired = true;
                ErrorMessage = $"Contract #{contractId} is currently '{contract.Status}' and cannot be renewed. Only Expired contracts can be renewed.";
                return;
            }

            // Enrich related entities (outlets, products, vendors, organizations)
            var outletsTask = Api.GetOutletsAsync();
            var productsTask = Api.GetProductsAsync();
            var vendorsTask = Api.GetVendorsAsync();
            var orgsTask = Api.GetOrganizationsAsync();

            await Task.WhenAll(outletsTask, productsTask, vendorsTask, orgsTask);

            var outlets = await outletsTask ?? new();
            var products = await productsTask ?? new();
            var vendors = await vendorsTask ?? new();
            var orgs = await orgsTask ?? new();

            var matchedOutlet = outlets.FirstOrDefault(o => o.OutletID == contract.OutletID);
            OutletName = !string.IsNullOrWhiteSpace(matchedOutlet?.OutletName)
                ? matchedOutlet.OutletName
                : (!string.IsNullOrWhiteSpace(contract.OutletName) ? contract.OutletName : $"Outlet #{contract.OutletID}");

            int orgId = contract.OrganizationID > 0 ? contract.OrganizationID : (matchedOutlet?.OrganizationID ?? Auth.OrganizationID ?? 0);
            var matchedOrg = orgs.FirstOrDefault(o => o.OrganizationID == orgId);
            OrganizationName = !string.IsNullOrWhiteSpace(matchedOrg?.OrganizationName)
                ? matchedOrg.OrganizationName
                : (!string.IsNullOrWhiteSpace(contract.OrganizationName) ? contract.OrganizationName : "Organization");

            int vendorId = contract.VendorID ?? contract.Allocations?.FirstOrDefault()?.VendorID ?? 0;
            var matchedVendor = vendors.FirstOrDefault(v => v.VendorID == vendorId);
            VendorName = !string.IsNullOrWhiteSpace(matchedVendor?.VendorName)
                ? matchedVendor.VendorName
                : (!string.IsNullOrWhiteSpace(contract.VendorName) ? contract.VendorName : (vendorId > 0 ? $"Vendor #{vendorId}" : "Primary Vendor"));

            // Pre-fill editable fields
            StartDate = DateTime.Today;
            EndDate = DateTime.Today.AddDays(30);
            PaymentMethod = !string.IsNullOrWhiteSpace(contract.PaymentMethod) ? contract.PaymentMethod : "Bank Transfer";

            // Populate multi-product lines
            ProductLines.Clear();

            if (contract.ContractProducts != null && contract.ContractProducts.Count > 0)
            {
                foreach (var cp in contract.ContractProducts)
                {
                    var p = products.FirstOrDefault(pr => pr.ProductID == cp.ProductID);
                    string pName = !string.IsNullOrWhiteSpace(cp.ProductName) && !cp.ProductName.StartsWith("Product #", StringComparison.OrdinalIgnoreCase)
                        ? cp.ProductName
                        : (p?.ProductName ?? $"Product #{cp.ProductID}");
                    string pUnit = !string.IsNullOrWhiteSpace(cp.Unit) ? cp.Unit : (p?.Unit ?? (!string.IsNullOrWhiteSpace(contract.Unit) ? contract.Unit : "Kg"));
                    decimal uPrice = cp.UnitPrice.HasValue && cp.UnitPrice.Value > 0 ? cp.UnitPrice.Value : contract.UnitPrice;

                    ProductLines.Add(new RenewalProductLineItem
                    {
                        ProductID = cp.ProductID,
                        ProductName = pName,
                        Unit = pUnit,
                        OldContractQuantity = cp.ContractQuantity,
                        OldPurchasedQuantity = cp.PurchasedQuantity,
                        UnitPrice = uPrice,
                        NewQuantity = cp.ContractQuantity,
                        QuantityInputString = FormatQuantity(cp.ContractQuantity)
                    });
                }
            }
            else
            {
                // Backward compatibility fallback for legacy single-product contracts
                var p = products.FirstOrDefault(pr => pr.ProductID == contract.ProductID);
                string pName = !string.IsNullOrWhiteSpace(contract.ProductName)
                    ? contract.ProductName
                    : (p?.ProductName ?? (contract.ProductID > 0 ? $"Product #{contract.ProductID}" : "Contracted Product"));
                string pUnit = !string.IsNullOrWhiteSpace(contract.Unit) ? contract.Unit : (p?.Unit ?? "Kg");

                ProductLines.Add(new RenewalProductLineItem
                {
                    ProductID = contract.ProductID,
                    ProductName = pName,
                    Unit = pUnit,
                    OldContractQuantity = contract.TotalQuantity,
                    OldPurchasedQuantity = contract.UsedQuantity,
                    UnitPrice = contract.UnitPrice,
                    NewQuantity = contract.TotalQuantity,
                    QuantityInputString = FormatQuantity(contract.TotalQuantity)
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RenewContract] Load error: {ex.Message}");
            HasError = true;
            ErrorMessage = "An error occurred while loading contract information for renewal.";
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private void OnProductQuantityInput(RenewalProductLineItem item, ChangeEventArgs e)
    {
        item.QuantityInputString = e.Value?.ToString() ?? string.Empty;
        ValidationMessage = null;

        string trimmed = item.QuantityInputString.Trim();
        if (decimal.TryParse(trimmed, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedQty) ||
            decimal.TryParse(trimmed, out parsedQty))
        {
            if (parsedQty >= 0)
            {
                item.NewQuantity = parsedQty;
            }
            else
            {
                item.NewQuantity = 0;
            }
        }
        else
        {
            item.NewQuantity = 0;
        }

        StateHasChanged();
    }

    private bool ValidateRenewalForm()
    {
        ValidationMessage = null;

        if (ExpiredContract == null)
        {
            ValidationMessage = "No contract selected for renewal.";
            return false;
        }

        if (!string.Equals(ExpiredContract.Status, "Expired", StringComparison.OrdinalIgnoreCase))
        {
            ValidationMessage = "Only Expired contracts can be renewed.";
            return false;
        }

        if (EndDate <= StartDate)
        {
            ValidationMessage = "End Date must be strictly after Start Date.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(PaymentMethod))
        {
            ValidationMessage = "Please select a payment method.";
            return false;
        }

        if (ProductLines.Count == 0)
        {
            ValidationMessage = "At least one product line must exist to renew the contract.";
            return false;
        }

        // Check for duplicate products
        var duplicateIds = ProductLines.GroupBy(p => p.ProductID).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateIds.Any())
        {
            ValidationMessage = "Duplicate products detected in renewal lines.";
            return false;
        }

        // Check quantities
        foreach (var line in ProductLines)
        {
            if (line.ProductID <= 0)
            {
                ValidationMessage = "Invalid product reference found.";
                return false;
            }

            if (line.NewQuantity <= 0)
            {
                ValidationMessage = $"Planning quantity for '{line.ProductName}' must be greater than zero.";
                return false;
            }
        }

        return true;
    }

    private void OpenReviewModal()
    {
        if (!ValidateRenewalForm())
        {
            return;
        }

        IsReviewModalOpen = true;
    }

    private void CloseReviewModal()
    {
        IsReviewModalOpen = false;
    }

    private async Task SubmitRenewalAsync()
    {
        if (!ValidateRenewalForm())
        {
            IsReviewModalOpen = false;
            return;
        }

        IsProcessing = true;
        ErrorMessage = null;
        ValidationMessage = null;
        StateHasChanged();

        try
        {
            var command = new RenewContractCommand
            {
                StartDate = StartDate,
                EndDate = EndDate,
                PaymentMethod = PaymentMethod,
                Products = ProductLines.Select(p => new RenewContractProductItemDto
                {
                    ProductID = p.ProductID,
                    ContractQuantity = p.NewQuantity,
                    UnitPrice = p.UnitPrice
                }).ToList()
            };

            var result = await Api.RenewContractAsync(contractId, command);

            if (result != null && result.Success && result.Data != null && result.Data.ContractID > 0)
            {
                int newContractId = result.Data.ContractID;
                IsReviewModalOpen = false;
                SuccessMessage = $"Contract #{contractId} renewed successfully. New Active Contract #{newContractId} has been created.";
                StateHasChanged();

                // Navigate to the newly created Active contract details
                await Task.Delay(400);
                Nav.NavigateTo($"/contract/{newContractId}");
            }
            else
            {
                ErrorMessage = !string.IsNullOrWhiteSpace(result?.ErrorMessage)
                    ? result.ErrorMessage
                    : "Unable to renew contract. Please check all details and try again.";
                IsReviewModalOpen = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RenewContract] Submit exception: {ex.Message}");
            ErrorMessage = ex.Message;
            IsReviewModalOpen = false;
        }
        finally
        {
            IsProcessing = false;
            StateHasChanged();
        }
    }

    private void NavigateBack()
    {
        Nav.NavigateTo($"/contract/{contractId}");
    }

    private void NavigateToContractsList()
    {
        Nav.NavigateTo("/organization/contracts");
    }

    private string FormatQuantity(decimal qty)
    {
        return qty % 1 == 0 ? qty.ToString("0") : qty.ToString("N2");
    }
}
