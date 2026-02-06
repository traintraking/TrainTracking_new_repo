using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using TrainTracking.Application.Interfaces;
using TrainTracking.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System;

namespace TrainTracking.Web.Controllers
{
   
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IUserService _userService;

        public ProfileController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: Profile/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                // جيب الـ User ID من المستخدم المسجل دخوله
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Content("User not authenticated - redirecting to login");
                }

                // حاول تجيب الـ User من الـ Database بالـ Email
                var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                           ?? User.Identity?.Name
                           ?? "unknown@example.com";

                var user = await _userService.GetUserByEmailAsync(email);

                // لو مفيش User، اعمل واحد جديد
                if (user == null)
                {
                    user = new User
                    {
                        FullName = User.Identity?.Name ?? "مستخدم جديد",
                        Username = User.Identity?.Name ?? "user",
                        Email = email,
                        PhoneNumber = "غير محدد",
                        Password = "temp123",
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };

                    // احفظ الـ User في الـ Database
                    user = await _userService.CreateUserAsync(user);
                }

                return View(user);
            }
            catch (Exception ex)
            {
                // بدل ما نعمل Redirect، نعرض الـ Error
                return Content($"ERROR: {ex.Message}\n\nStack Trace: {ex.StackTrace}\n\nInner: {ex.InnerException?.Message}");
            }
        }
        // GET: Profile/Edit
        public async Task<IActionResult> Edit()
        {
            try
            {
                var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                           ?? User.Identity?.Name;

                if (string.IsNullOrEmpty(email))
                {
                    return RedirectToAction("Login", "Account");
                }

                var user = await _userService.GetUserByEmailAsync(email);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "لم يتم العثور على الملف الشخصي";
                    return RedirectToAction("Index");
                }

                return View(user);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"حدث خطأ: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(User model, IFormFile profilePicture)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // حفظ الصورة لو موجودة
                    if (profilePicture != null && profilePicture.Length > 0)
                    {
                        var picturePath = await _userService.SaveProfilePictureAsync(profilePicture, model.Id);
                        model.ProfilePicturePath = picturePath;
                    }

                    await _userService.UpdateUserAsync(model);

                    TempData["SuccessMessage"] = "تم تحديث الملف الشخصي بنجاح!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "حدث خطأ أثناء التحديث: " + ex.Message);
                }
            }

            return View(model);
        }

        // POST: Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(int userId, string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "كلمة المرور الجديدة وتأكيدها غير متطابقين";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userService.GetUserByIdAsync(userId);

            if (user == null || user.Password != currentPassword)
            {
                TempData["ErrorMessage"] = "كلمة المرور الحالية غير صحيحة";
                return RedirectToAction(nameof(Index));
            }

            user.Password = newPassword;
            await _userService.UpdateUserAsync(user);

            TempData["SuccessMessage"] = "تم تغيير كلمة المرور بنجاح!";
            return RedirectToAction(nameof(Index));
        }
    }
}
