using System.Collections.Generic;
using System.Threading.Tasks;
using TrainTracking.Domain.Entities;

namespace TrainTracking.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<ApplicationUser?> GetByIdAsync(string id);
        Task<ApplicationUser?> GetByEmailAsync(string email);
        Task<ApplicationUser?> GetByUsernameAsync(string username);
        Task<IEnumerable<ApplicationUser>> GetAllAsync();
        Task<ApplicationUser> UpdateAsync(ApplicationUser user);
        Task<bool> DeleteAsync(string id);
        Task<int> GetCountAsync();
    }
}
