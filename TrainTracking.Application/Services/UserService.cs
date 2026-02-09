using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using TrainTracking.Application.Interfaces;
using TrainTracking.Domain.Entities;

namespace TrainTracking.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
        {
            return await _userRepository.GetByEmailAsync(email);
        }

        public async Task<ApplicationUser> UpdateUserAsync(ApplicationUser user)
        {
            return await _userRepository.UpdateAsync(user);
        }

        public async Task<int> GetTotalUsersCountAsync()
        {
            return await _userRepository.GetCountAsync();
        }

        public async Task<string?> SaveProfilePictureAsync(IFormFile file, string userId, string? oldFilePath = null)
        {
            if (file == null || file.Length == 0)
                return null;

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // „”Õ «·’Ê—… «·ﬁœÌ„… ·Ê „ÊÃÊœ…
            if (!string.IsNullOrEmpty(oldFilePath))
            {
                var oldFileFullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldFilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (File.Exists(oldFileFullPath))
                {
                    File.Delete(oldFileFullPath);
                }
            }

            var uniqueFileName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/profiles/{uniqueFileName}";
        }

    }
}
