using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sayartii.Api.Models;

namespace Sayartii.Api.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<DataFromCar> DataFromCar { get; set; }
        public DbSet<Notifications> Notifications { get; set; }
    }
}
