using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using TrainTracking.Application.Interfaces;
using TrainTracking.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;

namespace TrainTracking.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(IUserService userService, UserManager<ApplicationUser> userManager)
        {
            _userService = userService;
            _userManager = userManager;
        }

        // GET: Profile/Index
        public async Task<IActionResult> Index()
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
                // إذا لم يتم العثور على البروفايل (قد يحدث عند مسح قاعدة البيانات وبقاء الكوكيز)
                return Content("User profile not found. Please log in again.");
            }

            return View(user);
        }

        // GET: Profile/Edit
        public async Task<IActionResult> Edit()
        {
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
                       ?? User.Identity?.Name;
            
            var user = await _userService.GetUserByEmailAsync(email);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ApplicationUser model, IFormFile? profilePicture)
        {
            // نزيل التحقق من الحقول التي تدار بواسطة Identity مباشرة
            ModelState.Remove("UserName");
            ModelState.Remove("Email");
            ModelState.Remove("PasswordHash");

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userService.GetUserByIdAsync(model.Id);
                    if (user == null) return NotFound();

                    // تحديث الحقول المسموح بتعديلها في البروفايل
                    user.FullName = model.FullName;
                    user.PhoneNumber = model.PhoneNumber;
                    user.Bio = model.Bio;
                    user.Address = model.Address;
                    user.City = model.City;
                    user.Country = model.Country;
                    user.DateOfBirth = model.DateOfBirth;

                    // حفظ الصورة لو تم رفعها
                    if (profilePicture != null && profilePicture.Length > 0)
                    {
                        var picturePath = await _userService.SaveProfilePictureAsync(profilePicture, model.Id);
                        user.ProfilePicturePath = picturePath;
                    }

                    await _userService.UpdateUserAsync(user);

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
        public async Task<IActionResult> ChangePassword(string userId, string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "كلمة المرور الجديدة وتأكيدها غير متطابقين";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "المستخدم غير موجود";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "تم تغيير كلمة المرور بنجاح!";
            }
            else
            {
                TempData["ErrorMessage"] = string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }
    }
}