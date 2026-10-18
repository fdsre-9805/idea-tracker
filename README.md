# Журнал идей (idea-tracker)

Проектная тема: **журнал идей**.

Веб-приложение для записи идей. Каждая идея хранится в базе данных и имеет судьбу:
её можно отметить как **реализованную** или **отклонить**. Отклонённые идеи не удаляются —
журнал хранит всю историю.

**Статус:** готово — добавление идей, список, кнопки «Реализовано» и «Отклонить»

## Стек

- C# / .NET 10
- ASP.NET Core Minimal API
- SQLite
- HTML + JavaScript (одна страница)

## Файлы

- `project-notes.md` — заметки по проекту
- `api-plan.md` — описание API
- `src/IdeaJournal/` — код приложения
- `src/IdeaJournal/Dockerfile` — сборка Docker-образа
- `docs/docker/` — вывод команд проверки контейнера

## Запуск

Нужен .NET SDK 10.

```bash
git clone https://github.com/fdsre-9805/idea-tracker.git
cd idea-tracker
dotnet run --project src/IdeaJournal --urls http://localhost:5000
```

Открыть http://localhost:5000

## Docker

Нужен только Docker — .NET на компьютер ставить не надо, SDK и runtime уже в образе.

```bash
docker build -t idea-journal ./src/IdeaJournal
docker run -d -p 8080:8080 --name idea-journal idea-journal
```

Открыть http://localhost:8080. `-p 8080:8080` — порт 8080 компьютера → порт 8080 контейнера.

### Dockerfile

Два этапа: в образе с SDK проект собирается, во второй образ (только runtime) копируются готовые файлы — итоговый образ меньше и без компилятора.

| Инструкция | Зачем |
|------------|-------|
| `FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build` | базовый образ с .NET SDK для сборки |
| `WORKDIR /src` | рабочая папка внутри контейнера |
| `COPY IdeaJournal.csproj .` + `RUN dotnet restore` | сначала только проект и зависимости — слой кешируется |
| `COPY . .` | исходный код (без `bin/`, `obj/`, `*.db` — см. `.dockerignore`) |
| `RUN dotnet publish -c Release -o /app/publish` | сборка приложения |
| `FROM mcr.microsoft.com/dotnet/aspnet:10.0` | образ только с runtime ASP.NET Core |
| `COPY --from=build /app/publish .` | готовые файлы из первого этапа |
| `RUN mkdir -p /app/data` + `ENV DB_PATH=/app/data/ideas.db` | папка для базы SQLite |
| `ENV ASPNETCORE_HTTP_PORTS=8080` | приложение слушает порт 8080 |
| `EXPOSE 8080` | порт приложения |
| `ENTRYPOINT ["dotnet", "IdeaJournal.dll"]` | команда запуска — контейнер = приложение |

База живёт внутри контейнера: после `docker rm` идеи пропадут. Чтобы сохранить их, подключить volume:
`docker run -d -p 8080:8080 -v idea-data:/app/data --name idea-journal idea-journal`.

### Проверка

```bash
docker ps
curl -X POST http://localhost:8080/api/ideas -H 'Content-Type: application/json' -d '{"title":"Бот с шрифтом Impact"}'
curl -i -X POST http://localhost:8080/api/ideas/1/implement
curl http://localhost:8080/api/ideas
docker logs idea-journal
```

Результат (2026-10-18), картинки — в [docs/docker](docs/docker):

```
$ docker ps
CONTAINER ID   IMAGE          COMMAND                  CREATED         STATUS         PORTS                                         NAMES
4224e05b7524   idea-journal   "dotnet IdeaJournal.…"   4 seconds ago   Up 4 seconds   0.0.0.0:8080->8080/tcp, [::]:8080->8080/tcp   idea-journal

$ curl -X POST http://localhost:8080/api/ideas -H 'Content-Type: application/json' -d '{"title":"Бот с шрифтом Impact"}'
{"id":1,"title":"Бот с шрифтом Impact","status":"New","createdAt":"2026-10-18T00:46:17.7522977Z"}

$ curl -i -X POST http://localhost:8080/api/ideas/1/implement | head -1
HTTP/1.1 204 No Content

$ curl -i -X POST http://localhost:8080/api/ideas/1/reject | head -1
HTTP/1.1 409 Conflict

$ docker logs idea-journal
...
info: IdeaJournal[0]
      POST /api/ideas: добавлена идея 1 «Бот с шрифтом Impact»
info: IdeaJournal[0]
      Идея 1: статус → Implemented
warn: IdeaJournal[0]
      Идея 1: статус не изменён — идеи нет или она уже закрыта
```

Остановить и удалить контейнер: `docker stop idea-journal && docker rm idea-journal`.

## Git workflow

| Ветка | Зачем |
|-------|-------|
| `feature/db` | модель `Idea` и хранилище SQLite |
| `feature/ui` | страница: поле ввода и список идей |
| `feature/actions` | кнопки «Реализовано» / «Отклонить» и их API |
| `docs/readme` | инструкция запуска |
| `feature/docker` | Dockerfile, логи запросов, раздел «Docker» в README |

Все ветки слиты в `main` через `merge --no-ff`.
Конфликт при слиянии `docs/readme` описан в [git-conflict.md](git-conflict.md).

## Граф веток

Живой граф: https://github.com/fdsre-9805/idea-tracker/network
