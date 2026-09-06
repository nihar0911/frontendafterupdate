using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.QuotationReview;

public partial class QuotationReview : ComponentBase
{
    [Parameter]
    public int? QuotationId { get; set; }

    [SupplyParameterFromQuery(Name = "quotationId")]
    public int? QueryQuotationId { get; set; }

    private bool IsLoading { get; set; } = true;
    private bool IsProcessing { get; set; } = false;
    private bool IsSidebarCollapsed { get; set; } = false;
    private bool IsProfileDropdownOpen { get; set; } = false;

    private string? ErrorMessage { get; set; }
    private string? SuccessMessage { get; set; }

    private QuotationDto? Quotation { get; set; }
    private PurchaseRequestDto? PurchaseRequest { get; set; }
    private VendorDto? VendorInfo { get; set; }
    private ContractDto? ExistingContract { get; set; }

    private string OutletName { get; set; } = "Outlet";
    private string OutletAddress { get; set; } = string.Empty;
    private string ProductName { get; set; } = string.Empty;
    private string Unit { get; set; } = "Kg";
    private decimal RequestedQuantity { get; set; }
    private decimal UnitPrice { get; set; }
    private decimal TaxRate { get; set; }
    private decimal TaxAmount { get; set; }
    private decimal TotalAmount { get; set; }
    private string VendorName { get; set; } = string.Empty;
    private Dictionary<int, string> ProductNames { get; set; } = new();

    private string? ConfirmAction { get; set; }

