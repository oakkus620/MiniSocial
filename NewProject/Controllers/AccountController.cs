using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewProject.Data;
using NewProject.Models;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using YourProject.Models;

namespace NewProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // 1. GİRİŞ SAYFASI (GET)
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        // 2. GİRİŞ YAPMA İŞLEMİ (POST)
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string emailOrUsername, string password)
        {
            if (string.IsNullOrWhiteSpace(emailOrUsername) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.ErrorMessage = "Lütfen tüm alanları doldurun.";
                return View();
            }

            var user = _context.Users.FirstOrDefault(u =>
                (u.Email.ToLower() == emailOrUsername.ToLower() || u.Username.ToLower() == emailOrUsername.ToLower())
                && u.Password == password);

            if (user != null)
            {
                // Cookie (Oturum) Oluşturma
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                // Session'a da kaydedelim
                HttpContext.Session.SetString("GirisYapanKullanici", user.Username);

                // BAŞARILI GİRİŞ -> HOME CONTROLLER INDEX SAYFASINA UÇURUYORUZ
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ErrorMessage = "E-posta/Kullanıcı adı veya şifre hatalı!";
            return View();
        }

        // 3. KAYIT SAYFASI (GET)
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Register(string adSoyad, string username, string email, string password, int birthDay, int birthMonth, int birthYear)
        {
            if (_context.Users.Any(u => u.Email.ToLower() == email.ToLower()))
            {
                TempData["InfoMessage"] = "Bu e-posta adresiyle zaten bir hesap var. Lütfen giriş yapın.";
                return RedirectToAction("Login");
            }

            Random random = new Random();
            string code = random.Next(100000, 999999).ToString();

            var newUser = new User
            {
                AdSoyad = adSoyad,
                Username = username,
                Email = email,
                Password = password,
                BirthDay = birthDay,
                BirthMonth = birthMonth,
                BirthYear = birthYear,
                IsEmailConfirmed = false
            };
            _context.Users.Add(newUser);
            _context.SaveChanges();

            // TempData yerine Session kullanıyoruz (Asla silinmez/kaybolmaz)
            HttpContext.Session.SetString("VerificationEmail", email);
            HttpContext.Session.SetString("VerificationCode", code);

            SendVerificationEmail(email, code);
            return RedirectToAction("ConfirmEmail");
        }

        [HttpGet]
        public IActionResult CheckUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return Json(new { isAvailable = false });

            bool isTaken = _context.Users.Any(u => u.Username.ToLower() == username.ToLower());
            return Json(new { isAvailable = !isTaken });
        }

        [HttpGet]
        public IActionResult CheckEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { isAvailable = false });

            bool isTaken = _context.Users.Any(u => u.Email.ToLower() == email.ToLower());
            return Json(new { isAvailable = !isTaken });
        }


        [HttpGet]
        public IActionResult ConfirmEmail()
        {
            // Session'dan e-postayı ekrana taşıyoruz
            ViewBag.Email = HttpContext.Session.GetString("VerificationEmail");
            return View();
        }
        [HttpPost]
        public IActionResult ConfirmEmail(string inputCode)
        {
            string? actualCode = HttpContext.Session.GetString("VerificationCode") ?? "";
            string? targetEmail = HttpContext.Session.GetString("VerificationEmail") ?? "";

            // 🔍 BURAYA BAKALIM: Konsola (Terminal/Çıktı penceresine) yazdıralım ki ne geldiğini görelim
            System.Diagnostics.Debug.WriteLine($"===> Gelen Kod: '{inputCode}' | Beklenen Kod: '{actualCode}' | Hedef Email: '{targetEmail}'");

            if (!string.IsNullOrEmpty(inputCode) && inputCode.Trim() == actualCode.Trim() && !string.IsNullOrEmpty(targetEmail))
            {
                var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == targetEmail.ToLower());

                if (user != null)
                {
                    user.IsEmailConfirmed = true;
                    _context.SaveChanges();

                    HttpContext.Session.SetString("GirisYapanKullanici", user.Username);

                    // Başarılı -> Detaing sayfasına gönderiyoruz
                    return RedirectToAction("Detaing");
                }
            }

            ViewBag.ErrorMessage = "Hatalı onay kodu girdiniz!";
            ViewBag.Email = targetEmail;
            return View();
        }

        private void SendVerificationEmail(string destinationEmail, string code)
        {
            try
            {
                string senderEmail = "Haricioakkus1@gmail.com";
                string appPassword = "nuda dcgr kpeo zjkp";

                var fromAddress = new MailAddress(senderEmail, "Hesap Doğrulama");
                var toAddress = new MailAddress(destinationEmail);

                string subject = "E-posta Doğrulama Kodunuz";
                string body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>
                    <h2>Aramıza Hoş Geldiniz!</h2>
                    <p>Kayıt işlemini tamamlamak için aşağıdaki doğrulama kodunu kullanabilirsiniz:</p>
                    <h1 style='color: #0d6efd; letter-spacing: 5px;'>{code}</h1>
                    <p>Bu kodu kimseyle paylaşmayınız.</p>
                </div>";

                using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.EnableSsl = true;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(senderEmail, appPassword);
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    })
                    {
                        smtp.Send(message);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"===> E-POSTA GÖNDERME HATASI: {ex.Message}");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Detay/Profil Foto Ekranı (GET)
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Detaing()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Detaing(string detail, IFormFile? profileInput)
        {
            string? currentUsername = HttpContext.Session.GetString("GirisYapanKullanici");

            if (string.IsNullOrEmpty(currentUsername))
            {
                return RedirectToAction("Login");
            }

            var user = _context.Users.FirstOrDefault(u => u.Username == currentUsername);

            if (user != null)
            {
                user.Bio = detail;

                if (profileInput != null && profileInput.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(profileInput.FileName);
                    string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads"));

                    using (var stream = new FileStream(uploadPath, FileMode.Create))
                    {
                        await profileInput.CopyToAsync(stream);
                    }

                    user.ProfilePicturePath = "/uploads/" + fileName;
                }

                _context.SaveChanges();
            }

            // İşlem bitince Ana Sayfaya yönlendiriyoruz
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [HttpGet]
        public IActionResult Logout()
        {
            // Oturumu temizliyoruz
            HttpContext.Session.Clear();

            // Tarayıcı önbelleğini tamamen temizleyerek geri tuşuna basıldığında eski sayfaya dönülmesini engelliyoruz
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return RedirectToAction("Login", "Account");
        }


    }
}


