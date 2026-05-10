# VnNotes — консольная система заметок и мониторинга

VnNotes — консольное приложение для ведения служебных заметок в ИТ-инфраструктуре VPN-сервиса.

Приложение разработано на C# и использует PostgreSQL для хранения пользователей, ролей, заметок, статистики нагрузки и журнала безопасности.

Система предназначена для запуска на устройствах ИТ-инфраструктуры: серверах приложений, серверах баз данных, шлюзах, рабочих станциях и других узлах, на которых требуется быстро создать служебную заметку, посмотреть состояние системы или проверить события безопасности.

---

## Возможности

- авторизация пользователей;
- роли пользователей: `admin`, `operator`, `user`;
- MD5-хеширование паролей;
- создание заметок;
- просмотр заметок;
- удаление заметок;
- создание пользователей администратором;
- разблокировка пользователей;
- регистрация узлов инфраструктуры;
- просмотр статистики CPU, RAM и HDD;
- сохранение статистики в PostgreSQL;
- просмотр журнала безопасности;
- проверка обновлений через GitHub;
- загрузка обновлений;
- применение обновлений через отдельный updater;
- создание Markdown-карты команд.

---

## Используемые технологии

- C#;
- .NET Framework;
- PostgreSQL;
- Npgsql;
- Git;
- GitHub;
- GitHub Releases;
- Visual Studio.

---

## Роли пользователей

| Роль | Возможности |
|---|---|
| `admin` | Полный доступ к системе. Может создавать пользователей, просматривать журнал безопасности, проверять и применять обновления |
| `operator` | Может работать с заметками, регистрировать узлы инфраструктуры и сохранять статистику |
| `user` | Может создавать, просматривать и удалять свои заметки |

---

## Структура проекта

```text
VnNotes/
├── Core/
│   ├── UserSession.cs
│   ├── UpdateInfo.cs
│   └── SystemMetric.cs
│
├── Infrastructure/
│   ├── AppConfig.cs
│   ├── CommandReader.cs
│   ├── Md5Hasher.cs
│   └── RoleGuard.cs
│
├── Services/
│   ├── DatabaseService.cs
│   ├── SecurityLogService.cs
│   ├── AuthService.cs
│   ├── UserService.cs
│   ├── NoteService.cs
│   ├── SystemMetricService.cs
│   ├── UpdateService.cs
│   └── CommandMapService.cs
│
├── database/
│   └── init.sql
│
├── App.config
├── Program.cs
├── packages.config
├── README.md
├── COMMANDS.md
├── version.json
├── .gitignore
└── VnNotes.csproj
```

---

## Назначение основных файлов

| Файл | Назначение |
|---|---|
| `Program.cs` | Главная точка входа в приложение, обработка консольных команд |
| `App.config` | Хранение строки подключения к PostgreSQL, версии приложения и ссылки на `version.json` |
| `database/init.sql` | SQL-скрипт для создания таблиц базы данных |
| `UserSession.cs` | Хранение данных авторизованного пользователя |
| `AppConfig.cs` | Загрузка настроек приложения |
| `CommandReader.cs` | Разбор аргументов командной строки |
| `Md5Hasher.cs` | MD5-хеширование паролей |
| `RoleGuard.cs` | Проверка прав пользователя по роли |
| `DatabaseService.cs` | Инициализация таблиц PostgreSQL |
| `AuthService.cs` | Авторизация пользователей |
| `UserService.cs` | Создание, просмотр и разблокировка пользователей |
| `NoteService.cs` | Создание, просмотр и удаление заметок |
| `SystemMetricService.cs` | Получение и сохранение статистики CPU, RAM и HDD |
| `SecurityLogService.cs` | Запись и просмотр журнала безопасности |
| `UpdateService.cs` | Проверка, загрузка и применение обновлений |
| `CommandMapService.cs` | Вывод справки и создание Markdown-карты команд |

---

## Настройка PostgreSQL

Перед запуском приложения необходимо создать базу данных и отдельного пользователя для программы.

Создание базы данных:

```sql
CREATE DATABASE vn_notes_db;
```

Создание пользователя приложения:

```sql
CREATE ROLE vn_app WITH LOGIN PASSWORD 'vn_password';
```

Выдача права на подключение к базе:

```sql
GRANT CONNECT ON DATABASE vn_notes_db TO vn_app;
```

После этого нужно подключиться к базе `vn_notes_db` и выдать права на схему `public`:

```sql
GRANT USAGE, CREATE ON SCHEMA public TO vn_app;
```

---

## Строка подключения

В файле `App.config` указывается строка подключения к PostgreSQL:

```xml
<add key="ConnectionString" value="Host=localhost;Port=5432;Database=vn_notes_db;Username=vn_app;Password=vn_password" />
```

Также в `App.config` хранится текущая версия приложения:

```xml
<add key="ApplicationVersion" value="1.0.0" />
```

И ссылка на файл версии в GitHub:

```xml
<add key="UpdateInfoUrl" value="https://raw.githubusercontent.com/FiLLLLiN/VnNotes/main/version.json" />
```

---

## Таблицы базы данных

Программа использует следующие таблицы:

| Таблица | Назначение |
|---|---|
| `app_users` | Хранение пользователей, ролей, MD5-хешей паролей и признака блокировки |
| `notes` | Хранение заметок пользователей |
| `infrastructure_nodes` | Хранение узлов ИТ-инфраструктуры |
| `system_metrics` | Хранение статистики CPU, RAM и HDD |
| `security_logs` | Хранение журнала безопасности |

