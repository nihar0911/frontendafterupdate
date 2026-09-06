using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class QuotationItemDto
    {
        public int QuotationItemID { get; set; }
        public int ProductID { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }
