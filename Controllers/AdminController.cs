using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;

namespace SchoolVotingApp.Controllers
{
    public class Voter
    {
        public string StudentId { get; set; }
        public string FullName { get; set; }
        public string VoterClass { get; set; }
        public string Gender { get; set; }
        public bool IsCandidate { get; set; }
        public bool HasVoted { get; set; }
        public bool IsActive { get; set; }
        // Added ProfileImage property
        public string? ProfileImage { get; set; }
    }

    public class AdminController : Controller
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _environment; // Added for safe path mapping

        public AdminController(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _environment = environment;
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult VoterTokenHub()
        {
            List<Voter> voters = new List<Voter>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    // Added profile_image to the SELECT query
                    string sql = "SELECT student_id, full_name, voter_class, gender, is_candidate, has_voted, is_active, profile_image FROM voters ORDER BY created_at DESC";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    conn.Open();
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            voters.Add(new Voter
                            {
                                StudentId = reader["student_id"].ToString(),
                                FullName = reader["full_name"].ToString(),
                                VoterClass = reader["voter_class"]?.ToString(),
                                Gender = reader["gender"].ToString(),
                                IsCandidate = Convert.ToBoolean(reader["is_candidate"]),
                                HasVoted = Convert.ToBoolean(reader["has_voted"]),
                                IsActive = Convert.ToBoolean(reader["is_active"]),
                                // Read profile image path
                                ProfileImage = reader["profile_image"]?.ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Read Error: " + ex.Message;
            }
            return View(voters);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVoter(string full_name, string voter_class, string gender, bool is_candidate, IFormFile? profile_image)
        {
            try
            {
                string? imagePath = null;
                if (profile_image != null && profile_image.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(profile_image.FileName);
                    // FIXED: Use WebRootPath for reliable access to wwwroot
                    string uploadDir = Path.Combine(_environment.WebRootPath, "uploads", "voters");

                    if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                    imagePath = "/uploads/voters/" + fileName;
                    using (var stream = new FileStream(Path.Combine(uploadDir, fileName), FileMode.Create))
                    {
                        await profile_image.CopyToAsync(stream);
                    }
                }

                string generatedId = $"STU-{DateTime.Now.Year}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    string sql = @"INSERT INTO voters (student_id, full_name, voter_class, gender, is_candidate, is_active, profile_image, created_at) 
                                   VALUES (@sid, @name, @class, @gen, @isCand, 1, @img, NOW())";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@sid", generatedId);
                    cmd.Parameters.AddWithValue("@name", full_name);
                    cmd.Parameters.AddWithValue("@class", voter_class);
                    cmd.Parameters.AddWithValue("@gen", gender);
                    cmd.Parameters.AddWithValue("@isCand", is_candidate);
                    cmd.Parameters.AddWithValue("@img", (object?)imagePath ?? DBNull.Value);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                TempData["Success"] = "Voter Enrolled Successfully!";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction("VoterTokenHub");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVoter(string student_id, string full_name, string voter_class, string gender, bool is_candidate, IFormFile? profile_image)
        {
            try
            {
                string? imagePath = null;
                // Handle new image upload during edit
                if (profile_image != null && profile_image.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(profile_image.FileName);
                    // FIXED: Use WebRootPath for reliable access to wwwroot
                    string uploadDir = Path.Combine(_environment.WebRootPath, "uploads", "voters");

                    if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                    imagePath = "/uploads/voters/" + fileName;
                    using (var stream = new FileStream(Path.Combine(uploadDir, fileName), FileMode.Create))
                    {
                        await profile_image.CopyToAsync(stream);
                    }
                }

                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    // Update query checks if a new image was provided; if not, it keeps the old one
                    string sql = @"UPDATE voters 
                                   SET full_name = @name, 
                                       voter_class = @class, 
                                       gender = @gen, 
                                       is_candidate = @isCand" +
                                       (imagePath != null ? ", profile_image = @img " : " ") +
                                   "WHERE student_id = @id";

                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@name", full_name);
                    cmd.Parameters.AddWithValue("@class", voter_class);
                    cmd.Parameters.AddWithValue("@gen", gender);
                    cmd.Parameters.AddWithValue("@isCand", is_candidate);
                    cmd.Parameters.AddWithValue("@id", student_id);
                    if (imagePath != null) cmd.Parameters.AddWithValue("@img", imagePath);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                TempData["Success"] = "Update Successful";
            }
            catch (Exception ex) { TempData["Error"] = "Update Error: " + ex.Message; }
            return RedirectToAction("VoterTokenHub");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleStatus(string id)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    string sql = "UPDATE voters SET is_active = NOT is_active WHERE student_id = @id";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex) { TempData["Error"] = "Status Change Error: " + ex.Message; }
            return RedirectToAction("VoterTokenHub");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportVoters(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["Error"] = "Please select a valid CSV file.";
                return RedirectToAction("VoterTokenHub");
            }

            int successCount = 0;
            int errorCount = 0;

            try
            {
                using (var reader = new StreamReader(excelFile.OpenReadStream()))
                {
                    // Skip the header row
                    await reader.ReadLineAsync();

                    using (MySqlConnection conn = new MySqlConnection(_connectionString))
                    {
                        await conn.OpenAsync();

                        while (!reader.EndOfStream)
                        {
                            var line = await reader.ReadLineAsync();
                            var values = line.Split(','); // Assumes: FullName, Class, Gender, IsCandidate(0/1)

                            if (values.Length >= 3)
                            {
                                try
                                {
                                    string generatedId = $"STU-{DateTime.Now.Year}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
                                    string sql = @"INSERT INTO voters (student_id, full_name, voter_class, gender, is_candidate, is_active, created_at) 
                                           VALUES (@sid, @name, @class, @gen, @isCand, 1, NOW())";

                                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                                    {
                                        cmd.Parameters.AddWithValue("@sid", generatedId);
                                        cmd.Parameters.AddWithValue("@name", values[0].Trim());
                                        cmd.Parameters.AddWithValue("@class", values[1].Trim());
                                        cmd.Parameters.AddWithValue("@gen", values[2].Trim());
                                        // Default to 0 if column 4 is missing or empty
                                        bool isCand = values.Length > 3 && values[3].Trim() == "1";
                                        cmd.Parameters.AddWithValue("@isCand", isCand);

                                        await cmd.ExecuteNonQueryAsync();
                                        successCount++;
                                    }
                                }
                                catch { errorCount++; }
                            }
                        }
                    }
                }
                TempData["Success"] = $"Successfully imported {successCount} students. Errors: {errorCount}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Import failed: " + ex.Message;
            }

            return RedirectToAction("VoterTokenHub");
        }
    }
}