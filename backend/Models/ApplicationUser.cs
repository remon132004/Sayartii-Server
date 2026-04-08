using System;
using Microsoft.AspNetCore.Identity;

namespace Sayartii.Api.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; } = string.Empty;
        public DateTime RegisterDate { get; set; } = DateTime.UtcNow;
    }
}
