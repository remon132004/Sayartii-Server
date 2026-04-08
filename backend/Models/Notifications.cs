using System;

namespace Sayartii.Api.Models
{
    public class Notifications
    {
        public int Id { get; set; }
        public string User_id { get; set; } = string.Empty;
        public string Notification { get; set; } = string.Empty;
    }

    public class NotificationsDto
    {
        public string Notification { get; set; } = string.Empty;
    }
}
