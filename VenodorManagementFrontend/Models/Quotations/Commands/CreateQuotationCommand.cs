using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class CreateQuotationCommand
    {
        public int RequestID { get; set; }
        public int VendorID { get; set; }
        public DateTime ValidUntil { get; set; } = DateTime.Now.AddDays(7);
        public string Status { get; set; } = "Submitted";
        public List<CreateQuotationItemDto> Items { get; set; } = new();
    }
