using System.Threading.Tasks;
using ClaimSystem.Data;
using ClaimSystem.Models;
using ClaimSystem.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClaimSystem.Controllers
{
    [RoleRequired(RoleNames.HR)]
    public class HRLecturersController : Controller
    {
        private readonly ApplicationDbContext _db;
        public HRLecturersController(ApplicationDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _db.Lecturers.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
            return View(list);
        }

        [HttpGet]
        public IActionResult Create() => View(new Lecturer());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Lecturer m)
        {
            if (!ModelState.IsValid) return View(m);
            _db.Lecturers.Add(m);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Lecturer added.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Lecturers.FindAsync(id);
            return m is null ? NotFound() : View(m);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Lecturer m)
        {
            if (!ModelState.IsValid) return View(m);
            _db.Lecturers.Update(m);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Lecturer updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Lecturers.FindAsync(id);
            if (m is null) return NotFound();
            _db.Lecturers.Remove(m);
            await _db.SaveChangesAsync();
            TempData["ok"] = "Lecturer removed.";
            return RedirectToAction(nameof(Index));
        }
    }
}
