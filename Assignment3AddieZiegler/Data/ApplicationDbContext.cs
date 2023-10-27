using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Assignment3AddieZiegler.Models;

namespace Assignment3AddieZiegler.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Assignment3AddieZiegler.Models.Movie>? Movie { get; set; }
        public DbSet<Assignment3AddieZiegler.Models.Actor>? Actor { get; set; }

    }
}