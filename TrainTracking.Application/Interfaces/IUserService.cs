using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using TrainTracking.Domain.Entities;

namespace TrainTracking.Application.Interfaces
{
    public interface IUserService
    {
        Task<ApplicationUser?> GetUserByIdAsync(string id);
        Task<ApplicationUser?> GetUserByEmailAsync(string email);
        Task<ApplicationUser> UpdateUserAsync(ApplicationUser user);
        Task<string?> SaveProfilePictureAsync(IFormFile file, string userId, string? oldFilePath);
        Task<int> GetTotalUsersCountAsync();
    }
}
