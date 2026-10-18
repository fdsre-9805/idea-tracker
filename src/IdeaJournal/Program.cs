using IdeaJournal.Data;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Строка подключения к PostgreSQL — из переменной окружения CONNECTION_STRING.
// В Compose там Host=db (имя сервиса), без Docker — значение по умолчанию на localhost.
var connectionString = builder.Configuration["CONNECTION_STRING"]
    ?? "Host=localhost;Port=5432;Database=app;Username=postgres;Password=1234";
// Kerberos не используем: без этого Npgsql ищет libgssapi_krb5, которой нет в образе, и пишет ошибку в лог
connectionString = new NpgsqlConnectionStringBuilder(connectionString) { GssEncryptionMode = GssEncryptionMode.Disable }.ConnectionString;
var repository = new IdeaRepository(connectionString);
builder.Services.AddSingleton(repository);

var app = builder.Build();
repository.EnsureCreated(app.Logger);

string[] statuses = ["New", "Implemented", "Rejected"];

app.UseDefaultFiles();   // / → index.html
app.UseStaticFiles();    // раздаёт wwwroot/

app.MapGet("/api/ideas", (IdeaRepository repo) =>
{
    var ideas = repo.GetAll();
    app.Logger.LogInformation("GET /api/ideas: идей в журнале — {Count}", ideas.Count);
    return ideas;
});

app.MapPost("/api/ideas", (NewIdea body, IdeaRepository repo) =>
{
    if (string.IsNullOrWhiteSpace(body.Title))
    {
        app.Logger.LogWarning("POST /api/ideas: пустая идея отклонена");
        return Results.BadRequest("Идея не может быть пустой");
    }
    var idea = repo.Add(body.Title.Trim());
    app.Logger.LogInformation("POST /api/ideas: добавлена идея {Id} «{Title}»", idea.Id, idea.Title);
    return Results.Created("/api/ideas", idea);
});

app.MapGet("/api/ideas/{id:long}", (long id, IdeaRepository repo) =>
{
    var idea = repo.GetById(id);
    app.Logger.LogInformation("GET /api/ideas/{Id}: {Result}", id, idea is null ? "не найдена" : "найдена");
    return idea is null ? Results.NotFound() : Results.Ok(idea);
});

app.MapPut("/api/ideas/{id:long}", (long id, UpdateIdea body, IdeaRepository repo) =>
{
    if (string.IsNullOrWhiteSpace(body.Title) || !statuses.Contains(body.Status))
    {
        app.Logger.LogWarning("PUT /api/ideas/{Id}: неверные данные", id);
        return Results.BadRequest($"Нужны title и status ({string.Join(", ", statuses)})");
    }
    var idea = repo.Update(id, body.Title.Trim(), body.Status);
    app.Logger.LogInformation("PUT /api/ideas/{Id}: {Result}", id, idea is null ? "не найдена" : $"«{idea.Title}», {idea.Status}");
    return idea is null ? Results.NotFound() : Results.Ok(idea);
});

app.MapDelete("/api/ideas/{id:long}", (long id, IdeaRepository repo) =>
{
    var deleted = repo.Delete(id);
    app.Logger.LogInformation("DELETE /api/ideas/{Id}: {Result}", id, deleted ? "удалена" : "не найдена");
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.MapPost("/api/ideas/{id:long}/implement", (long id, IdeaRepository repo) =>
    ChangeStatus(id, "Implemented", repo));

app.MapPost("/api/ideas/{id:long}/reject", (long id, IdeaRepository repo) =>
    ChangeStatus(id, "Rejected", repo));

app.Run();

// Общая логика кнопок «Реализовано» и «Отклонить» + строка в лог
IResult ChangeStatus(long id, string status, IdeaRepository repo)
{
    if (repo.SetStatus(id, status))
    {
        app.Logger.LogInformation("Идея {Id}: статус → {Status}", id, status);
        return Results.NoContent();
    }
    app.Logger.LogWarning("Идея {Id}: статус не изменён — идеи нет или она уже закрыта", id);
    return Results.Conflict();
}

record NewIdea(string Title);
record UpdateIdea(string Title, string Status);
