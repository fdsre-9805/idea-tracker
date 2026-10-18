# API — Журнал идей

Базовый адрес: `http://localhost:8080` (Docker Compose) или `http://localhost:5000` (`dotnet run`)
Формат: JSON

## Модель

| Поле | Тип | Описание |
|------|-----|----------|
| id | число | номер идеи |
| title | строка | текст идеи |
| status | строка | `New`, `Implemented`, `Rejected` |
| createdAt | строка | дата создания (ISO 8601, UTC) |

## Эндпоинты

### GET /api/ideas
Список всех идей. Сначала открытые (`New`), потом закрытые; новые сверху.

Ответ `200 OK`:

```json
[
  { "id": 2, "title": "Бот с шрифтом Impact", "status": "New", "createdAt": "2026-09-12T10:00:00.0000000Z" },
  { "id": 1, "title": "Плагин для терминала", "status": "Rejected", "createdAt": "2026-09-11T09:00:00.0000000Z" }
]
```

### POST /api/ideas
Добавить идею.

Тело запроса:

```json
{ "title": "Бот с шрифтом Impact" }
```

Ответы:
- `201 Created` — идея создана, в теле — созданная идея
- `400 Bad Request` — пустой `title`

### GET /api/ideas/{id}
Одна идея.

Ответы:
- `200 OK` — идея
- `404 Not Found` — такой нет

### PUT /api/ideas/{id}
Изменить текст и статус идеи.

Тело запроса:

```json
{ "title": "Бот с шрифтом Impact v2", "status": "Implemented" }
```

Ответы:
- `200 OK` — в теле обновлённая идея
- `400 Bad Request` — пустой `title` или статус не из `New`, `Implemented`, `Rejected`
- `404 Not Found` — такой идеи нет

### DELETE /api/ideas/{id}
Удалить идею (например, записанную по ошибке).

Ответы:
- `204 No Content` — удалена
- `404 Not Found` — такой нет

### POST /api/ideas/{id}/implement
Кнопка «Реализовано». Меняет статус `New` → `Implemented`.

Ответы:
- `204 No Content` — готово
- `409 Conflict` — идеи нет или она уже закрыта

### POST /api/ideas/{id}/reject
Кнопка «Отклонить». Меняет статус `New` → `Rejected`.

Ответы:
- `204 No Content` — готово
- `409 Conflict` — идеи нет или она уже закрыта

## DELETE и «Отклонить»
Отклонённая идея — тоже результат, поэтому для «не буду делать» есть `reject`: идея остаётся в журнале.
`DELETE` — для ошибочных записей (дубль, опечатка); добавлен в КТ «Compose» как часть CRUD.

## План развития
- `GET /api/ideas?status=New` — фильтр по статусу
- теги/категории идей
