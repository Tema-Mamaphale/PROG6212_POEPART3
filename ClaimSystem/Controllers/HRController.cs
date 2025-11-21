using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClaimSystem.Data;
using ClaimSystem.Models;
using ClaimSystem.Models.ViewModels;
using ClaimSystem.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClaimSystem.Controllers
{
    [RoleRequired(RoleNames.HR)]
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<HRController> _log;

        public HRController(ApplicationDbContext db, ILogger<HRController> log)
        {
            _db = db;
            _log = log;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? month = null)
        {

            var months = await _db.Claims.AsNoTracking()
                .Where(c => c.Status == ClaimStatus.Approved && c.Month != null && c.Month != "")
                .Select(c => c.Month)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();

            if (string.IsNullOrWhiteSpace(month))
                month = months.FirstOrDefault() ?? DateTime.UtcNow.ToString("MMMM yyyy");

            var query = _db.Claims.AsNoTracking()
                .Where(c => c.Status == ClaimStatus.Approved && c.Month == month);

            var rows = await query
                .GroupBy(c => c.LecturerName)
                .Select(g => new HrSummaryRow
                {
                    LecturerName = g.Key,
                    TotalHours = g.Sum(x => x.HoursWorked),
                    AverageRate = g.Average(x => x.HourlyRate),
                    TotalAmount = g.Sum(x => x.HoursWorked * x.HourlyRate)
                })
                .OrderBy(r => r.LecturerName)
                .ToListAsync();

            var vm = new HrMonthlySummaryViewModel
            {
                SelectedMonth = month,
                AvailableMonths = months,
                Rows = rows,
                GrandTotalHours = rows.Sum(r => r.TotalHours),
                GrandTotalAmount = rows.Sum(r => r.TotalAmount)
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Invoice(string lecturer, string month)
        {
            if (string.IsNullOrWhiteSpace(lecturer) || string.IsNullOrWhiteSpace(month))
                return BadRequest();

            var items = await _db.Claims.AsNoTracking()
                .Where(c => c.Status == ClaimStatus.Approved
                            && c.LecturerName == lecturer
                            && c.Month == month)
                .OrderBy(c => c.Id)
                .ToListAsync();

            if (items.Count == 0) return NotFound();

            return View(items); 
        }

        [HttpGet]
        public async Task<IActionResult> ExportApprovedForMonthCsv(string month)
        {
            if (string.IsNullOrWhiteSpace(month)) return BadRequest();

            var rows = await _db.Claims.AsNoTracking()
                .Where(c => c.Status == ClaimStatus.Approved && c.Month == month)
                .OrderBy(c => c.LecturerName).ThenBy(c => c.Id)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Id,Lecturer,Month,Hours,Rate,Amount");
            foreach (var r in rows)
            {
                static string Esc(string? s) => $"\"{(s ?? "").Replace("\"", "\"\"")}\"";
                sb.AppendLine(string.Join(",",
                    r.Id.ToString(),
                    Esc(r.LecturerName),
                    Esc(r.Month),
                    r.HoursWorked.ToString("0.##"),
                    r.HourlyRate.ToString("0.##"),
                    (r.HoursWorked * r.HourlyRate).ToString("0.##")
                ));
            }

            var bom = Encoding.UTF8.GetPreamble();
            var csv = Encoding.UTF8.GetBytes(sb.ToString());
            var output = new byte[bom.Length + csv.Length];
            Buffer.BlockCopy(bom, 0, output, 0, bom.Length);
            Buffer.BlockCopy(csv, 0, output, bom.Length, csv.Length);

            var fileName = $"HR_Approved_{month.Replace(' ', '_')}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return File(output, "text/csv", fileName);
        }
    }
}
