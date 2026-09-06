using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class CreateQuotationItemDto
    {
        public int ProductID { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
