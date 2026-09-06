using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class CreatePurchaseOrderCommand
    {
        public int QuotationID { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; } = DateTime.Now.AddDays(3);
    }
