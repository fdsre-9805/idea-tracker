using IdeaJournal.Models;
using Microsoft.Data.Sqlite;

namespace IdeaJournal.Data;

public class IdeaRepository(string connectionString)
{
    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    public void EnsureCreated()
    {
        using var connection = Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Ideas (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Title     TEXT NOT NULL,
                Status    TEXT NOT NULL DEFAULT 'New',
                CreatedAt TEXT NOT NULL
            )
            """;
        cmd.ExecuteNonQuery();
    }

    public List<Idea> GetAll()
    {
        using var connection = Open();
        var cmd = connection.CreateCommand();
        // Сначала открытые (New), внутри группы — новые сверху
        cmd.CommandText = """
            SELECT Id, Title, Status, CreatedAt
            FROM Ideas
            ORDER BY Status = 'New' DESC, Id DESC
            """;
        using var reader = cmd.ExecuteReader();
        var ideas = new List<Idea>();
        while (reader.Read())
        {
            ideas.Add(new Idea(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
        }
        return ideas;
    }

    public Idea Add(string title)
    {
        using var connection = Open();
        var createdAt = DateTime.UtcNow.ToString("o");
        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Ideas (Title, CreatedAt) VALUES ($title, $createdAt);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$title", title);
        cmd.Parameters.AddWithValue("$createdAt", createdAt);
        var id = (long)cmd.ExecuteScalar()!;
        return new Idea(id, title, "New", createdAt);
    }

    // true — статус изменён; false — идеи нет или она уже закрыта
    public bool SetStatus(long id, string status)
    {
        using var connection = Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE Ideas SET Status = $status WHERE Id = $id AND Status = 'New'";
        cmd.Parameters.AddWithValue("$status", status);
        cmd.Parameters.AddWithValue("$id", id);
        return cmd.ExecuteNonQuery() == 1;
    }
}
