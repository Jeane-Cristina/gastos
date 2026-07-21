using Microsoft.EntityFrameworkCore;
using GastosApi.Models;

namespace GastosApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Expense> Expenses { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<FinancialProfile> FinancialProfiles { get; set; }
}