Таблицы создаются автоматически при запуске команды:

```bash
vn --db-init
```

---

## Команды без авторизации

Показать справку:

```bash
vn --help
```

Показать текущую версию приложения:

```bash
vn --version
```

Создать таблицы PostgreSQL:

```bash
vn --db-init
```

Проверить подключение к PostgreSQL:

```bash
vn --db-check
```

Создать Markdown-карту команд:

```bash
vn --map
```

---

## Команды с авторизацией

Создание заметки:

```bash
vn --login admin admin123 --addNewNote "Текст заметки"
```

Просмотр заметок:

```bash
vn --login admin admin123 --notes
```

Удаление заметки:

```bash
vn --login admin admin123 --deleteNote 1
```

Создание пользователя:

```bash
vn --login admin admin123 --createUser student 12345 user
```

Просмотр пользователей:

```bash
vn --login admin admin123 --users
```

Разблокировка пользователя:

```bash
vn --login admin admin123 --unlockUser student
```

Регистрация узла инфраструктуры:

```bash
vn --login operator operator123 --registerNode app-server-1 10.0.0.10 "Сервер приложений"
```

Просмотр текущей статистики CPU, RAM и HDD:

```bash
vn --login operator operator123 --metrics
```

Сохранение статистики текущего устройства:

```bash
vn --login operator operator123 --saveMetrics
```

Просмотр последней статистики по узлам:

```bash
vn --login operator operator123 --metrics-list
```

Просмотр журнала безопасности:

```bash
vn --login admin admin123 --logs 20
```

Проверка обновлений:

```bash
vn --login admin admin123 --update-check
```

Загрузка обновления:

```bash
vn --login admin admin123 --update-download
```

Применение обновления:

```bash
vn --login admin admin123 --update-apply
```

---

## Пользователи по умолчанию

После выполнения команды `vn --db-init` в базе создаются стандартные пользователи:

| Логин | Пароль | Роль |
|---|---|---|
| `admin` | `admin123` | `admin` |
| `operator` | `operator123` | `operator` |
| `user` | `user123` | `user` |

Пароли в базе данных хранятся не в открытом виде, а в виде MD5-хеша.

---

## Хеширование паролей

Для хранения паролей используется MD5-хеширование.

При создании пользователя программа формирует строку:

```text
username:password
```

После этого строка преобразуется в MD5-хеш и сохраняется в таблицу `app_users`.

При авторизации введенный пароль снова хешируется, после чего полученный результат сравнивается с хешем, сохраненным в PostgreSQL.

---

## Журнал безопасности

В журнал безопасности записываются важные действия пользователей:

- успешная авторизация;
- неудачная попытка входа;
- блокировка пользователя после нескольких ошибок входа;
- создание заметки;
- удаление заметки;
- создание пользователя;
- разблокировка пользователя;
- регистрация узла инфраструктуры;
- просмотр статистики;
- сохранение статистики;
- просмотр логов;
- проверка обновлений;
- загрузка обновлений;
- применение обновлений.

Журнал хранится в таблице:

```text
security_logs
```

---

## Обновления через GitHub

Для проверки обновлений используется файл `version.json`, который размещается в корне GitHub-репозитория.

Пример файла `version.json`:

```json
{
  "version": "1.0.1",
  "downloadUrl": "https://github.com/FiLLLLiN/VnNotes/releases/download/v1.0.1/VnNotes-v1.0.1.zip"
}
```

Приложение сравнивает текущую версию из `App.config`:

```xml
<add key="ApplicationVersion" value="1.0.0" />
```

с версией из `version.json`:

```json
"version": "1.0.1"
```

Если версия в GitHub больше, программа сообщает о наличии обновления.

---

## GitHub Releases

Архив новой версии программы должен быть загружен в раздел GitHub Releases.

Название архива:

```text
VnNotes-v1.0.1.zip
```

Ссылка на него указывается в `version.json`:

```text
https://github.com/FiLLLLiN/VnNotes/releases/download/v1.0.1/VnNotes-v1.0.1.zip
```

---

## Git

Разработка проекта ведется с использованием Git.

Инициализация репозитория:

```bash
git init
```

Добавление файлов:

```bash
git add .
```

Создание коммита:

```bash
git commit -m "init: создана консольная система заметок"
```

Переименование ветки в `main`:

```bash
git branch -M main
```

Подключение удаленного репозитория:

```bash
git remote add origin https://github.com/FiLLLLiN/VnNotes.git
```

Загрузка проекта на GitHub:

```bash
git push -u origin main
```

---

## Пример истории коммитов

```text
init: создана структура консольного приложения
feat: добавлена конфигурация PostgreSQL
feat: добавлена авторизация пользователей
feat: добавлена работа с заметками
feat: добавлена статистика CPU RAM HDD
feat: добавлена проверка обновлений через GitHub
docs: добавлена инструкция README
```

---

## Что не загружается в GitHub

В GitHub не загружаются временные и служебные файлы:

```text
bin/
obj/
.vs/
updates/
*.user
*.suo
*.log
```

Эти правила задаются в файле `.gitignore`.

---

## Работа

Учебный проект по дисциплине «Тестирование и отладка программного обеспечения».