using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace VnNotes
{
    /// <summary>
    /// Выполняет MD5-хеширование пароля.
    /// </summary>
    public static class Md5Hasher
    {
        public static string HashPassword(string username, string password)
        {
            string rawValue = username + ":" + password;
            byte[] bytes = Encoding.UTF8.GetBytes(rawValue);

            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder();

                foreach (byte currentByte in hashBytes)
                    builder.Append(currentByte.ToString("x2"));

                return builder.ToString();
            }
        }
    }
}
