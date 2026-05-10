using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VnNotes
{
    /// <summary>
    /// Проверяет права пользователя по роли.
    /// </summary>
    public static class RoleGuard
    {
        public static bool IsAdmin(UserSession user)
        {
            return string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase);
        }

        public static bool HasAnyRole(UserSession user, params string[] roles)
        {
            foreach (string role in roles)
            {
                if (string.Equals(user.Role, role, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
