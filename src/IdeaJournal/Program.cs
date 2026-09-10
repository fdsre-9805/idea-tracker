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

app.MapGet("/api/ideas", (IdeaRepository repo) => repo.GetAll());

app.MapPost("/api/ideas", (NewIdea body, IdeaRepository repo) =>
    string.IsNullOrWhiteSpace(body.Title)
        ? Results.BadRequest("Идея не может быть пустой")
        : Results.Created("/api/ideas", repo.Add(body.Title.Trim())));

app.Run();

record NewIdea(string Title);
