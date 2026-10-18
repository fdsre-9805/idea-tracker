using IdeaJournal.Models;
using Npgsql;

namespace IdeaJournal.Data;

public class IdeaRepository(string connectionString)
{
    private NpgsqlConnection Open()
    {
        var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        return connection;
    }

    // В Compose база может ещё инициализироваться, когда API уже стартовал, — пробуем несколько раз
    public void EnsureCreated(ILogger logger, int attempts = 10)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var connection = Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = """
                    CREATE TABLE IF NOT EXISTS ideas (
                        id         BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                        title      TEXT NOT NULL,
                        status     TEXT NOT NULL DEFAULT 'New',
                        created_at TIMESTAMPTZ NOT NULL DEFAULT now()
                    )
                    """;
                cmd.ExecuteNonQuery();
                logger.LogInformation("PostgreSQL: подключение есть, таблица ideas готова");
                return;
            }
            catch (NpgsqlException ex) when (attempt < attempts)
            {
                logger.LogWarning("PostgreSQL недоступна (попытка {Attempt}/{Attempts}): {Message}", attempt, attempts, ex.Message);
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
        }
    }

    public List<Idea> GetAll()
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        // Сначала открытые (New), внутри группы — новые сверху
        cmd.CommandText = """
            SELECT id, title, status, created_at
            FROM ideas
            ORDER BY status = 'New' DESC, id DESC
            """;
        using var reader = cmd.ExecuteReader();
        var ideas = new List<Idea>();
        while (reader.Read())
        {
            ideas.Add(Read(reader));
        }
        return ideas;
    }

    public Idea? GetById(long id)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id, title, status, created_at FROM ideas WHERE id = $1";
        cmd.Parameters.AddWithValue(id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? Read(reader) : null;
    }

    public Idea Add(string title)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO ideas (title) VALUES ($1) RETURNING id, title, status, created_at";
        cmd.Parameters.AddWithValue(title);
        using var reader = cmd.ExecuteReader();
        reader.Read();
        return Read(reader);
    }

    // null — идеи с таким id нет
    public Idea? Update(long id, string title, string status)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE ideas SET title = $1, status = $2 WHERE id = $3
            RETURNING id, title, status, created_at
            """;
        cmd.Parameters.AddWithValue(title);
        cmd.Parameters.AddWithValue(status);
        cmd.Parameters.AddWithValue(id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? Read(reader) : null;
    }

    public bool Delete(long id)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM ideas WHERE id = $1";
        cmd.Parameters.AddWithValue(id);
        return cmd.ExecuteNonQuery() == 1;
    }

    // true — статус изменён; false — идеи нет или она уже закрыта
    public bool SetStatus(long id, string status)
    {
        using var connection = Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE ideas SET status = $1 WHERE id = $2 AND status = 'New'";
        cmd.Parameters.AddWithValue(status);
        cmd.Parameters.AddWithValue(id);
        return cmd.ExecuteNonQuery() == 1;
    }

    private static Idea Read(NpgsqlDataReader reader) =>
        new(reader.GetInt64(0), reader.GetString(1), reader.GetString(2),
            reader.GetFieldValue<DateTime>(3).ToUniversalTime().ToString("o"));
}
