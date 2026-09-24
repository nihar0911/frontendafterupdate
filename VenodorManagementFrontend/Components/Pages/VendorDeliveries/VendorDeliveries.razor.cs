using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VendorManagement.Web.Models.Deliveries.DTOs;
using VendorManagement.Web.Models.Deliveries.Responses;
using VendorManagement.Web.Services;

namespace VendorManagement.Web.Components.Pages.VendorDeliveries;

public partial class VendorDeliveries : ComponentBase
{
    private List<VendorDeliveryItemDto> AllDeliveries { get; set; } = new();
    private bool IsLoading { get; set; } = true;
    private string? ErrorMessage { get; set; }
    private int? ExpandedRecordId { get; set; }

    // Search and Filter fields
    private string SearchQuery { get; set; } = string.Empty;
    private string SelectedProductFilter { get; set; } = "All";
    private string SelectedOutletFilter { get; set; } = "All";
    private string SelectedStatusFilter { get; set; } = "All";
    private DateTime? StartDateFilter { get; set; }
    private DateTime? EndDateFilter { get; set; }

    private string DisplayName => !string.IsNullOrWhiteSpace(Auth.UserName) ? Auth.UserName : "Vendor Manager";

    protected override async Task OnInitializedAsync()
    {
        Auth.OnAuthStateChanged += OnAuthStateChanged;
        if (Auth.IsAuthenticated)
        {
            await LoadDeliveriesAsync();
        }
        else
        {
            IsLoading = false;
        }
    }

    private void OnAuthStateChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    private async Task LoadDeliveriesAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var response = await Api.GetMyDeliveriesAsync();
            if (response != null)
            {
                AllDeliveries = response.Deliveries ?? new List<VendorDeliveryItemDto>();
            }
            else
            {
                AllDeliveries = new List<VendorDeliveryItemDto>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VendorDeliveries] Error loading deliveries: {ex.Message}");
            ErrorMessage = "Failed to load delivery history. Please verify network connectivity and try again.";
            AllDeliveries = new List<VendorDeliveryItemDto>();
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private void ToggleDetails(int deliveryRecordId)
    {
        if (ExpandedRecordId == deliveryRecordId)
        {
            ExpandedRecordId = null;
        }
        else
        {
            ExpandedRecordId = deliveryRecordId;
        }
    }

    // Filter Options
    private List<string> AllProductNames => AllDeliveries
        .Select(d => d.ProductName?.Trim())
        .Where(n => !string.IsNullOrWhiteSpace(n))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(n => n)
        .ToList()!;

    private List<string> AllOutletNames => AllDeliveries
        .Select(d => d.OutletName?.Trim())
        .Where(n => !string.IsNullOrWhiteSpace(n))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(n => n)
        .ToList()!;

    private List<string> AllStatuses => AllDeliveries
        .Select(d => string.IsNullOrWhiteSpace(d.DeliveryStatus) ? d.Status : d.DeliveryStatus)
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(s => s)
        .ToList();

    private bool IsFiltered =>
        !string.IsNullOrWhiteSpace(SearchQuery) ||
        SelectedProductFilter != "All" ||
        SelectedOutletFilter != "All" ||
        SelectedStatusFilter != "All" ||
        StartDateFilter.HasValue ||
        EndDateFilter.HasValue;