    private bool IsExpired => Quotation != null && Quotation.ValidUntil < DateTime.Now;
    private bool IsActionable => Quotation != null && (string.Equals(Quotation.Status, "Submitted", StringComparison.OrdinalIgnoreCase) || string.Equals(Quotation.Status, "Pending", StringComparison.OrdinalIgnoreCase));
    private bool IsAccepted => Quotation != null && string.Equals(Quotation.Status, "Accepted", StringComparison.OrdinalIgnoreCase);
    private bool IsRejected => Quotation != null && string.Equals(Quotation.Status, "Rejected", StringComparison.OrdinalIgnoreCase);

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
        return Auth.IsPurchaseManager ? "P" : "G";
    }

    protected override async Task OnInitializedAsync()
    {
        int targetId = QuotationId ?? QueryQuotationId ?? 0;
        if (targetId > 0)
        {
            await LoadQuotationDetails(targetId);
        }
        else
        {
            IsLoading = false;
            ErrorMessage = "No valid Quotation ID was specified.";
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        int targetId = QuotationId ?? QueryQuotationId ?? 0;
        if (targetId > 0 && (Quotation == null || Quotation.QuotationID != targetId))
        {
            await LoadQuotationDetails(targetId);
        }
    }

    private async Task LoadQuotationDetails(int id)
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            Quotation = await Api.GetQuotationByIdAsync(id);
            if (Quotation == null)
            {
                ErrorMessage = $"Quotation #{id} could not be loaded from the server.";
                return;
            }

            var productsTask = Api.GetProductsAsync();
            var requestsTask = Api.GetPurchaseRequestsAsync();
            var vendorsTask = Api.GetVendorsAsync();
            var outletsTask = Api.GetOutletsAsync();
            var contractsTask = Api.GetContractsAsync();

            await Task.WhenAll(productsTask, requestsTask, vendorsTask, outletsTask, contractsTask);

            var products = await productsTask;
            if (products != null)
            {
                ProductNames = products.ToDictionary(p => p.ProductID, p => p.ProductName);
            }

            if (Quotation.Items != null && Quotation.Items.Count > 0)
            {
                var item = Quotation.Items.First();
                UnitPrice = item.UnitPrice;
                TaxRate = item.TaxRate;
                TaxAmount = item.TaxAmount;
                TotalAmount = Quotation.Items.Sum(i => i.TotalAmount > 0 ? i.TotalAmount : (i.Quantity * i.UnitPrice + i.TaxAmount));
                RequestedQuantity = item.Quantity;

                if (ProductNames.TryGetValue(item.ProductID, out var pName))
                {
                    ProductName = pName;
                }
                else
                {
                    ProductName = $"Product #{item.ProductID}";
                }

                var matchedProd = products?.FirstOrDefault(pr => pr.ProductID == item.ProductID);
                if (matchedProd != null && !string.IsNullOrWhiteSpace(matchedProd.Unit))
                {
                    Unit = matchedProd.Unit;
                }
            }

            var requests = await requestsTask;
            PurchaseRequest = requests?.FirstOrDefault(pr => pr.RequestID == Quotation.RequestID);
            if (PurchaseRequest != null)
            {
                if (Auth.IsPurchaseManager && Auth.OutletID.HasValue && PurchaseRequest.OutletID != Auth.OutletID.Value)
                {
                    ErrorMessage = "You are not authorized to view quotations belonging to another outlet.";
                    Quotation = null;
                    return;
                }

                var outlets = await outletsTask;
                var o = outlets?.FirstOrDefault(outl => outl.OutletID == PurchaseRequest.OutletID);
                if (o != null)
                {
                    OutletName = o.OutletName;
                    OutletAddress = !string.IsNullOrWhiteSpace(o.Address) ? o.Address : o.OutletName;
                }
                else
                {
                    OutletName = $"Outlet #{PurchaseRequest.OutletID}";
                }

                if (PurchaseRequest.Items != null && PurchaseRequest.Items.Count > 0)
                {
                    var prItem = PurchaseRequest.Items.First();
                    if (RequestedQuantity == 0) RequestedQuantity = prItem.Quantity;
                    if (string.IsNullOrEmpty(Unit) || Unit == "Units") Unit = prItem.Unit ?? "Kg";
                    if (string.IsNullOrEmpty(ProductName)) ProductName = prItem.ProductName;
                }
            }

            var vendors = await vendorsTask;
            VendorInfo = vendors?.FirstOrDefault(v => v.VendorID == Quotation.VendorID);
            VendorName = VendorInfo?.VendorName ?? $"Vendor #{Quotation.VendorID}";

            var contracts = await contractsTask;
            ExistingContract = contracts?.FirstOrDefault(c => c.QuotationID == Quotation.QuotationID && string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[QuotationReview] Error: {ex.Message}");
            ErrorMessage = "An error occurred while fetching quotation details.";
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private string GetProductName(int productId)
    {
        if (ProductNames.TryGetValue(productId, out var name) && !string.IsNullOrWhiteSpace(name)) return name;
        return $"Product #{productId}";
    }

    private void ShowConfirmation(string action)
    {
        ConfirmAction = action;
        ErrorMessage = null;
        StateHasChanged();
    }

    private async Task ProcessResponse()
    {
        if (Quotation == null || string.IsNullOrEmpty(ConfirmAction)) return;

        IsProcessing = true;
        ErrorMessage = null;
        SuccessMessage = null;
        StateHasChanged();

        try
        {
            QuotationDto? updated = null;
            if (ConfirmAction == "Accepted")
            {
                updated = await Api.AcceptQuotationAsync(Quotation.QuotationID);
            }
            else if (ConfirmAction == "Rejected")
            {
                updated = await Api.RejectQuotationAsync(Quotation.QuotationID);
            }

            if (updated == null)
            {
                updated = await Api.RespondToQuotationAsync(new RespondToQuotationCommand
                {
                    QuotationID = Quotation.QuotationID,
                    Status = ConfirmAction
                });
            }

            if (updated != null)
            {
                Quotation = updated;
                SuccessMessage = ConfirmAction == "Accepted" 
                    ? (Auth.IsPurchaseManager ? "Quotation Accepted Successfully! The purchase request is approved and ready for purchase order processing." : "Quotation Accepted Successfully! You can now create the contract.") 
                    : "Quotation Rejected Successfully!";
                ConfirmAction = null;
                await LoadQuotationDetails(Quotation.QuotationID);
            }
            else
            {
                ErrorMessage = $"Failed to {ConfirmAction.ToLower()} quotation. It may have already been processed or expired.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[QuotationReview] Action error: {ex.Message}");
            ErrorMessage = "An error occurred while sending response to the server.";
        }
        finally
        {
            IsProcessing = false;
            StateHasChanged();
        }
    }
}