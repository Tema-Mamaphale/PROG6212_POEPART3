using System.ComponentModel.DataAnnotations;

namespace ClaimSystem.Models
{
    public class Lecturer
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; } = "";

        [EmailAddress, StringLength(200)]
        public string? Email { get; set; }

        [Phone, StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Department { get; set; }

       
        public decimal HourlyRate { get; set; } = 0m;

       
        public bool IsActive { get; set; } = true;
    }
}
