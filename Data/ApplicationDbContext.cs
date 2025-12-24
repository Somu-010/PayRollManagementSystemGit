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
        public DbSet<Designation> Designations { get; set; }
        public DbSet<Shift> Shifts { get; set; }

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

                // Configure relationship with Shift
                entity.HasOne(e => e.ShiftNavigation)
                    .WithMany(s => s.Employees)
                    .HasForeignKey(e => e.ShiftId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Department entity
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(d => d.DepartmentId);
                entity.HasIndex(d => d.DepartmentCode).IsUnique();
                entity.HasIndex(d => d.Name).IsUnique();
            });

            // Configure Designation entity
            modelBuilder.Entity<Designation>(entity =>
            {
                entity.HasKey(d => d.DesignationId);
                entity.HasIndex(d => d.DesignationCode).IsUnique();

                // Configure relationship with Department
                entity.HasOne(d => d.Department)
                    .WithMany()
                    .HasForeignKey(d => d.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Shift entity
            modelBuilder.Entity<Shift>(entity =>
            {
                entity.HasKey(s => s.ShiftId);
                entity.HasIndex(s => s.ShiftCode).IsUnique();
                entity.HasIndex(s => s.ShiftName).IsUnique();
            });
        }
    }
}
