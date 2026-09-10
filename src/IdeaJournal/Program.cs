using IdeaJournal.Data;

var builder = WebApplication.CreateBuilder(args);

// Путь к БД можно переопределить переменной окружения DB_PATH (пригодится в Docker)
var dbPath = builder.Configuration["DB_PATH"] ?? "ideas.db";
var repository = new IdeaRepository($"Data Source={dbPath}");
repository.EnsureCreated();
builder.Services.AddSingleton(repository);

var app = builder.Build();

app.Run();
