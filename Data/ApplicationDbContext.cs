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
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Leave> Leaves { get; set; }
        public DbSet<LeaveBalance> LeaveBalances { get; set; }
        public DbSet<AllowanceDeduction> AllowanceDeductions { get; set; }
        public DbSet<ComponentTemplate> ComponentTemplates { get; set; }
        public DbSet<ComponentTemplateItem> ComponentTemplateItems { get; set; }
        public DbSet<Payroll> Payrolls { get; set; }
        public DbSet<PayrollDetail> PayrollDetails { get; set; }

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
            // Configure Leave entity
            modelBuilder.Entity<Leave>(entity =>
            {
                entity.HasKey(l => l.LeaveId);

                // Configure relationship with Employee
                entity.HasOne(l => l.Employee)
                    .WithMany()
                    .HasForeignKey(l => l.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure LeaveBalance entity
            modelBuilder.Entity<LeaveBalance>(entity =>
            {
                entity.HasKey(lb => lb.LeaveBalanceId);

                // Configure relationship with Employee
                entity.HasOne(lb => lb.Employee)
                    .WithMany()
                    .HasForeignKey(lb => lb.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Create unique index on EmployeeId and Year
                entity.HasIndex(lb => new { lb.EmployeeId, lb.Year }).IsUnique();
            });
            // Configure AllowanceDeduction entity
            modelBuilder.Entity<AllowanceDeduction>(entity =>
            {
                entity.HasKey(a => a.AllowanceDeductionId);
                entity.HasIndex(a => a.Code).IsUnique();
            });
            // Configure ComponentTemplate
            modelBuilder.Entity<ComponentTemplate>(entity =>
            {
                entity.HasKey(t => t.TemplateId);
                entity.HasIndex(t => t.Name);
            });

            // Configure ComponentTemplateItem
            modelBuilder.Entity<ComponentTemplateItem>(entity =>
            {
                entity.HasKey(ti => ti.TemplateItemId);

                entity.HasOne(ti => ti.Template)
                    .WithMany(t => t.TemplateItems)
                    .HasForeignKey(ti => ti.TemplateId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ti => ti.Component)
                    .WithMany()
                    .HasForeignKey(ti => ti.AllowanceDeductionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Payroll entity
            modelBuilder.Entity<Payroll>(entity =>
            {
                entity.HasKey(p => p.PayrollId);
                entity.HasIndex(p => p.PayrollNumber).IsUnique();
                entity.HasIndex(p => new { p.EmployeeId, p.Month, p.Year }).IsUnique();

                entity.HasOne(p => p.Employee)
                    .WithMany()
                    .HasForeignKey(p => p.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure PayrollDetail entity
            modelBuilder.Entity<PayrollDetail>(entity =>
            {
                entity.HasKey(pd => pd.PayrollDetailId);

                entity.HasOne(pd => pd.Payroll)
                    .WithMany(p => p.PayrollDetails)
                    .HasForeignKey(pd => pd.PayrollId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pd => pd.AllowanceDeduction)
                    .WithMany()
                    .HasForeignKey(pd => pd.AllowanceDeductionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
