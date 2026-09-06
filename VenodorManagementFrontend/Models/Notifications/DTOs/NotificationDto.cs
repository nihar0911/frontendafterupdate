using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class NotificationDto
    {
        public int NotificationID { get; set; }
        public int UserID { get; set; }
        public int? RelatedRequestID { get; set; }
        public int? RelatedVendorID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string NotificationType { get; set; } = "General";
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
    }
