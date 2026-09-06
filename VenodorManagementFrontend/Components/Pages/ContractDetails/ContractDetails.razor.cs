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

    private decimal RemainingQuantity => (Contract?.TotalQuantity ?? 0) - (Contract?.UsedQuantity ?? 0);
    private decimal ComputedTotalValue => (Contract?.TotalAmount > 0) ? Contract.TotalAmount : ((Contract?.TotalQuantity ?? 0) * (Contract?.UnitPrice ?? 0) + (Contract?.TaxAmount ?? 0));

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

            // If unit price / financials are 0 and QuotationID exists, enrich from quotation
            if (Contract.UnitPrice == 0 && Contract.QuotationID.HasValue && Contract.QuotationID.Value > 0)
            {
                var quote = await Api.GetQuotationByIdAsync(Contract.QuotationID.Value);
                if (quote?.Items != null && quote.Items.Count > 0)
                {
                    var qItem = quote.Items.First();
                    Contract.UnitPrice = qItem.UnitPrice;
                    Contract.TaxAmount = qItem.TaxAmount;
                    Contract.TotalAmount = quote.Items.Sum(i => i.TotalAmount > 0 ? i.TotalAmount : (i.Quantity * i.UnitPrice + i.TaxAmount));
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
            Nav.NavigateTo("/admin/contracts");
        }
        else if (Auth.IsOrgManager)
        {
            Nav.NavigateTo("/admin/contracts");
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
}