using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClaimSystem.Data;
using ClaimSystem.Models;
using ClaimSystem.Models.ViewModels;
using ClaimSystem.Security;
using ClaimSystem.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClaimSystem.Controllers
{
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ClaimsController> _log;

        private const decimal MaxHoursPerMonth = 180m;
        private const decimal MinHourlyRate = 100m;
        private const decimal MaxHourlyRate = 1000m;
        private const decimal AutoApproveThreshold = 5000m;

        public ClaimsController(ApplicationDbContext db, IWebHostEnvironment env, ILogger<ClaimsController> log)
        {
            _db = db;
            _env = env;
            _log = log;
        }

        public IActionResult Index() => RedirectToAction(nameof(Submit));


        [HttpGet]
        [RoleRequired(RoleNames.Lecturer)]
        public async Task<IActionResult> Submit()
        {
            var vm = new ClaimFormViewModel
            {
                Month = DateTime.UtcNow.ToString("MMMM yyyy")
            };

            await PopulateLecturers(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleRequired(RoleNames.Lecturer)]
        public async Task<IActionResult> Submit(ClaimFormViewModel vm)
        {
           
            await PopulateLecturers(vm);

            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                var lecturerName = (vm.LecturerName ?? string.Empty).Trim();
                var month = (vm.Month ?? string.Empty).Trim();

                Lecturer? selectedLecturer = null;
                if (vm.LecturerId.HasValue && vm.LecturerId.Value > 0)
                {
                    selectedLecturer = await _db.Lecturers.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == vm.LecturerId.Value && x.IsActive);
                    if (selectedLecturer is null)
                    {
                        ModelState.AddModelError(nameof(vm.LecturerId), "Selected lecturer not found or not active.");
                        return View(vm);
                    }

                    lecturerName = selectedLecturer.Name;
                    vm.HourlyRate = selectedLecturer.HourlyRate;
                }

                var exists = await _db.Claims.AnyAsync(c =>
                    c.LecturerName == lecturerName &&
                    c.Month == month &&
                    c.Status != ClaimStatus.Rejected);

                if (exists)
                {
                    ModelState.AddModelError(string.Empty,
                        "A claim for this lecturer and month already exists and is under review or already submitted.");
                    ModelState.AddModelError(nameof(vm.Month), "Duplicate for this month.");
                    return View(vm);
                }

                if (vm.HoursWorked <= 0 || vm.HoursWorked > MaxHoursPerMonth)
                {
                    ModelState.AddModelError(nameof(vm.HoursWorked),
                        $"Hours must be greater than 0 and no more than {MaxHoursPerMonth} for a month.");
                    return View(vm);
                }

                if (vm.HourlyRate < MinHourlyRate || vm.HourlyRate > MaxHourlyRate)
                {
                    ModelState.AddModelError(nameof(vm.HourlyRate),
                        $"Hourly rate must be between R{MinHourlyRate:N0} and R{MaxHourlyRate:N0}.");
                    return View(vm);
                }

                var claim = new Claim
                {
                    LecturerId = vm.LecturerId ?? 0,
                    LecturerName = lecturerName,
                    Month = month,
                    HoursWorked = vm.HoursWorked,
                    HourlyRate = vm.HourlyRate,
                    Notes = string.IsNullOrWhiteSpace(vm.Notes) ? null : vm.Notes.Trim(),
                    Status = ClaimStatus.Submitted
                };

                if (claim.CalculatedAmount <= 0)
                {
                    ModelState.AddModelError(string.Empty, "Calculated amount must be greater than zero.");
                    return View(vm);
                }

                _db.Claims.Add(claim);
                await _db.SaveChangesAsync();

                if (vm.File is { Length: > 0 })
                {
                    if (!DocumentRules.IsAllowed(vm.File.FileName))
                    {
                        ModelState.AddModelError("File", "Only .pdf, .docx, or .xlsx files are allowed.");
                        return View(vm);
                    }
                    if (DocumentRules.IsTooLarge(vm.File.Length))
                    {
                        ModelState.AddModelError("File", "File too large (max 10 MB).");
                        return View(vm);
                    }

                    var uploadsRoot = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "claims", claim.Id.ToString());
                    Directory.CreateDirectory(uploadsRoot);

                    var stored = $"{Guid.NewGuid():N}{Path.GetExtension(vm.File.FileName)}";
                    var savePath = Path.Combine(uploadsRoot, stored);

                    using (var fs = System.IO.File.Create(savePath))
                        await vm.File.CopyToAsync(fs);

                    claim.AttachmentFileName = vm.File.FileName;
                    claim.AttachmentStoredName = stored;
                    await _db.SaveChangesAsync();
                }

                TempData["ok"] = "Claim submitted successfully.";
                return RedirectToAction(nameof(Status), new { id = claim.Id });
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Submit failed");
                ModelState.AddModelError(string.Empty, "Sorry, something went wrong while submitting your claim.");
                return View(vm);
            }
        }

        private async Task PopulateLecturers(ClaimFormViewModel vm)
        {
            var lecturers = await _db.Lecturers
                .AsNoTracking()
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .ToListAsync();

            vm.AvailableLecturers = lecturers
                .Select(l => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Text = $"{l.Name} (R {l.HourlyRate:0.##})",
                    Value = l.Id.ToString(),
                    Selected = vm.LecturerId.HasValue && vm.LecturerId.Value == l.Id
                })
                .ToList();

            if (!vm.LecturerId.HasValue && !string.IsNullOrWhiteSpace(vm.LecturerName))
            {
                var match = lecturers.FirstOrDefault(x => x.Name.Equals(vm.LecturerName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    vm.LecturerId = match.Id;
                    
                    vm.HourlyRate = match.HourlyRate;
                }
            }
        }

        private (bool ok, string reason, bool autoApprove) AutoValidateClaim(Claim c)
        {
            if (c.HoursWorked <= 0 || c.HourlyRate <= 0)
                return (false, "Hours or rate cannot be zero or negative.", false);

            if (c.HoursWorked > MaxHoursPerMonth)
                return (false, $"Hours exceed {MaxHoursPerMonth} for the month.", false);

            if (c.HourlyRate < MinHourlyRate || c.HourlyRate > MaxHourlyRate)
                return (false, $"Rate must be between R{MinHourlyRate:N0} and R{MaxHourlyRate:N0}.", false);

            var amount = c.CalculatedAmount;
            var auto = amount <= AutoApproveThreshold;
            return (true, auto ? $"Low-risk amount ≤ R{AutoApproveThreshold:N0}." : "OK", auto);
        }


        [HttpGet]
        [RoleRequired(RoleNames.Coordinator)]
        public async Task<IActionResult> CoordinatorReview()
        {
            var items = await _db.Claims
                .AsNoTracking()
                .Where(c => c.Status == ClaimStatus.Submitted)
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            return View("Review", items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleRequired(RoleNames.Coordinator)]
        public async Task<IActionResult> CoordinatorApprove(int id)
        {
            try
            {
                var claim = await _db.Claims.FindAsync(id);
                if (claim is null) return NotFound();

                var (ok, reason, auto) = AutoValidateClaim(claim);
                if (!ok)
                {
                    TempData["err"] = $"Auto-check failed: {reason}";
                    return RedirectToAction(nameof(CoordinatorReview));
                }

                if (auto)
                {
                    claim.Status = ClaimStatus.Approved;
                    await _db.SaveChangesAsync();
                    TempData["ok"] = "Auto-approved by policy (low-risk threshold).";
                    return RedirectToAction(nameof(CoordinatorReview));
                }

                var forwarded = TryTransition(claim, from: ClaimStatus.Submitted, to: ClaimStatus.PendingReview);
                if (!forwarded)
                {
                    TempData["err"] = "Only newly submitted claims can be forwarded for manager review.";
                    return RedirectToAction(nameof(CoordinatorReview));
                }

                await _db.SaveChangesAsync();
                TempData["ok"] = $"Forwarded to manager (auto-check: {reason})";
                return RedirectToAction(nameof(CoordinatorReview));
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "CoordinatorApprove failed for claim {Id}", id);
                TempData["err"] = "Could not forward the claim due to an internal error.";
                return RedirectToAction(nameof(CoordinatorReview));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleRequired(RoleNames.Coordinator)]
        public async Task<IActionResult> CoordinatorReject(int id, string? reason)
        {
            try
            {
                var claim = await _db.Claims.FindAsync(id);
                if (claim is null) return NotFound();

                var ok = TryTransition(claim, from: ClaimStatus.Submitted, to: ClaimStatus.Rejected);
                if (!ok)
                {
                    TempData["err"] = "Only newly submitted claims can be rejected by the coordinator.";
                    return RedirectToAction(nameof(CoordinatorReview));
                }

                await _db.SaveChangesAsync();
                TempData["ok"] = "Claim rejected by coordinator.";
                return RedirectToAction(nameof(CoordinatorReview));
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "CoordinatorReject failed for claim {Id}", id);
                TempData["err"] = "Could not reject the claim due to an internal error.";
                return RedirectToAction(nameof(CoordinatorReview));
            }
        }


        [HttpGet]
        [RoleRequired(RoleNames.Manager)]
        public async Task<IActionResult> ManagerReview()
        {
            var items = await _db.Claims
                .AsNoTracking()
                .Where(c => c.Status == ClaimStatus.PendingReview)
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleRequired(RoleNames.Manager)]
        public async Task<IActionResult> ManagerApprove(int id)
        {
            try
            {
                var claim = await _db.Claims.FindAsync(id);
                if (claim is null) return NotFound();

                var (ok, reason, _) = AutoValidateClaim(claim);
                if (!ok)
                {
                    TempData["err"] = $"Auto-check failed: {reason}";
                    return RedirectToAction(nameof(ManagerReview));
                }

                var done = TryTransition(claim, from: ClaimStatus.PendingReview, to: ClaimStatus.Approved);
                if (!done)
                {
                    TempData["err"] = "Only claims pending review can be approved by the manager.";
                    return RedirectToAction(nameof(ManagerReview));
                }

                await _db.SaveChangesAsync();
                TempData["ok"] = $"✅ Claim approved (auto-check: {reason}).";
                return RedirectToAction(nameof(ManagerReview));
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "ManagerApprove failed for claim {Id}", id);
                TempData["err"] = "Could not approve the claim due to an internal error.";
                return RedirectToAction(nameof(ManagerReview));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleRequired(RoleNames.Manager)]
        public async Task<IActionResult> ManagerReject(int id, string? reason)
        {
            try
            {
                var claim = await _db.Claims.FindAsync(id);
                if (claim is null) return NotFound();

                var ok = TryTransition(claim, from: ClaimStatus.PendingReview, to: ClaimStatus.Rejected);
                if (!ok)
                {
                    TempData["err"] = "Only claims pending review can be rejected by the manager.";
                    return RedirectToAction(nameof(ManagerReview));
                }

                await _db.SaveChangesAsync();
                TempData["ok"] = "⚠️ Claim rejected.";
                return RedirectToAction(nameof(ManagerReview));
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "ManagerReject failed for claim {Id}", id);
                TempData["err"] = "Could not reject the claim due to an internal error.";
                return RedirectToAction(nameof(ManagerReview));
            }
        }

        [HttpGet]
        [RoleRequired(RoleNames.Coordinator)]
        public Task<IActionResult> Review() => CoordinatorReview();


        [HttpGet]
        public async Task<IActionResult> Status(int id)
        {
            Claim? claim = null;

            if (id > 0)
            {
                claim = await _db.Claims
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == id);
            }

            if (claim is null)
            {
                claim = new Claim
                {
                    LecturerName = "—",
                    Month = "—",
                    HoursWorked = 0,
                    HourlyRate = 0,
                    Status = ClaimStatus.Submitted
                };
            }

            return View(claim);
        }

        [HttpGet]
        public async Task<IActionResult> StatusJson(int id)
        {
            var c = await _db.Claims.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound();

            return Json(new
            {
                id = c.Id,
                lecturer = c.LecturerName,
                month = c.Month,
                status = c.Status.ToString()
            });
        }

        [HttpGet]
        public async Task<IActionResult> StatusList()
        {
            var items = await _db.Claims.AsNoTracking()
                .OrderByDescending(x => x.Id)
                .Take(100)
                .ToListAsync();

            return View(items);
        }


        [HttpGet]
        [RoleRequired(RoleNames.HR)]
        public async Task<IActionResult> ExportApprovedCsv(string? month = null)
        {
            IQueryable<Claim> query = _db.Claims.AsNoTracking()
                .Where(c => c.Status == ClaimStatus.Approved);

            if (!string.IsNullOrWhiteSpace(month))
                query = query.Where(c => c.Month == month);

            var rows = await query
                .OrderBy(c => c.Month)
                .ThenBy(c => c.LecturerName)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Id,Lecturer,Month,Hours,Rate,Amount,Status,Attachment");
            foreach (var r in rows)
            {
                static string Esc(string? s) => $"\"{(s ?? "").Replace("\"", "\"\"")}\"";
                sb.AppendLine(string.Join(",",
                    r.Id.ToString(),
                    Esc(r.LecturerName),
                    Esc(r.Month),
                    r.HoursWorked.ToString("0.##"),
                    r.HourlyRate.ToString("0.##"),
                    (r.HoursWorked * r.HourlyRate).ToString("0.##"),
                    r.Status.ToString(),
                    Esc(r.AttachmentFileName)
                ));
            }

            var bom = Encoding.UTF8.GetPreamble();
            var csv = Encoding.UTF8.GetBytes(sb.ToString());
            var output = new byte[bom.Length + csv.Length];
            Buffer.BlockCopy(bom, 0, output, 0, bom.Length);
            Buffer.BlockCopy(csv, 0, output, bom.Length, csv.Length);

            var safeMonth = string.IsNullOrWhiteSpace(month) ? "All" : month.Replace(' ', '_');
            var fileName = $"ApprovedClaims_{safeMonth}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return File(output, "text/csv", fileName);
        }

        private static bool TryTransition(Claim claim, ClaimStatus from, ClaimStatus to)
        {
            if (claim.Status != from) return false;
            claim.Status = to;
            return true;
        }
    }
}
