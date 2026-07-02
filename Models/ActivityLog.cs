using System;

namespace CafeManager.Models
{
    public class ActivityLog
    {
        public string Username { get; set; }     // چه کسی؟
        public string Action { get; set; }       // چه کاری انجام داده؟
        public DateTime Timestamp { get; set; }  // در چه زمانی؟
    }
}