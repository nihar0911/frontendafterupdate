using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.ContractDetails;

public partial class ContractDetails : ComponentBase
{
    [Parameter] public int? contractId { get; set; }

    private ContractDto? Contract { get; set; }
    private bool IsLoading { get; set; } = true;
    private bool HasError { get; set; } = false;
    private string ErrorMessage { get; set; } = "You are not authorized to view this contract or it does not exist.";

    private bool IsSidebarCollapsed { get; set; } = false;
    private bool IsProfileDropdownOpen { get; set; } = false;

    private decimal RemainingQuantity => Math.Max((Contract?.TotalQuantity ?? 0) - (Contract?.UsedQuantity ?? 0), 0m);
    private decimal ExtraOrderQuantity => Math.Max((Contract?.UsedQuantity ?? 0) - (Contract?.TotalQuantity ?? 0), 0m);
    private decimal ComputedTotalValue
    {
        get
        {
            if (Contract?.TotalAmount > 0) return Contract.TotalAmount;
            if (Contract?.ContractProducts != null && Contract.ContractProducts.Count > 1)
            {
                decimal sum = Contract.ContractProducts.Sum(p => p.ContractQuantity * (p.UnitPrice ?? Contract.UnitPrice));
                if (sum > 0) return sum + (Contract?.TaxAmount ?? 0m);
            }
            return ((Contract?.TotalQuantity ?? 0) * (Contract?.UnitPrice ?? 0) + (Contract?.TaxAmount ?? 0));
        }
    }

