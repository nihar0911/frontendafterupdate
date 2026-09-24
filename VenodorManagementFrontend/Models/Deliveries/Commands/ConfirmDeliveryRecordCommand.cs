using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class ConfirmDeliveryRecordCommand
    {
        public int DeliveryRecordID { get; set; }
        public int ConfirmedByUserID { get; set; }
    }
