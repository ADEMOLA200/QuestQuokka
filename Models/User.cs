using Microsoft.EntityFrameworkCore;
using System;

public class DatabaseContext : DbContext
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasKey(u => u.UserId);
    }
}

public class User
{
    public ulong UserId { get; set; }
    public int Score { get; set; } = 0;
    public DateTime LastDaily { get; set; } = DateTime.MinValue;
}
