namespace VenodorManagementFrontend.Models.Invoices.Requests;

public class CreateInvoiceRequest
{
    public int PurchaseOrderID { get; set; }
    public int VendorID { get; set; }
}