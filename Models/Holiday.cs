using System.ComponentModel.DataAnnotations;

namespace PayRollManagementSystem.Models
{
    public class Holiday
    {
        [Key]
        public int HolidayId { get; set; }

        [Required(ErrorMessage = "Holiday name is required")]
        [StringLength(200)]
        [Display(Name = "Holiday Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Holiday Date")]
        public DateTime Date { get; set; }

        [Display(Name = "Holiday Type")]
        public HolidayType Type { get; set; } = HolidayType.Public;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [DataType(DataType.DateTime)]
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DataType(DataType.DateTime)]
        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }
    }

    public enum HolidayType
    {
        Public,
        National,
        Religious,
        Company
    }
}
