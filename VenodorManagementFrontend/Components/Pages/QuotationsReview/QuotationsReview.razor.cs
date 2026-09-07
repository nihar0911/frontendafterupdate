using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.QuotationsReview;

public partial class QuotationsReview : ComponentBase
{
    private List<QuotationDto>? Quotations { get; set; }
    private Dictionary<int, string> VendorNames { get; set; } = new();
    private Dictionary<int, VendorDto> VendorsDict { get; set; } = new();
    private Dictionary<int, string> ProductNames { get; set; } = new();
    private Dictionary<int, string> OutletNames { get; set; } = new();
    private Dictionary<int, PurchaseRequestDto> RequestDict { get; set; } = new();

    private bool IsSidebarCollapsed { get; set; } = false;
    private bool IsProfileDropdownOpen { get; set; } = false;

    private string SearchQuery { get; set; } = string.Empty;
    private string StatusFilter { get; set; } = "All";
    private string SortBy { get; set; } = "Latest First";

    private QuotationDto? SelectedQuotation { get; set; }
    private QuotationDto? ModalQuotation { get; set; }
    private string? ConfirmationAction { get; set; }

    private bool IsLoading { get; set; } = true;
    private bool IsProcessing { get; set; } = false;

    private string? ErrorMessage { get; set; }
    private string? SuccessMessage { get; set; }
    private QuotationDto? RespondedQuotation { get; set; }

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
        return Auth.IsPurchaseManager ? "P" : "O";
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    protected override async Task OnParametersSetAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var quotationsTask = Api.GetQuotationsAsync();
            var vendorsTask = Api.GetVendorsAsync();
            var productsTask = Api.GetProductsAsync();
            var outletsTask = Api.GetOutletsAsync();
            var requestsTask = Api.GetPurchaseRequestsAsync();

            await Task.WhenAll(quotationsTask, vendorsTask, productsTask, outletsTask, requestsTask);

            var allQuotations = await quotationsTask ?? new List<QuotationDto>();
            var outlets = await outletsTask ?? new List<OutletDto>();
            var requests = await requestsTask ?? new List<PurchaseRequestDto>();

            OutletNames = outlets.ToDictionary(o => o.OutletID, o => string.IsNullOrWhiteSpace(o.OutletName) 
                ? (!string.IsNullOrWhiteSpace(o.Address) ? $"{o.Address.Split(',')[0].Trim()} Outlet" : $"Outlet #{o.OutletID}") 
                : o.OutletName);

            RequestDict = requests.ToDictionary(r => r.RequestID, r => r);

            int userOrgId = Auth.OrganizationID ?? 0;
            bool isScopedToOrg = Auth.IsOrgManager && userOrgId > 0;
            var orgOutletIDs = isScopedToOrg
                ? outlets.Where(o => o.OrganizationID == userOrgId).Select(o => o.OutletID).ToHashSet()
                : new HashSet<int>();

            if (Auth.IsPurchaseManager || Auth.IsOutletManager)
            {
                if (Auth.OutletID.HasValue && Auth.OutletID.Value > 0)
                {
                    int outletId = Auth.OutletID.Value;
                    Quotations = allQuotations
                        .Where(q => RequestDict.TryGetValue(q.RequestID, out var pr) && pr.OutletID == outletId)
                        .OrderByDescending(q => q.QuotationID)
                        .ToList();
                }
                else
                {
                    Quotations = new List<QuotationDto>();
                    ErrorMessage = "No assigned outlet found for this account.";
                }
            }
            else if (isScopedToOrg)
            {
                Quotations = orgOutletIDs.Count > 0
                    ? allQuotations.Where(q => RequestDict.TryGetValue(q.RequestID, out var pr) && orgOutletIDs.Contains(pr.OutletID))
                        .OrderByDescending(q => q.QuotationID).ToList()
                    : new List<QuotationDto>();
            }
            else
            {
                Quotations = allQuotations.OrderByDescending(q => q.QuotationID).ToList();
            }

            var vendors = await vendorsTask;
            if (vendors != null)
            {
                VendorNames = vendors.ToDictionary(v => v.VendorID, v => v.VendorName);
                VendorsDict = vendors.ToDictionary(v => v.VendorID, v => v);
            }

