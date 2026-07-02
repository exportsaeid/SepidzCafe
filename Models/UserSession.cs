using System;

namespace CafeManager.Models
{
    public class UserSession
    {
        public string Username { get; set; } = "admin";
        public string Role { get; set; } = "مدیر سیستم";
        public bool IsLoggedIn { get; set; } = true;
        public DateTime LoginTime { get; set; } = DateTime.Now;
    }
}
