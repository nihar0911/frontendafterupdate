using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class CreateDeliveryRecordCommand
    {
        public int PurchaseOrderID { get; set; }
        public int POItemID { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal SpoiledQuantity { get; set; }
        public DateTime? DeliveryDate { get; set; }
    }
