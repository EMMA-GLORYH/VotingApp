using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using System.Collections.Generic;

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
    }

    public class AdminController : Controller
    {
        private readonly string _connectionString;

        public AdminController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult VoterTokenHub()
        {
            List<Voter> voters = new List<Voter>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    // Perfectly aligned with your provided SQL Schema
                    string sql = "SELECT student_id, full_name, voter_class, gender, is_candidate, has_voted, is_active FROM voters ORDER BY created_at DESC";
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
                                IsActive = Convert.ToBoolean(reader["is_active"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Database Retrieval Error: " + ex.Message;
            }
            return View(voters);
        }

        [HttpPost]
        public IActionResult AddVoter(string full_name, string voter_class, string gender, string is_candidate)
        {
            bool isCandidateBool = is_candidate == "true";

            try
            {
                // Professional Student ID generation
                string generatedId = $"STU-{DateTime.Now.Year}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";

                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    // Removed voter_token to match your database schema
                    string sql = @"INSERT INTO voters (student_id, full_name, voter_class, gender, is_candidate, is_active, created_at) 
                                   VALUES (@sid, @name, @class, @gen, @isCand, 1, NOW())";

                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@sid", generatedId);
                    cmd.Parameters.AddWithValue("@name", full_name);
                    cmd.Parameters.AddWithValue("@class", voter_class);
                    cmd.Parameters.AddWithValue("@gen", gender);
                    cmd.Parameters.AddWithValue("@isCand", isCandidateBool);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                TempData["Success"] = $"Successfully Registered: {full_name}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Database Save Error: " + ex.Message;
            }
            return RedirectToAction("VoterTokenHub");
        }

        public IActionResult Dashboard() => View();
    
        [HttpPost]
        public IActionResult EditVoter(string student_id, string full_name, string voter_class, string gender, bool is_candidate)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connectionString))
                {
                    string sql = "UPDATE voters SET full_name = @name, voter_class = @class, gender = @gen, is_candidate = @isCand WHERE student_id = @id";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@name", full_name);
                    cmd.Parameters.AddWithValue("@class", voter_class);
                    cmd.Parameters.AddWithValue("@gen", gender);
                    cmd.Parameters.AddWithValue("@isCand", is_candidate);
                    cmd.Parameters.AddWithValue("@id", student_id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                TempData["Success"] = $"Record for {full_name} has been updated.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Update Error: " + ex.Message;
            }
            return RedirectToAction("VoterTokenHub");
        }
    }
}