    private string GetContractProductsSummary()
    {
        if (Contract?.ContractProducts != null && Contract.ContractProducts.Count > 1)
        {
            return string.Join(", ", Contract.ContractProducts.Select(p => p.ProductName));
        }
        return Contract?.ProductName ?? "Products";
    }

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
        if (!string.IsNullOrWhiteSpace(Auth.UserName))
        {
            return Auth.UserName[0].ToString().ToUpperInvariant();
        }
        return "G";
    }

    protected override async Task OnInitializedAsync()
    {
        await CheckAndLoad();
    }

    protected override async Task OnParametersSetAsync()
    {
        await CheckAndLoad();
    }

    private async Task CheckAndLoad()
    {
        int targetId = contractId ?? 0;
        if (targetId > 0)
        {
            if (Contract == null || Contract.ContractID != targetId)
            {
                await LoadDetails(targetId);
            }
        }
        else
        {
            IsLoading = false;
            HasError = true;
            ErrorMessage = "No Contract ID specified in URL.";
        }
    }

    private async Task LoadDetails(int id)
    {
        IsLoading = true;
        HasError = false;
        StateHasChanged();

        try
        {
            Contract = await Api.GetContractByIdAsync(id);
            if (Contract == null)
            {
                HasError = true;
                return;
            }

            // Role Security Authorization Check
            if (Auth.IsOutletManager && Auth.OutletID.HasValue && Auth.OutletID.Value > 0)
            {
                if (Contract.OutletID != Auth.OutletID.Value)
                {
                    HasError = true;
                    ErrorMessage = "You are not authorized to view contracts belonging to another outlet.";
                    Contract = null;
                    return;
                }
            }
            else if (Auth.IsOrgManager && Auth.OrganizationID.HasValue && Auth.OrganizationID.Value > 0)
            {
                if (Contract.OrganizationID > 0 && Contract.OrganizationID != Auth.OrganizationID.Value)
                {
                    HasError = true;
                    ErrorMessage = "You are not authorized to view contracts outside your organization.";
                    Contract = null;
                    return;
                }
            }

            // Enrich missing names and details
            var outletsTask = Api.GetOutletsAsync();
            var productsTask = Api.GetProductsAsync();
            var vendorsTask = Api.GetVendorsAsync();
            var orgsTask = Api.GetOrganizationsAsync();

            await Task.WhenAll(outletsTask, productsTask, vendorsTask, orgsTask);

            var outlets = await outletsTask;
            var matchedOutlet = outlets?.FirstOrDefault(o => o.OutletID == Contract.OutletID);
            if (matchedOutlet != null)
            {
                Contract.OutletName = matchedOutlet.OutletName;
                Contract.OrganizationID = matchedOutlet.OrganizationID;
            }
            else if (string.IsNullOrWhiteSpace(Contract.OutletName))
            {
                Contract.OutletName = $"Outlet #{Contract.OutletID}";
            }

            var orgs = await orgsTask;
            var matchedOrg = orgs?.FirstOrDefault(og => og.OrganizationID == Contract.OrganizationID);
            if (matchedOrg != null)
            {
                Contract.OrganizationName = matchedOrg.OrganizationName;
            }
            else if (string.IsNullOrWhiteSpace(Contract.OrganizationName))
            {
                Contract.OrganizationName = "Organization";
            }

            var products = await productsTask;
            var prod = products?.FirstOrDefault(p => p.ProductID == Contract.ProductID);
            if (prod != null)
            {
                if (string.IsNullOrWhiteSpace(Contract.ProductName)) Contract.ProductName = prod.ProductName;
                if (string.IsNullOrWhiteSpace(Contract.Unit)) Contract.Unit = prod.Unit;
            }
            else if (string.IsNullOrWhiteSpace(Contract.ProductName))
            {
                Contract.ProductName = $"Product #{Contract.ProductID}";
            }

            // Enrich all ContractProducts
            if (Contract.ContractProducts != null && Contract.ContractProducts.Count > 0)
            {
                foreach (var cp in Contract.ContractProducts)
                {
                    var matchedP = products?.FirstOrDefault(p => p.ProductID == cp.ProductID);
                    if (matchedP != null)
                    {
                        if (string.IsNullOrWhiteSpace(cp.ProductName) || cp.ProductName.StartsWith("Product #", StringComparison.OrdinalIgnoreCase))
                        {
                            cp.ProductName = matchedP.ProductName;
                        }
                        if (string.IsNullOrWhiteSpace(cp.Unit))
                        {
                            cp.Unit = matchedP.Unit ?? Contract.Unit ?? "Kg";
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(cp.ProductName))
                    {
                        cp.ProductName = $"Product #{cp.ProductID}";
                    }

                    if (string.IsNullOrWhiteSpace(cp.Unit))
                    {
                        cp.Unit = !string.IsNullOrWhiteSpace(Contract.Unit) ? Contract.Unit : "Kg";
                    }

                    if (!cp.UnitPrice.HasValue || cp.UnitPrice == 0)
                    {
                        if (Contract.UnitPrice > 0)
                        {
                            cp.UnitPrice = Contract.UnitPrice;
                        }
                    }
                }
            }
            else
            {
                // Fallback for single-product contracts if ContractProducts was empty in response
                Contract.ContractProducts = new List<ContractProductDto>
                {
                    new ContractProductDto
                    {
                        ContractID = Contract.ContractID,
                        ProductID = Contract.ProductID,
                        ProductName = !string.IsNullOrWhiteSpace(Contract.ProductName) ? Contract.ProductName : $"Product #{Contract.ProductID}",
                        Unit = !string.IsNullOrWhiteSpace(Contract.Unit) ? Contract.Unit : "Kg",
                        ContractQuantity = Contract.TotalQuantity,
                        PurchasedQuantity = Contract.UsedQuantity,
                        UnitPrice = Contract.UnitPrice
                    }
                };
            }

            var vendors = await vendorsTask;
            var firstAlloc = Contract.Allocations?.FirstOrDefault();
            int vendorId = Contract.VendorID ?? firstAlloc?.VendorID ?? 0;
            var v = vendors?.FirstOrDefault(ven => ven.VendorID == vendorId);
            if (v != null)
            {
                if (string.IsNullOrWhiteSpace(Contract.VendorName)) Contract.VendorName = v.VendorName;
            }
            else if (string.IsNullOrWhiteSpace(Contract.VendorName))
            {
                Contract.VendorName = $"Vendor #{vendorId}";
            }

            // If QuotationID exists, enrich financials and per-product unit prices from quotation
            if (Contract.QuotationID.HasValue && Contract.QuotationID.Value > 0)
            {
                var quote = await Api.GetQuotationByIdAsync(Contract.QuotationID.Value);
                if (quote?.Items != null && quote.Items.Count > 0)
                {
                    if (Contract.UnitPrice == 0)
                    {
                        var qItem = quote.Items.First();
                        Contract.UnitPrice = qItem.UnitPrice;
                        Contract.TaxAmount = qItem.TaxAmount;
                        Contract.TotalAmount = quote.Items.Sum(i => i.TotalAmount > 0 ? i.TotalAmount : (i.Quantity * i.UnitPrice + i.TaxAmount));
                    }

                    foreach (var cp in Contract.ContractProducts)
                    {
                        if (!cp.UnitPrice.HasValue || cp.UnitPrice == 0)
                        {
                            var matchingQItem = quote.Items.FirstOrDefault(qi => qi.ProductID == cp.ProductID);
                            if (matchingQItem != null && matchingQItem.UnitPrice > 0)
                            {
                                cp.UnitPrice = matchingQItem.UnitPrice;
                            }
                            else if (Contract.UnitPrice > 0)
                            {
                                cp.UnitPrice = Contract.UnitPrice;
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(Contract.PaymentMethod))
            {
                Contract.PaymentMethod = "Bank Transfer";
            }

            if (string.IsNullOrWhiteSpace(Contract.Unit))
            {
                Contract.Unit = "Kg";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ContractDetails] Error loading details: {ex.Message}");
            HasError = true;
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private void NavigateBack()
    {
        if (Auth.IsAdmin)
        {
            Nav.NavigateTo("/admin");
        }
        else if (Auth.IsOrgManager)
        {
            Nav.NavigateTo("/organization/contracts");
        }
        else if (Auth.IsOutletManager)
        {
            Nav.NavigateTo("/outlet-manager");
        }
        else if (Auth.IsVendorManager)
        {
            Nav.NavigateTo("/vendor-dashboard");
        }
        else
        {
            Nav.NavigateTo("/org-dashboard");
        }
    }

    // ==========================================
    // RESET CONTRACT LOGIC (FOR CAPACITY REACHED)
    // ==========================================
    private bool IsResetModalOpen { get; set; } = false;
    private bool IsResetting { get; set; } = false;
    private string? ResetErrorMessage { get; set; }

    private void PromptReset()
    {
        ResetErrorMessage = null;
        IsResetModalOpen = true;
    }

    private void CancelReset()
    {
        IsResetModalOpen = false;
        ResetErrorMessage = null;
        IsResetting = false;
    }

    private async Task ExecuteResetContractAsync()
    {
        if (Contract == null || IsResetting) return;

        IsResetting = true;
        ResetErrorMessage = null;

        try
        {
            var result = await Api.ResetContractAsync(Contract.ContractID);
            if (result != null && result.Success)
            {
                IsResetModalOpen = false;
                // Reload contract details to reflect active status and 0 used quantity
                await LoadDetails(Contract.ContractID);
            }
            else
            {
                ResetErrorMessage = result?.ErrorMessage ?? "Failed to reset contract. Please try again.";
            }
        }
        catch (Exception ex)
        {
            ResetErrorMessage = ex.Message;
        }
        finally
        {
            IsResetting = false;
            StateHasChanged();
        }
    }

    // ==========================================
    // END CONTRACT LOGIC
    // ==========================================
    private bool IsEndModalOpen { get; set; } = false;
    private bool IsEnding { get; set; } = false;
    private string? EndErrorMessage { get; set; }

    private void PromptEndContract()
    {
        EndErrorMessage = null;
        IsEndModalOpen = true;
    }

    private void CancelEndContract()
    {
        IsEndModalOpen = false;
        EndErrorMessage = null;
        IsEnding = false;
    }

    private async Task ExecuteEndContractAsync()
    {
        if (Contract == null || IsEnding) return;

        IsEnding = true;
        EndErrorMessage = null;

        try
        {
            var result = await Api.EndContractAsync(Contract.ContractID);
            if (result != null && result.Success)
            {
                IsEndModalOpen = false;
                // Reload contract details to reflect ended status
                await LoadDetails(Contract.ContractID);
            }
            else
            {
                EndErrorMessage = result?.ErrorMessage ?? "Failed to end contract. Please try again.";
            }
        }
        catch (Exception ex)
        {
            EndErrorMessage = ex.Message;
        }
        finally
        {
            IsEnding = false;
            StateHasChanged();
        }
    }
}