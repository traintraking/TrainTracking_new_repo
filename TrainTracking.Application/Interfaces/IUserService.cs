using TrainTracking.Domain.Entities;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TrainTracking.Application.Interfaces
{
    public interface IUserService
    {
        Task<User> GetUserByIdAsync(int id);
        Task<User> GetUserByEmailAsync(string email);
        Task<User> CreateUserAsync(User user);
        Task<User> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int id);
        Task<string> SaveProfilePictureAsync(IFormFile file, int userId);
    }
}