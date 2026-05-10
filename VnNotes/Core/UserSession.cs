using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VnNotes
{
    /// <summary>
    /// Хранит данные пользователя, который успешно прошел авторизацию.
    /// </summary>
    public sealed class UserSession
    {
        public int Id { get; private set; }
        public string Username { get; private set; }
        public string Role { get; private set; }

        public UserSession(int id, string username, string role)
        {
            Id = id;
            Username = username;
            Role = role;
        }
    }
}
