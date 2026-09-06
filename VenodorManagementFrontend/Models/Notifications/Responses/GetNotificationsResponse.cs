using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class GetNotificationsResponse
    {
        public List<NotificationDto> Notifications { get; set; } = new();
        public int UnreadCount { get; set; }
    }
