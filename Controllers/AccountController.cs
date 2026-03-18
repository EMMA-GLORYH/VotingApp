using Microsoft.AspNetCore.Mvc;
using SchoolVotingApp.Models;
using MySql.Data.MySqlClient;

namespace SchoolVotingApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly string _connectionString;

        public AccountController(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult StaffPortal() => View("Login");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StaffPortal(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View("Login", model);

            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "SELECT FullName FROM Users WHERE Email=@e AND PasswordHash=@p LIMIT 1";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@e", model.Email);
                        cmd.Parameters.AddWithValue("@p", model.Password);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read()) return RedirectToAction("Dashboard", "Admin");
                        }
                    }
                }
                ModelState.AddModelError("", "Invalid credentials.");
            }
            catch { ModelState.AddModelError("", "Database connection error (Check XAMPP)."); }

            return View("Login", model);
        }

        public IActionResult VoterPortal() => RedirectToAction("TokenEntry", "Voting");
    }
}