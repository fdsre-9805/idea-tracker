# Журнал идей (idea-tracker)

Проектная тема: **журнал идей**.

Веб-приложение для записи идей. Каждая идея хранится в базе данных и имеет судьбу:
её можно отметить как **реализованную** или **отклонить**. Отклонённые идеи не удаляются —
журнал хранит всю историю.

**Статус:** готово — CRUD идей, кнопки «Реализовано» и «Отклонить», запуск API + PostgreSQL через Docker Compose

## Стек

- C# / .NET 10
- ASP.NET Core Minimal API
- PostgreSQL 16 (Npgsql)
- Docker, Docker Compose
- HTML + JavaScript (одна страница)

## Файлы

- `project-notes.md` — заметки по проекту
- `api-plan.md` — описание API
- `src/IdeaJournal/` — код приложения
- `src/IdeaJournal/Dockerfile` — сборка Docker-образа API
- `docker-compose.yml` — API + PostgreSQL одной командой
- `docs/compose/` — вывод команд проверки (КТ «Compose»)
- `docs/docker/` — вывод команд проверки одного контейнера (КТ «Контейнер», ещё на SQLite)

## Запуск через Docker Compose (основной способ)

Нужен только Docker с плагином Compose — .NET и PostgreSQL ставить не надо.

```bash
git clone https://github.com/fdsre-9805/idea-tracker.git
cd idea-tracker
docker compose up -d --build
```

Открыть http://localhost:8080 — страница «Журнал идей». API — `http://localhost:8080/api/ideas`.

### docker-compose.yml

| Что | Значение | Зачем |
|-----|----------|-------|
| сервис `api` | `build: ./src/IdeaJournal` | образ API собирается из нашего Dockerfile |
| сервис `db` | `image: postgres:16` | PostgreSQL отдельным контейнером |
| `ports` | `"8080:8080"` у `api` | порт_хоста:порт_контейнера; у `db` порт наружу не открыт — база нужна только API |
| `environment` у `api` | `CONNECTION_STRING=Host=db;Port=5432;Database=app;Username=postgres;Password=1234` | строка подключения; `Host=db` — имя сервиса в сети Compose, а не `localhost` |
| `environment` у `db` | `POSTGRES_USER=postgres`, `POSTGRES_PASSWORD=1234`, `POSTGRES_DB=app` | пользователь, пароль и база создаются при первом запуске |
| `depends_on` | `db: condition: service_healthy` | `api` стартует, когда база **готова** (проверка `pg_isready`), а не просто запущена |
| `healthcheck` у `db` | `pg_isready -U postgres -d app` | Compose понимает, что PostgreSQL принимает подключения |
| `networks` | `idea-net` | общая сеть: `api` находит `db` по имени |
| `volumes` | `db_data:/var/lib/postgresql/data` + блок `volumes: db_data:` внизу | данные базы живут в named volume и переживают `docker compose down` |
| `restart` | `unless-stopped` | контейнер поднимается сам после падения |

Дополнительно API при старте пробует подключиться к базе до 10 раз с паузой 2 с — на случай, если её всё равно не успели подготовить.

### Dockerfile API

Два этапа: в образе с SDK проект собирается, во второй образ (только runtime) копируются готовые файлы — итоговый образ меньше и без компилятора.

| Инструкция | Зачем |
|------------|-------|
| `FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build` | базовый образ с .NET SDK для сборки |
| `WORKDIR /src` | рабочая папка внутри контейнера |
| `COPY IdeaJournal.csproj .` + `RUN dotnet restore` | сначала только проект и зависимости — слой кешируется |
| `COPY . .` | исходный код (без `bin/`, `obj/` — см. `.dockerignore`) |
| `RUN dotnet publish -c Release -o /app/publish` | сборка приложения |
| `FROM mcr.microsoft.com/dotnet/aspnet:10.0` | образ только с runtime ASP.NET Core |
| `COPY --from=build /app/publish .` | готовые файлы из первого этапа |
| `ENV ASPNETCORE_HTTP_PORTS=8080` | приложение слушает порт 8080 |
| `EXPOSE 8080` | порт приложения |
| `ENTRYPOINT ["dotnet", "IdeaJournal.dll"]` | команда запуска — контейнер = приложение |

### CRUD — примеры curl

| Операция | Запрос |
|----------|--------|
| Create | `curl -X POST http://localhost:8080/api/ideas -H 'Content-Type: application/json' -d '{"title":"Бот с шрифтом Impact"}'` |
| Read (все) | `curl http://localhost:8080/api/ideas` |
| Read (одна) | `curl http://localhost:8080/api/ideas/1` |
| Update | `curl -X PUT http://localhost:8080/api/ideas/1 -H 'Content-Type: application/json' -d '{"title":"Бот с шрифтом Impact для Telegram","status":"Implemented"}'` |
| Delete | `curl -i -X DELETE http://localhost:8080/api/ideas/3` |

