using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayRollManagementSystem.Models
{
    public class Payroll
    {
        [Key]
        public int PayrollId { get; set; }

        [Required]
        [Display(Name = "Payroll Number")]
        [StringLength(50)]
        public string PayrollNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Employee")]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        [Required]
        [Display(Name = "Pay Period Month")]
        [Range(1, 12)]
        public int Month { get; set; }

        [Required]
        [Display(Name = "Pay Period Year")]
        public int Year { get; set; }

        [Required]
        [Display(Name = "Payment Date")]
        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; }

        // Salary Components
        [Required]
        [Display(Name = "Basic Salary")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasicSalary { get; set; }

        [Display(Name = "Total Allowances")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAllowances { get; set; }

        [Display(Name = "Total Deductions")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalDeductions { get; set; }

        [Display(Name = "Gross Salary")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal GrossSalary { get; set; }

        [Display(Name = "Net Salary")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal NetSalary { get; set; }

        // Attendance Details
        [Display(Name = "Working Days")]
        public int TotalWorkingDays { get; set; }

        [Display(Name = "Present Days")]
        public int PresentDays { get; set; }

        [Display(Name = "Absent Days")]
        public int AbsentDays { get; set; }

        [Display(Name = "Leave Days")]
        public int LeaveDays { get; set; }

        [Display(Name = "Paid Leaves")]
        public int PaidLeaves { get; set; }

        [Display(Name = "Unpaid Leaves")]
        public int UnpaidLeaves { get; set; }

        [Display(Name = "Late Days")]
        public int LateDays { get; set; }

        // Leave Deduction
        [Display(Name = "Leave Deduction Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal LeaveDeductionAmount { get; set; }

        [Display(Name = "Overtime Hours")]
        public decimal OvertimeHours { get; set; }

        [Display(Name = "Overtime Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal OvertimeAmount { get; set; }

        // Status
        [Required]
        [Display(Name = "Status")]
        public PayrollStatus Status { get; set; } = PayrollStatus.Draft;

        [Display(Name = "Remarks")]
        [StringLength(500)]
        public string? Remarks { get; set; }

        // Audit Fields
        [Required]
        [Display(Name = "Created By")]
        public string CreatedBy { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Updated By")]
        public string? UpdatedBy { get; set; }

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Approved By")]
        public string? ApprovedBy { get; set; }

        [Display(Name = "Approved At")]
        public DateTime? ApprovedAt { get; set; }

        // Navigation Properties
        public virtual ICollection<PayrollDetail>? PayrollDetails { get; set; }

        // Computed Properties
        [NotMapped]
        public string PayPeriod => $"{GetMonthName(Month)} {Year}";

        [NotMapped]
        public string StatusDisplay => Status.ToString();

        private string GetMonthName(int month)
        {
            return new DateTime(Year, month, 1).ToString("MMMM");
        }
    }

    public enum PayrollStatus
    {
        Draft,
        Pending,
        Approved,
        Paid,
        Cancelled
    }
}
