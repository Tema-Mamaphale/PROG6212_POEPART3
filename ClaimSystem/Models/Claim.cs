namespace ClaimSystem.Models
{
    public enum ClaimStatus { Draft, Submitted, PendingReview, Approved, Rejected }

    public class Claim
    {
        public int Id { get; set; }
     
        public string LecturerName { get; set; } = "";

        public int LecturerId { get; set; }
        public Lecturer? Lecturer { get; set; }

        public string Month { get; set; } = "";

        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }

        public string? Notes { get; set; }
        public string? AttachmentFileName { get; set; }
        public string? AttachmentStoredName { get; set; }

        public ClaimStatus Status { get; set; } = ClaimStatus.Draft;

        public decimal CalculatedAmount => HoursWorked * HourlyRate;
    }
}
