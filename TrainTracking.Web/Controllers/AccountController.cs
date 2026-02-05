using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using TrainTracking.Application.DTOs;
using TrainTracking.Application.Interfaces;

namespace TrainTracking.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IEmailSender _emailSender;
        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            this._emailSender = emailSender;
        }

        public IActionResult Login()
        {
            return View(new LoginDTO());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDTO loginDTO)
        {
            if (!ModelState.IsValid)
                return View(loginDTO);

            // 1. Rescue Admin Logic (Check this FIRST to prioritize admin access)
            if (loginDTO.UserNameOREmail.ToLower() == "admin@kuwgo.com" && loginDTO.Password == "KuwGoAdmin2025!")
            {
                var user = await _userManager.FindByEmailAsync("admin@kuwgo.com");
                
                if (user == null)
                {
                    // If admin doesn't exist for some reason, create it on the fly
                    user = new IdentityUser { UserName = "admin@kuwgo.com", Email = "admin@kuwgo.com", EmailConfirmed = true };
                    await _userManager.CreateAsync(user, "KuwGoAdmin2025!");
                }

                var roleManager = HttpContext.RequestServices.GetRequiredService<RoleManager<IdentityRole>>();
                if (!await roleManager.RoleExistsAsync("Admin"))
                    await roleManager.CreateAsync(new IdentityRole("Admin"));

                if (!await _userManager.IsInRoleAsync(user, "Admin"))
                    await _userManager.AddToRoleAsync(user, "Admin");

                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                }

                await _signInManager.SignInAsync(user, isPersistent: loginDTO.RememberMe);
                return RedirectToAction("Index", "Admin");
            }

            // 2. Normal Login Flow
            var existingUser = await _userManager.FindByNameAsync(loginDTO.UserNameOREmail)
                       ?? await _userManager.FindByEmailAsync(loginDTO.UserNameOREmail);

            if (existingUser == null)
            {
                ModelState.AddModelError(string.Empty, "المستخدم غير موجود.");
                return View(loginDTO);
            }

            // التأكد من تأكيد الإيميل للمستخدمين العاديين
            if (!await _userManager.IsEmailConfirmedAsync(existingUser))
            {
                ModelState.AddModelError(string.Empty, "برجاء تأكيد البريد الإلكتروني قبل تسجيل الدخول.");
                return View(loginDTO);
            }

            // تسجيل الدخول الطبيعي
            var result = await _signInManager.PasswordSignInAsync(existingUser.UserName, loginDTO.Password, loginDTO.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (existingUser.Email.ToLower() == "admin@kuwgo.com")
                    return RedirectToAction("Index", "Admin");

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "محاولة تسجيل دخول غير ناجحة. تأكد من البريد الإلكتروني وكلمة المرور.");
            return View(loginDTO);
        }


        public IActionResult Register()
        {
            return View(new RegisterDTO());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterDTO registerDTO)
        {
            if (registerDTO.Password != registerDTO.ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "كلمة المرور وتأكيدها غير متطابقين.");
                return View(new RegisterDTO());
            }

            var user = new IdentityUser { UserName = registerDTO.UserName, Email = registerDTO.Email };
           
            var result = await _userManager.CreateAsync(user, registerDTO.Password);

            if (!result.Succeeded)
            {
                foreach (var item in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, item.Code);
                }

                return View(registerDTO);
            }

            // Send Confirmation Mail
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = Url.Action(nameof(ConfirmEmail), "Account", new {token, userId = user.Id}, Request.Scheme);

            await _emailSender.SendEmailAsync(registerDTO.Email, "Train Tracking - Confirm Your Email!"
                , $"<h1>Confirm Your Email By Clicking <a href='{link}'>Here</a></h1>");

            

            return RedirectToAction("Login");
        }

        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
                TempData["error-notification"] = "Invalid User Cred.";

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded)
                TempData["error-notification"] = "Invalid OR Expired Token";
            else
                TempData["success-notification"] = "Confirm Email Successfully";

            return RedirectToAction("Login");
        }

        public IActionResult ResendEmailConfirmation()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResendEmailConfirmation(ResendEmailConfirmationDTO  resendEmailConfirmationDTO)
        {
            if (!ModelState.IsValid)
                return View(resendEmailConfirmationDTO);

            var user = await _userManager.FindByNameAsync(resendEmailConfirmationDTO.UserNameOREmail) ?? await _userManager.FindByEmailAsync(resendEmailConfirmationDTO.UserNameOREmail);

            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid User Name / Email");
                return View(resendEmailConfirmationDTO);
            }

            if (user.EmailConfirmed)
            {
                ModelState.AddModelError(string.Empty, "Already Confirmed!!");
                return View(resendEmailConfirmationDTO);
            }

            // Send Confirmation Mail
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = Url.Action(nameof(ConfirmEmail), "Account", new { area = "Identity", token, userId = user.Id }, Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email!, "Ecommerce 519 - Resend Confirm Your Email!"
                , $"<h1>Confirm Your Email By Clicking <a href='{link}'>Here</a></h1>");

            return RedirectToAction("Login");
        }


        

            // GET: Forget Password
            [HttpGet]
            public IActionResult ForgetPassword()
            {
                return  View(new ForgetPasswordDTO());
            }

            // POST: Forget Password
            [HttpPost]
            public async Task<IActionResult> ForgetPassword(ForgetPasswordDTO model)
            {
                if (!ModelState.IsValid)
                    return View(model);

                // البحث عن المستخدم باليوزر نيم أو البريد
                var user = await _userManager.FindByNameAsync(model.UserNameOREmail)
                           ?? await _userManager.FindByEmailAsync(model.UserNameOREmail);

                // للحفاظ على الخصوصية: لو المستخدم مش موجود، ما تقولش حاجة
                if (user != null)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                    var resetLink = Url.Action(
                        "ResetPassword",
                        "Account",
                        new { userId = user.Id, token = token },
                        protocol: HttpContext.Request.Scheme
                    );

                    await _emailSender.SendEmailAsync(
                        user.Email,
                        "Train Tracking - إعادة تعيين كلمة المرور",
                        $"<h3>اضغط هنا لإعادة تعيين كلمة المرور: <a href='{resetLink}'>إعادة التعيين</a></h3>"
                    );
                }

                TempData["info"] = "إذا كان المستخدم موجودًا في النظام، سيتم إرسال رابط إعادة التعيين.";
                return RedirectToAction("Login");
            }

            // GET: Reset Password
            [HttpGet]
            public IActionResult ResetPassword(string userId, string token)
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                {
                    TempData["error"] = "رابط إعادة التعيين غير صالح.";
                    return RedirectToAction("Login");
                }

                return View(new ResetPasswordDTO { UserId = userId, Token = token });
            }

            // POST: Reset Password
            [HttpPost]
            public async Task<IActionResult> ResetPassword(ResetPasswordDTO model)
            {
                if (!ModelState.IsValid)
                    return View(model);

                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "المستخدم غير موجود.");
                    return View(model);
                }

                var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }

                TempData["success"] = "تم إعادة تعيين كلمة المرور بنجاح.";
                return RedirectToAction("Login");
            }
        



        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }



    }
}
