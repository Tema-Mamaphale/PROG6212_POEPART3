
using System;

namespace ClaimSystem.Models
{
    public class ClaimAudit
    {
        public int Id { get; set; }
        public int ClaimId { get; set; }
        public string Action { get; set; } = "";
        public string? Notes { get; set; }
        public DateTime WhenUtc { get; set; } = DateTime.UtcNow;
    }
}
