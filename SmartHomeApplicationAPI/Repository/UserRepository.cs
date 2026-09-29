using Microsoft.EntityFrameworkCore;
using SmartHomeApplicationAPI.Infrastructure.Data;
using Utilities.Models;

namespace SmartHomeApplicationAPI.Repository
{
    public interface IUserRepository
    {
        Task<List<User>> GetAllUsersAsync();
    }

    public class UserRepository : IUserRepository
    {
        private readonly SmartHomeDbContext _context;

        public UserRepository(SmartHomeDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }
    }
}