    private List<VendorDeliveryItemDto> FilteredDeliveries
    {
        get
        {
            var query = AllDeliveries.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var q = SearchQuery.Trim();
                query = query.Where(d =>
                    (!string.IsNullOrEmpty(d.PONumber) && d.PONumber.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    d.PurchaseOrderID.ToString().Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(d.ProductName) && d.ProductName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(d.OutletName) && d.OutletName.Contains(q, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (SelectedProductFilter != "All")
            {
                query = query.Where(d => string.Equals(d.ProductName, SelectedProductFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (SelectedOutletFilter != "All")
            {
                query = query.Where(d => string.Equals(d.OutletName, SelectedOutletFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (SelectedStatusFilter != "All")
            {
                query = query.Where(d =>
                    string.Equals(d.DeliveryStatus, SelectedStatusFilter, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(d.Status, SelectedStatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (StartDateFilter.HasValue)
            {
                var start = StartDateFilter.Value.Date;
                query = query.Where(d => d.DeliveryDate.Date >= start);
            }

            if (EndDateFilter.HasValue)
            {
                var end = EndDateFilter.Value.Date;
                query = query.Where(d => d.DeliveryDate.Date <= end);
            }

            return query.OrderByDescending(d => d.DeliveryDate).ThenByDescending(d => d.DeliveryRecordID).ToList();
        }
    }

    private void ResetFilters()
    {
        SearchQuery = string.Empty;
        SelectedProductFilter = "All";
        SelectedOutletFilter = "All";
        SelectedStatusFilter = "All";
        StartDateFilter = null;
        EndDateFilter = null;
        StateHasChanged();
    }

    private int TotalDeliveriesCount => AllDeliveries.Count;

    private int DistinctProductsCount => AllDeliveries
        .Select(d => d.ProductID)
        .Where(id => id > 0)
        .Distinct()
        .Count();

    private List<string> DistinctUnits => AllDeliveries
        .Select(d => d.Unit?.Trim())
        .Where(u => !string.IsNullOrWhiteSpace(u))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList()!;

    private string TotalReceivedDisplay
    {
        get
        {
            if (AllDeliveries.Count == 0) return "0";

            var units = DistinctUnits;
            if (units.Count == 1)
            {
                decimal total = AllDeliveries.Sum(d => d.ReceivedQuantity);
                return $"{total:0.##} {units[0]}";
            }
            else if (units.Count == 0)
            {
                decimal total = AllDeliveries.Sum(d => d.ReceivedQuantity);
                return $"{total:0.##} units";
            }
            else
            {
                // Incompatible mixed units: do not perform arithmetic addition across different units
                return $"{AllDeliveries.Count} Items";
            }
        }
    }

    private string ReceivedSubtext
    {
        get
        {
            var units = DistinctUnits;
            if (units.Count > 1)
            {
                var breakdown = string.Join(", ", units.Select(u => $"{AllDeliveries.Where(d => string.Equals(d.Unit, u, StringComparison.OrdinalIgnoreCase)).Sum(d => d.ReceivedQuantity):0.##} {u}"));
                return breakdown;
            }
            return "Total confirmed received";
        }
    }

    private string TotalSpoiledDisplay
    {
        get
        {
            if (AllDeliveries.Count == 0) return "0";

            var units = DistinctUnits;
            if (units.Count == 1)
            {
                decimal total = AllDeliveries.Sum(d => d.SpoiledQuantity);
                return $"{total:0.##} {units[0]}";
            }
            else if (units.Count == 0)
            {
                decimal total = AllDeliveries.Sum(d => d.SpoiledQuantity);
                return $"{total:0.##} units";
            }
            else
            {
                // Incompatible mixed units: display item count with spoilage
                int itemsWithSpoilage = AllDeliveries.Count(d => d.SpoiledQuantity > 0);
                return $"{itemsWithSpoilage} Items";
            }
        }
    }

    private string SpoiledSubtext
    {
        get
        {
            var units = DistinctUnits;
            if (units.Count > 1)
            {
                var breakdown = string.Join(", ", units.Where(u => AllDeliveries.Any(d => string.Equals(d.Unit, u, StringComparison.OrdinalIgnoreCase) && d.SpoiledQuantity > 0))
                    .Select(u => $"{AllDeliveries.Where(d => string.Equals(d.Unit, u, StringComparison.OrdinalIgnoreCase)).Sum(d => d.SpoiledQuantity):0.##} {u}"));

                if (string.IsNullOrEmpty(breakdown)) return "0 spoiled across all units";
                return breakdown;
            }
            return "Total recorded spoilage";
        }
    }

    private void HandleLogout()
    {
        Auth.Logout();
        Nav.NavigateTo("/login");
    }
}
