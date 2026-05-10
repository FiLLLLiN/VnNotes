using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace VnNotes
{
    /// <summary>
    /// Выводит справку и создает Markdown-карту команд.
    /// </summary>
    public sealed class CommandMapService
    {
        public void PrintHelp()
        {
            Console.WriteLine("VN Notes — консольная система заметок и мониторинга");
            Console.WriteLine();

            Console.WriteLine("Команды без авторизации:");
            Console.WriteLine("vn --help");
            Console.WriteLine("vn --version");
            Console.WriteLine("vn --db-check");
            Console.WriteLine("vn --db-init");
            Console.WriteLine("vn --map");

            Console.WriteLine();

            Console.WriteLine("Команды с авторизацией:");
            Console.WriteLine("vn --login admin admin123 --addNewNote \"Текст заметки\"");
            Console.WriteLine("vn --login admin admin123 --notes");
            Console.WriteLine("vn --login admin admin123 --deleteNote 1");
            Console.WriteLine("vn --login admin admin123 --createUser student 12345 user");
            Console.WriteLine("vn --login admin admin123 --users");
            Console.WriteLine("vn --login admin admin123 --unlockUser student");
            Console.WriteLine("vn --login operator operator123 --registerNode app-server-1 10.0.0.10 \"Сервер приложений\"");
            Console.WriteLine("vn --login operator operator123 --metrics");
            Console.WriteLine("vn --login operator operator123 --saveMetrics");
            Console.WriteLine("vn --login operator operator123 --metrics-list");
            Console.WriteLine("vn --login admin admin123 --logs 20");
            Console.WriteLine("vn --login admin admin123 --update-check");
            Console.WriteLine("vn --login admin admin123 --update-download");
            Console.WriteLine("vn --login admin admin123 --update-apply");
        }

        public async Task CreateMarkdownAsync(string path)
        {
            string markdown =
@"# VN Notes

## Назначение

VN Notes — консольная система для создания заметок, авторизации пользователей, просмотра статистики CPU/RAM/HDD, ведения журнала безопасности и проверки обновлений через GitHub.

## Роли

| Роль | Возможности |
|---|---|
| admin | Полный доступ |
| operator | Заметки, узлы инфраструктуры, статистика |
| user | Работа со своими заметками |

## Команды

### Справка

```bash
vn --help
vn --version
vn --db-init
vn --db-check
vn --login admin admin123 --addNewNote ""Текст заметки""
vn --login admin admin123 --notes
vn --login admin admin123 --deleteNote 1
vn --login admin admin123 --createUser student 12345 user
vn --login admin admin123 --users
vn --login operator operator123 --registerNode app-server-1 10.0.0.10 ""Сервер приложений""
vn --login operator operator123 --metrics
vn --login operator operator123 --saveMetrics
vn --login operator operator123 --metrics-list
vn --login admin admin123 --logs 20
vn --login admin admin123 --update-check
vn --login admin admin123 --update-download
vn --login admin admin123 --update-apply
";
            File.WriteAllText(path, markdown, Encoding.UTF8);
        }
    }
}
