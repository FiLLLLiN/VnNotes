using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace VnUpdater
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                string packagePath = GetValue(args, "--package");
                string targetPath = GetValue(args, "--target");
                string waitText = GetValue(args, "--wait");

                if (string.IsNullOrWhiteSpace(packagePath))
                {
                    Console.WriteLine("Не указан пакет обновления.");
                    return 1;
                }

                if (string.IsNullOrWhiteSpace(targetPath))
                {
                    Console.WriteLine("Не указан каталог установки.");
                    return 1;
                }

                int wait;

                if (int.TryParse(waitText, out wait) && wait > 0)
                    Thread.Sleep(wait);

                if (!File.Exists(packagePath))
                {
                    Console.WriteLine("Пакет обновления не найден:");
                    Console.WriteLine(packagePath);
                    return 1;
                }

                if (!Directory.Exists(targetPath))
                    Directory.CreateDirectory(targetPath);

                ZipFile.ExtractToDirectory(packagePath, targetPath);

                Console.WriteLine("Обновление успешно применено.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка установки обновления.");
                Console.WriteLine(ex.Message);
                return 1;
            }
        }

        private static string GetValue(string[] args, string key)
        {
            int index = Array.FindIndex(
                args,
                x => string.Equals(x, key, StringComparison.OrdinalIgnoreCase));

            if (index < 0 || index + 1 >= args.Length)
                return "";

            return args[index + 1];
        }
    }
}