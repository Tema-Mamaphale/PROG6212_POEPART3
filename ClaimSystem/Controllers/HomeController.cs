using System.Diagnostics;
using ClaimSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace ClaimSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult SelectRole()
        {
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SelectRole(string role)
        {
      
            var allowed = new[] { RoleNames.Lecturer, RoleNames.Coordinator, RoleNames.Manager, RoleNames.HR };
            if (!allowed.Contains(role))
            {
                TempData["err"] = "Unknown role selected.";
                return View();
            }

            HttpContext.Session.SetString("UserRole", role);
            TempData["ok"] = $"Signed in as {role}.";

         
            return role switch
            {
                RoleNames.Lecturer => RedirectToAction("Submit", "Claims"),
                RoleNames.Coordinator => RedirectToAction("Review", "Claims"),
                RoleNames.Manager => RedirectToAction("ManagerReview", "Claims"),
                RoleNames.HR => RedirectToAction("Index", "HR"),
                _ => RedirectToAction("Index")
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("UserRole");
            TempData["ok"] = "You have been signed out.";
            return RedirectToAction("Index");
        }

        public IActionResult AccessDenied()
        {
            var role = HttpContext.Session.GetString("UserRole") ?? "None";
            ViewBag.CurrentRole = role;
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
