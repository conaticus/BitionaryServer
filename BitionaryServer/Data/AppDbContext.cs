using BitionaryServer.Models;
using Microsoft.EntityFrameworkCore;

namespace BitionaryServer.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
}