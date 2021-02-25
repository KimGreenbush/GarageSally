using System;
using Microsoft.AspNetCore.Mvc;
using UserLogin.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

// for file upload
using System.IO;
using System.Threading.Tasks;
using IFormFile = Microsoft.AspNetCore.Http.IFormFile;


namespace UserLogin.Controllers
{
    public class HomeController : Controller
    {
        private MyContext _context;

        public HomeController(MyContext context)
        {
            _context = context;
        }

        // Navigation no process
        [HttpGet("signin")]
        public IActionResult gotoSignin()
        {
            return RedirectToAction("login");
        }

        [HttpGet("registration")]
        public IActionResult gotoRegistration()
        {
            return RedirectToAction("index");
        }
        // -----------------------------------------------------------end

        // Rendering Pages in Views--------------------------------------------
        [HttpGet("")]
        public IActionResult index()
        {


            DashboardWrapper wMod = new DashboardWrapper();
            return View("index", wMod);
        }



        [HttpGet("login")]
        public IActionResult login()
        {
            return View("login");
        }

        [HttpGet("dashboard")]
        public IActionResult dashboard()
        {
            // block pages is not in session
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("login");

            }

            int UserIdInSession = (int)HttpContext.Session.GetInt32("UserId");
            // Filter db by User in Session 
            User UserIndb = _context.Users
                .FirstOrDefault(u => u.UserId == UserIdInSession);
            ViewBag.ToDisplay = UserIndb;

            // filter db by user id 
            ViewBag.allUserLogs = _context.Users
                .Where(ul => ul.UserId == UserIdInSession)
                .ToList();


            DashboardWrapper wMod = new DashboardWrapper();


            return View("dashboard", wMod);
        }


        [HttpGet("profile")]
        public IActionResult profilePartial()
        {



            // block pages is not in session
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("login");
            }

            ViewBag.AllUsers = _context.Users.ToList();


            int UserIdInSession = (int)HttpContext.Session.GetInt32("UserId");

            // Filter db by User in Session 
            User UserIndb = _context.Users
                .FirstOrDefault(u => u.UserId == UserIdInSession);
            ViewBag.ToDisplay = UserIndb;


            // filter db by section id 
            ViewBag.allUserLogs = _context.Users
                .Where(ul => ul.UserId == UserIdInSession)
                .ToList();



            return View("profilePartial");
        }


        // -----------------------------------------------------------end

        // Processing Registration and Login-------------------------------------------------
        [HttpPost("register")]
        public async Task<IActionResult> Redgister(List<IFormFile> files, User FromForm)
        {

            DashboardWrapper wMod = new DashboardWrapper();

            // Check if email is already in db
            if (_context.Users.Any(u => u.Email == FromForm.Email))
            {
                ModelState.AddModelError("Email", "Email already in use!");
            }
            // Validations
            if (ModelState.IsValid)
            {

                long size = files.Sum(f => f.Length);

                System.Console.WriteLine("here is files:", files);
                var filePaths = new List<string>();
                foreach (var formFile in files)
                {
                    if (formFile.Length > 0)
                    {
                        // TimeStamp
                        string timeStampMonth = DateTime.Now.Month.ToString("00");
                        string timeStampDay = DateTime.Now.Day.ToString("00");
                        string timeStampHour = DateTime.Now.Hour.ToString("00");
                        string timeStampMinutes = DateTime.Now.Minute.ToString("00");
                        string timeStampSeconds = DateTime.Now.Second.ToString("00");

                        string timeStamp = $"{timeStampMonth}{timeStampDay}{timeStampHour}{timeStampMinutes}{timeStampSeconds}";

                        //Place to save file
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(),
                         "wwwroot/img/uploads", $"{timeStamp}{formFile.FileName}");

                        // for the db
                        Console.WriteLine($"Apprentice Name: {FromForm.Username}");
                        Console.WriteLine($"FileName: {timeStamp}{formFile.FileName}");

                        // Assign name to be saved to the db
                        string newName = $"{timeStamp}{formFile.FileName}";
                        FromForm.ProfilePic = newName;


                        filePaths.Add(filePath);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await formFile.CopyToAsync(stream);
                            // #hash password
                            PasswordHasher<User> Hasher = new PasswordHasher<User>();
                            FromForm.Password = Hasher.HashPassword(FromForm, FromForm.Password);
                        }
                    }
                }

                // Add to db
                _context.Add(FromForm);
                _context.SaveChanges();
                // Session
                HttpContext.Session.SetInt32("UserId", _context.Users.FirstOrDefault(i => i.UserId == FromForm.UserId).UserId);
                // Redirect
                Console.WriteLine("You may contine!");
                return RedirectToAction("dashboard");
            }
            else
            {
                Console.WriteLine("Fix your erros!");
                return View("index", wMod);
            }

        }

        //Processing Registration Login-------------------------------------------------
        [HttpPost("login")]
        public IActionResult Login(LoginUser userSubmission)
        {

            DashboardWrapper wMod = new DashboardWrapper();

            // Validations
            if (ModelState.IsValid)
            {
                // Check db email with from email
                var userInDb = _context.Users.FirstOrDefault(u => u.Email == userSubmission.Email);
                // No user in db
                if (userInDb == null)
                {
                    ModelState.AddModelError("Email", "Invalid Email/Password");
                    return View("login");
                }
                // Check hashing are the same
                var hasher = new PasswordHasher<LoginUser>();
                var result = hasher.VerifyHashedPassword(userSubmission, userInDb.Password, userSubmission.Password);
                if (result == 0)
                {
                    // handle failure (this should be similar to how "existing email" is handled)
                }
                // Set Session Instance
                HttpContext.Session.SetInt32("UserId", userInDb.UserId);
                return RedirectToAction("dashboard");
            }
            return View("index", wMod);
        }


        [HttpGet("logout")]
        public IActionResult logout()
        {
            // Clear Session
            HttpContext.Session.Clear();
            return RedirectToAction("index");
        }
        // ------------------------------------------end of registration and login
    }
}