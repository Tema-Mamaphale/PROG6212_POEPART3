using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClaimSystem.Models.ViewModels
{
    public class ClaimFormViewModel
    {
        
        [Display(Name = "Lecturer")]
        public int? LecturerId { get; set; }

        [Required, StringLength(120)]
        [Display(Name = "Lecturer Name")]
        public string LecturerName { get; set; } = "";

        [Required, StringLength(40)]
        [Display(Name = "Month")]
        [RegularExpression(@"^[A-Za-z]+\s+\d{4}$", ErrorMessage = "Use format like “October 2025”.")]
        public string Month { get; set; } = "";

        [Range(0.5, 180, ErrorMessage = "Hours must be between 0.5 and 180 for a month.")]
        [Display(Name = "Hours Worked")]
        public decimal HoursWorked { get; set; }

        [Range(100, 1000, ErrorMessage = "Rate must be between R100 and R1000.")]
        [Display(Name = "Hourly Rate (R)")]
        public decimal HourlyRate { get; set; }

        [StringLength(500)]
        [Display(Name = "Notes (optional)")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Please attach a supporting document (.pdf, .docx, .xlsx).")]
        [DataType(DataType.Upload)]
        [Display(Name = "Upload Supporting Document")]
        public IFormFile? File { get; set; }

        // helper for the view
        public List<SelectListItem> AvailableLecturers { get; set; } = new();
        public decimal CalculatedAmount => HoursWorked * HourlyRate;
    }
}
