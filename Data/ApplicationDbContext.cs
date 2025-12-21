using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Models;

namespace PayRollManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Department> Departments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Employee entity
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.EmployeeId);
                entity.HasIndex(e => e.EmployeeCode).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();

                // Configure relationship with Department
                entity.HasOne(e => e.DepartmentNavigation)
                    .WithMany(d => d.Employees)
                    .HasForeignKey(e => e.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Department entity
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(d => d.DepartmentId);
                entity.HasIndex(d => d.DepartmentCode).IsUnique();
                entity.HasIndex(d => d.Name).IsUnique();
            });
        }
    }
}