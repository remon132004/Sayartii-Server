using System.Collections.Generic;

namespace Sayartii.Api.Models
{
    public class Car
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        
        public virtual ICollection<Issue> Issues { get; set; } = new List<Issue>();
    }
}
