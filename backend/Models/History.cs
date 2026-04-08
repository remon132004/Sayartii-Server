using System.Collections.Generic;

namespace Sayartii.Api.Models
{
    public class History
    {
        public int Id { get; set; }
        public string Image { get; set; } = string.Empty;
        
        public virtual ICollection<Issue> Issues { get; set; } = new List<Issue>();
    }
}
