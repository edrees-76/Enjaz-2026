using System;
using Enjaz.Models;

namespace Enjaz.Services
{
    public class UserService
    {
        private User? _currentUser;

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
    }
}