Полное описание ответов — в [api-plan.md](api-plan.md).

### Команды проверки

```bash
docker compose ps                 # оба сервиса Up, db — (healthy)
docker compose logs api           # подключение к PostgreSQL и строка на каждый запрос
docker compose logs db
docker compose down               # удалить контейнеры и сеть; volume db_data остаётся
docker compose up -d              # поднять снова — идеи на месте
docker compose exec -T db psql -U postgres -d app -c "SELECT id, title, status FROM ideas ORDER BY id"
```

> `docker compose down -v` удаляет ещё и volume — все идеи пропадут.

### Результат проверки (2026-10-18)

Картинки всех шагов — в [docs/compose](docs/compose).

`docker compose ps`:

```
NAME               IMAGE              COMMAND                  SERVICE   CREATED         STATUS                   PORTS
idea-journal-api   idea-tracker-api   "dotnet IdeaJournal.…"   api       5 seconds ago   Up 3 seconds             0.0.0.0:8080->8080/tcp, [::]:8080->8080/tcp
idea-journal-db    postgres:16        "docker-entrypoint.s…"   db        5 seconds ago   Up 5 seconds (healthy)   5432/tcp
```

CRUD:

```
$ curl -s -X POST http://localhost:8080/api/ideas -H 'Content-Type: application/json' -d '{"title":"Бот с шрифтом Impact"}'
{"id":1,"title":"Бот с шрифтом Impact","status":"New","createdAt":"2026-10-18T01:48:21.0542550Z"}
$ curl -s http://localhost:8080/api/ideas/1
{"id":1,"title":"Бот с шрифтом Impact","status":"New","createdAt":"2026-10-18T01:48:21.0542550Z"}
$ curl -s -X PUT http://localhost:8080/api/ideas/1 -H 'Content-Type: application/json' -d '{"title":"Бот с шрифтом Impact для Telegram","status":"Implemented"}'
{"id":1,"title":"Бот с шрифтом Impact для Telegram","status":"Implemented","createdAt":"2026-10-18T01:48:21.0542550Z"}
$ curl -s -i -X DELETE http://localhost:8080/api/ideas/3 | head -1
HTTP/1.1 204 No Content
$ curl -s -i http://localhost:8080/api/ideas/3 | head -1
HTTP/1.1 404 Not Found
```

Сохранение данных после перезапуска:

```
$ docker compose down
 ...
 Container idea-journal-db  Removed
 Network idea-tracker_idea-net  Removed
$ docker volume ls --filter name=db_data
DRIVER    VOLUME NAME
local     idea-tracker_db_data
$ docker compose up -d
 ...
$ docker compose exec -T db psql -U postgres -d app -c "SELECT id, title, status FROM ideas ORDER BY id"
 id |               title               |   status
----+-----------------------------------+-------------
  1 | Бот с шрифтом Impact для Telegram | Implemented
  2 | Плагин для терминала              | New
(2 rows)
```

`docker compose logs api` (до перезапуска):

```
idea-journal-api  | info: IdeaJournal[0]
idea-journal-api  |       PostgreSQL: подключение есть, таблица ideas готова
...
idea-journal-api  |       POST /api/ideas: добавлена идея 1 «Бот с шрифтом Impact»
idea-journal-api  |       GET /api/ideas/1: найдена
idea-journal-api  |       PUT /api/ideas/1: «Бот с шрифтом Impact для Telegram», Implemented
idea-journal-api  |       DELETE /api/ideas/3: удалена
idea-journal-api  |       GET /api/ideas/3: не найдена
```

## Запуск без Docker

Нужны .NET SDK 10 и PostgreSQL. Строку подключения передать переменной `CONNECTION_STRING`
(по умолчанию — `Host=localhost;Port=5432;Database=app;Username=postgres;Password=1234`):

```bash
CONNECTION_STRING="Host=localhost;Port=5432;Database=app;Username=postgres;Password=1234" \
  dotnet run --project src/IdeaJournal --urls http://localhost:5000
```

Открыть http://localhost:5000

## Git workflow

| Ветка | Зачем |
|-------|-------|
| `feature/db` | модель `Idea` и хранилище SQLite |
| `feature/ui` | страница: поле ввода и список идей |
| `feature/actions` | кнопки «Реализовано» / «Отклонить» и их API |
| `docs/readme` | инструкция запуска |
| `feature/docker` | Dockerfile, логи запросов, раздел «Docker» в README |
| `feature/compose` | PostgreSQL вместо SQLite, CRUD, `docker-compose.yml` |

Все ветки слиты в `main` через `merge --no-ff`.
Конфликт при слиянии `docs/readme` описан в [git-conflict.md](git-conflict.md).

## Граф веток

Живой граф: https://github.com/fdsre-9805/idea-tracker/network
