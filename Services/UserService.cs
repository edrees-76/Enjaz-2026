using System;
using System.Threading.Tasks;
using Enjaz.Models;
using Enjaz.Services.Repositories;

namespace Enjaz.Services
{
    public class UserService
    {
        private User? _currentUser;
        private readonly UserRepository _userRepository;

        public UserService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public User? CurrentUser 
        { 
            get => _currentUser; 
            private set => _currentUser = value; 
        }

        public bool IsLoggedIn => _currentUser != null;

        public event Action<User?>? UserChanged;

        public void Login(User user)
        {
            _currentUser = user;
            UserChanged?.Invoke(user);
        }

        public void Logout()
        {
            _currentUser = null;
            UserChanged?.Invoke(null);
        }

        /// <summary>
        /// التحقق من كلمة مرور المستخدم الحالي مباشرة من قاعدة البيانات
        /// بدون الحاجة لتخزين الهاش في الذاكرة (Security: Anti-Memory-Dump)
        /// Verify current user's password directly from DB without keeping hash in memory
        /// </summary>
        public async Task<bool> VerifyCurrentUserPasswordAsync(string password)
        {
            if (_currentUser == null) return false;
            return await _userRepository.VerifyPasswordByUserIdAsync(_currentUser.Id, password);
        }
    }
}
