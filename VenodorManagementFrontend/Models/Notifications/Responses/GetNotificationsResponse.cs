using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class GetNotificationsResponse
    {
        public List<NotificationDto> Notifications { get; set; } = new();
        public int UnreadCount { get; set; }
    }
