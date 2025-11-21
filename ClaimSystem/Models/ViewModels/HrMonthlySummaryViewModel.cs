using System.Collections.Generic;

namespace ClaimSystem.Models.ViewModels
{
    public class HrSummaryRow
    {
        public string LecturerName { get; set; } = "";
        public decimal TotalHours { get; set; }
        public decimal AverageRate { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class HrMonthlySummaryViewModel
    {
        public string? SelectedMonth { get; set; }            
        public List<string> AvailableMonths { get; set; } = new();
        public List<HrSummaryRow> Rows { get; set; } = new();

        public decimal GrandTotalHours { get; set; }
        public decimal GrandTotalAmount { get; set; }
    }
}
