using InterviewWizard.Models.User;
using Microsoft.EntityFrameworkCore;

namespace InterviewWizard.Helpers
{
    public class UserService
    {
        private ApplicationDbContext? _applicationDbContext;

        public UserService(ApplicationDbContext context)
        {
            _applicationDbContext = context;
        }

        public async Task<User> GetUserAsync(string userId)
        {
            var userGUID = new Guid(userId);
            User thisUser = await _applicationDbContext.Users.FindAsync(userGUID);
            if (thisUser == null)
            {
                throw new Exception("User not found");
            }
            return thisUser;
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            User thisUser = await _applicationDbContext.Users.FirstAsync(u => u.Email == email);
            if (thisUser == null)
            {
                throw new Exception("User not found");
            }
            return thisUser;
        }

        public async Task CreateUserAsync(User user)
        {
            _applicationDbContext.Users.Add(user);
            await _applicationDbContext.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            _applicationDbContext.Users.Update(user);
            await _applicationDbContext.SaveChangesAsync();
        }

        public void UpdateUser(User user)
        {
            _applicationDbContext.Users.Update(user);
            _applicationDbContext.SaveChanges();
        }

        public async Task DeleteUserAsync(User user)
        {
            _applicationDbContext.Users.Remove(user);
            await _applicationDbContext.SaveChangesAsync();
        }

        public async Task<bool> VerifyUserAsync(string email, string password)
        {
            User thisUser = await _applicationDbContext.Users.FirstAsync(u => u.Email == email);
            if (thisUser == null)
            {
                throw new Exception("User not found");
            }
            return await thisUser.VerifyPassword(password);
        }
    }
}
