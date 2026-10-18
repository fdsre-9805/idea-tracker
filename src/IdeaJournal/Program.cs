using IdeaJournal.Data;

var builder = WebApplication.CreateBuilder(args);

// Путь к БД можно переопределить переменной окружения DB_PATH (пригодится в Docker)
var dbPath = builder.Configuration["DB_PATH"] ?? "ideas.db";
var repository = new IdeaRepository($"Data Source={dbPath}");
repository.EnsureCreated();
builder.Services.AddSingleton(repository);

var app = builder.Build();

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
