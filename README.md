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

## Запуск

Нужен .NET SDK 10.

```bash
git clone https://github.com/fdsre-9805/idea-tracker.git
cd idea-tracker
dotnet run --project src/IdeaJournal --urls http://localhost:5000
```

Открыть http://localhost:5000
