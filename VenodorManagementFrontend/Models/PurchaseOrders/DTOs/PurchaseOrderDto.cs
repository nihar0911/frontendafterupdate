using System;
using System.Collections.Generic;
using System.Linq;

namespace VenodorManagementFrontend.Models;

public class PurchaseOrderDto
{
    private decimal _totalAmount;

    public int PurchaseOrderID { get; set; }
    public int RequestID { get; set; }
    public int QuotationID { get; set; }
    public int VendorID { get; set; }
    public int OutletID { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? DispatchDateTime { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public string? DeliveryStatus { get; set; }
    public decimal TotalAmount
    {
        get
        {
            if (_totalAmount > 0)
            {
                return _totalAmount;
            }

            if (Items == null || Items.Count == 0)
            {
                return _totalAmount;
            }

            return Items.Sum(item =>
                item.TotalAmount > 0
                    ? item.TotalAmount
                    : (item.Subtotal + item.TaxAmount));
        }
        set => _totalAmount = value;
    }
    public string VendorName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ApproverRole { get; set; }
    public List<PurchaseOrderItemDto> Items { get; set; } = new();
}
