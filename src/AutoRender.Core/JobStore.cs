using Microsoft.Data.Sqlite;
using System.Text.Json;
namespace AutoRender.Core;

public sealed class JobStore
{
    private readonly string connectionString;
    public JobStore(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS jobs(id INTEGER PRIMARY KEY, position INTEGER NOT NULL,
              name TEXT NOT NULL, output TEXT NOT NULL, state TEXT NOT NULL, message TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS metadata(key TEXT PRIMARY KEY, value TEXT NOT NULL);
            """;
        cmd.ExecuteNonQuery();
    }
    private SqliteConnection Open() { var db = new SqliteConnection(connectionString); db.Open(); return db; }
    public IReadOnlyList<Job> Read()
    {
        using var db = Open(); using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT id,position,name,output,state,message FROM jobs ORDER BY position";
        using var reader = cmd.ExecuteReader(); var jobs = new List<Job>();
        while (reader.Read()) jobs.Add(new(reader.GetInt64(0), reader.GetInt32(1), reader.GetString(2),
            reader.GetString(3), Enum.Parse<JobState>(reader.GetString(4)), reader.GetString(5)));
        return jobs;
    }
    public ExportSettings? Settings()
    {
        using var db = Open(); using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT value FROM metadata WHERE key='settings'";
        return cmd.ExecuteScalar() is string json ? JsonSerializer.Deserialize<ExportSettings>(json) : null;
    }
    public void Replace(IReadOnlyList<string> names, ExportSettings settings)
    {
        settings.Validate();
        using var db = Open(); using var tx = db.BeginTransaction(); using var cmd = db.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "DELETE FROM jobs"; cmd.ExecuteNonQuery();
        for (var i = 0; i < names.Count; i++)
        {
            cmd.Parameters.Clear();
            cmd.CommandText = "INSERT INTO jobs(position,name,output,state,message) VALUES($p,$n,$o,'Pending','')";
            cmd.Parameters.AddWithValue("$p", i + 1); cmd.Parameters.AddWithValue("$n", names[i]);
            cmd.Parameters.AddWithValue("$o", Path.Combine(settings.OutputDirectory, names[i] + ".mp4"));
            cmd.ExecuteNonQuery();
        }
        cmd.Parameters.Clear(); cmd.CommandText = "INSERT OR REPLACE INTO metadata VALUES('settings',$s)";
        cmd.Parameters.AddWithValue("$s", JsonSerializer.Serialize(settings)); cmd.ExecuteNonQuery(); tx.Commit();
    }
    public void Update(long id, JobState state, string message)
    {
        using var db = Open(); using var cmd = db.CreateCommand();
        cmd.CommandText = "UPDATE jobs SET state=$s,message=$m WHERE id=$i";
        cmd.Parameters.AddWithValue("$s", state.ToString()); cmd.Parameters.AddWithValue("$m", message);
        cmd.Parameters.AddWithValue("$i", id); cmd.ExecuteNonQuery();
    }
    public void RecoverInterrupted()
    {
        foreach (var job in Read().Where(j => j.State is JobState.Opening or JobState.Exporting))
            Update(job.Id, JobState.NeedsReview, "Phiên trước bị gián đoạn. Kiểm tra CapCut và file đầu ra trước khi chạy lại.");
    }
}
