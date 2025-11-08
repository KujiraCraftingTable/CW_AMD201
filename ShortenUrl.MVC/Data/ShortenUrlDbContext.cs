using Microsoft.EntityFrameworkCore;
using shortenUrl.MVC.Data.Entities;

namespace shortenUrl.MVC.Data
{
    public class ShortenUrlDbContext : DbContext
    {
        public ShortenUrlDbContext(DbContextOptions<ShortenUrlDbContext> options) 
            : base(options)
        {   
        }
        public DbSet<Url> Urls { get; set; }
    }
}
