
namespace ClaimSystem.Models
{
    public class ClaimPolicy
    {
        public decimal MaxHoursPerMonth { get; set; }
        public decimal MinHourlyRate { get; set; }
        public decimal MaxHourlyRate { get; set; }
        public decimal AutoApproveThreshold { get; set; }
    }
}