            var products = await productsTask;
            if (products != null)
            {
                ProductNames = products.ToDictionary(p => p.ProductID, p => p.ProductName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[QuotationsReview] Error loading quotations: {ex.Message}");
            ErrorMessage = "Unable to load quotations from the server.";
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private IEnumerable<QuotationDto> FilteredQuotations
    {
        get
        {
            if (Quotations == null) return Enumerable.Empty<QuotationDto>();

            var list = Quotations.AsEnumerable();

            // Status filter
            if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
            {
                if (string.Equals(StatusFilter, "Submitted", StringComparison.OrdinalIgnoreCase))
                {
                    list = list.Where(q => (string.Equals(q.Status, "Submitted", StringComparison.OrdinalIgnoreCase) || string.Equals(q.Status, "Pending", StringComparison.OrdinalIgnoreCase)) && q.ValidUntil >= DateTime.Now);
                }
                else if (string.Equals(StatusFilter, "Accepted", StringComparison.OrdinalIgnoreCase))
                {
                    list = list.Where(q => string.Equals(q.Status, "Accepted", StringComparison.OrdinalIgnoreCase));
                }
                else if (string.Equals(StatusFilter, "Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    list = list.Where(q => string.Equals(q.Status, "Rejected", StringComparison.OrdinalIgnoreCase));
                }
                else if (string.Equals(StatusFilter, "Expired", StringComparison.OrdinalIgnoreCase))
                {
                    list = list.Where(q => q.ValidUntil < DateTime.Now && !string.Equals(q.Status, "Accepted", StringComparison.OrdinalIgnoreCase));
                }
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                string q = SearchQuery.Trim().ToLowerInvariant();
                list = list.Where(item =>
                    item.QuotationID.ToString().Contains(q) ||
                    $"quotation {item.QuotationID}".ToLowerInvariant().Contains(q) ||
                    item.RequestID.ToString().Contains(q) ||
                    $"pr-{item.RequestID}".ToLowerInvariant().Contains(q) ||
                    GetVendorName(item.VendorID).ToLowerInvariant().Contains(q) ||
                    GetOutletNameForRequest(item.RequestID).ToLowerInvariant().Contains(q) ||
                    (item.Items != null && item.Items.Any(i => GetProductName(i.ProductID).ToLowerInvariant().Contains(q)))
                );
            }

            // Sorting
            list = SortBy switch
            {
                "Oldest First" => list.OrderBy(q => q.QuotationID),
                "Total: High to Low" => list.OrderByDescending(q => GetQuotationTotal(q)),
                "Total: Low to High" => list.OrderBy(q => GetQuotationTotal(q)),
                _ => list.OrderByDescending(q => q.QuotationID)
            };

            return list;
        }
    }

    private string GetVendorName(int vendorId)
    {
        if (VendorNames.TryGetValue(vendorId, out var name) && !string.IsNullOrWhiteSpace(name)) return name;
        return $"Vendor #{vendorId}";
    }

    private VendorDto? GetVendorDetails(int vendorId)
    {
        if (VendorsDict.TryGetValue(vendorId, out var v)) return v;
        return null;
    }

    private string GetProductName(int productId)
    {
        if (ProductNames.TryGetValue(productId, out var name) && !string.IsNullOrWhiteSpace(name)) return name;
        return $"Product #{productId}";
    }

    private string GetOutletNameForRequest(int requestId)
    {
        if (RequestDict.TryGetValue(requestId, out var pr) && OutletNames.TryGetValue(pr.OutletID, out var oName))
        {
            return oName;
        }
        return "N/A";
    }

    private string GetFirstProductNameForRequest(int requestId)
    {
        if (RequestDict.TryGetValue(requestId, out var pr) && pr.Items != null && pr.Items.Count > 0)
        {
            var firstItem = pr.Items[0];
            if (!string.IsNullOrWhiteSpace(firstItem.ProductName)) return firstItem.ProductName;
            return GetProductName(firstItem.ProductID);
        }
        return "General Produce";
    }

    private string GetRequestedQuantityForRequest(int requestId)
    {
        if (RequestDict.TryGetValue(requestId, out var pr) && pr.Items != null && pr.Items.Count > 0)
        {
            var firstItem = pr.Items[0];
            string unit = !string.IsNullOrWhiteSpace(firstItem.Unit) ? firstItem.Unit : "Kg";
            return $"{firstItem.Quantity:N2} {unit}";
        }
        return "—";
    }

    private string GetRemainingDaysText(DateTime validUntil)
    {
        var diff = (validUntil.Date - DateTime.Now.Date).TotalDays;
        if (diff < 0) return "Expired";
        if (diff == 0) return "Expires today";
        if (diff == 1) return "1 day remaining";
        return $"{diff} days remaining";
    }

    private decimal GetQuotationTotal(QuotationDto q)
    {
        if (q.Items == null || q.Items.Count == 0) return 0;
        return q.Items.Sum(i => i.TotalAmount > 0 ? i.TotalAmount : (i.Quantity * i.UnitPrice));
    }

    private void OpenDetailsModal(QuotationDto q)
    {
        ModalQuotation = q;
        StateHasChanged();
    }

    private void CloseDetailsModal()
    {
        ModalQuotation = null;
        StateHasChanged();
    }

    private void OpenConfirmation(QuotationDto q, string action)
    {
        if (!Auth.IsPurchaseManager && !Auth.IsAdmin) return;
        if (q.ValidUntil < DateTime.Now && action == "Accepted")
        {
            ErrorMessage = "This quotation has expired and cannot be accepted because its validity period has ended.";
            StateHasChanged();
            return;
        }

        SelectedQuotation = q;
        ConfirmationAction = action;
        ErrorMessage = null;
        StateHasChanged();
    }

    private void CancelConfirmation()
    {
        SelectedQuotation = null;
        ConfirmationAction = null;
        StateHasChanged();
    }

    private async Task ExecuteResponse()
    {
        if (!Auth.IsPurchaseManager && !Auth.IsAdmin) return;
        if (SelectedQuotation == null || string.IsNullOrEmpty(ConfirmationAction)) return;

        IsProcessing = true;
        ErrorMessage = null;
        SuccessMessage = null;
        RespondedQuotation = null;
        StateHasChanged();

        try
        {
            var command = new RespondToQuotationCommand
            {
                QuotationID = SelectedQuotation.QuotationID,
                Status = ConfirmationAction
            };

            var updatedQuotation = await Api.RespondToQuotationAsync(command);
            if (updatedQuotation != null)
            {
                RespondedQuotation = updatedQuotation;
                SuccessMessage = ConfirmationAction == "Accepted" ? "QUOTATION ACCEPTED" : "QUOTATION REJECTED";
                SelectedQuotation = null;
                ConfirmationAction = null;
                ModalQuotation = null;

                await LoadData();
            }
            else
            {
                ErrorMessage = "Unable to process quotation response. This quotation may have already been responded to or expired.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[QuotationsReview] Error responding to quotation: {ex.Message}");
            ErrorMessage = "An error occurred while communicating with the server.";
        }
        finally
        {
            IsProcessing = false;
            StateHasChanged();
        }
    }
}