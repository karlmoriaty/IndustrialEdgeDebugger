using Microsoft.Data.Sqlite;
using IndustrialEdgeDebugger.Comm.Models;
using System.Text;

namespace IndustrialEdgeDebugger.Comm.Services;

public class SqliteLogStorage : ILogStorage
{
    private readonly string _connectionString;
    private bool _disposed;

    public SqliteLogStorage(string dbPath = "logs.db")
    {
        _connectionString = $"Data Source={dbPath}";
    }

    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS DeviceLogs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp TEXT NOT NULL,
                DeviceId TEXT NOT NULL,
                Data TEXT NOT NULL,
                DataType TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_timestamp ON DeviceLogs(Timestamp);
            CREATE INDEX IF NOT EXISTS idx_deviceid ON DeviceLogs(DeviceId);
            CREATE INDEX IF NOT EXISTS idx_datatype ON DeviceLogs(DataType);
        ";
        command.ExecuteNonQuery();
    }

    public int AddLog(DeviceLog log)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO DeviceLogs (Timestamp, DeviceId, Data, DataType)
            VALUES ($timestamp, $deviceId, $data, $dataType);
            SELECT last_insert_rowid();
        ";
        command.Parameters.AddWithValue("$timestamp", log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        command.Parameters.AddWithValue("$deviceId", log.DeviceId);
        command.Parameters.AddWithValue("$data", log.Data);
        command.Parameters.AddWithValue("$dataType", log.DataType);

        var result = command.ExecuteScalar();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    public List<DeviceLog> GetLogs(DateTime? startTime = null, DateTime? endTime = null, string? deviceId = null, string? dataType = null, int? limit = null)
    {
        var logs = new List<DeviceLog>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        var queryBuilder = new StringBuilder("SELECT Id, Timestamp, DeviceId, Data, DataType FROM DeviceLogs WHERE 1=1");
        var conditions = new List<string>();

        if (startTime.HasValue)
        {
            conditions.Add("Timestamp >= $startTime");
            command.Parameters.AddWithValue("$startTime", startTime.Value.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        }

        if (endTime.HasValue)
        {
            conditions.Add("Timestamp <= $endTime");
            command.Parameters.AddWithValue("$endTime", endTime.Value.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        }

        if (!string.IsNullOrEmpty(deviceId))
        {
            conditions.Add("DeviceId = $deviceId");
            command.Parameters.AddWithValue("$deviceId", deviceId);
        }

        if (!string.IsNullOrEmpty(dataType))
        {
            conditions.Add("DataType = $dataType");
            command.Parameters.AddWithValue("$dataType", dataType);
        }

        if (conditions.Any())
        {
            queryBuilder.Append(" AND ").Append(string.Join(" AND ", conditions));
        }

        queryBuilder.Append(" ORDER BY Timestamp DESC");

        if (limit.HasValue)
        {
            queryBuilder.Append(" LIMIT $limit");
            command.Parameters.AddWithValue("$limit", limit.Value);
        }

        command.CommandText = queryBuilder.ToString();

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            logs.Add(new DeviceLog
            {
                Id = reader.GetInt32(0),
                Timestamp = DateTime.Parse(reader.GetString(1)),
                DeviceId = reader.GetString(2),
                Data = reader.GetString(3),
                DataType = reader.GetString(4)
            });
        }

        return logs;
    }

    public (List<DeviceLog> logs, int totalCount) GetLogsPaged(DateTime? startTime = null, DateTime? endTime = null, string? deviceId = null, string? dataType = null, string? keyword = null, int pageNumber = 1, int pageSize = 50)
    {
        var logs = new List<DeviceLog>();
        int totalCount = 0;

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var conditions = new List<string>();
        var parameters = new List<(string name, object value)>();

        if (startTime.HasValue)
        {
            conditions.Add("Timestamp >= $startTime");
            parameters.Add(("$startTime", startTime.Value.ToString("yyyy-MM-dd HH:mm:ss.fff")));
        }

        if (endTime.HasValue)
        {
            conditions.Add("Timestamp <= $endTime");
            parameters.Add(("$endTime", endTime.Value.ToString("yyyy-MM-dd HH:mm:ss.fff")));
        }

        if (!string.IsNullOrEmpty(deviceId))
        {
            conditions.Add("DeviceId = $deviceId");
            parameters.Add(("$deviceId", deviceId));
        }

        if (!string.IsNullOrEmpty(dataType))
        {
            conditions.Add("DataType = $dataType");
            parameters.Add(("$dataType", dataType));
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            conditions.Add("(DeviceId LIKE $keyword OR Data LIKE $keyword OR DataType LIKE $keyword)");
            parameters.Add(("$keyword", $"%{keyword}%"));
        }

        var whereClause = conditions.Any() ? " WHERE " + string.Join(" AND ", conditions) : "";

        var countCommand = connection.CreateCommand();
        countCommand.CommandText = $"SELECT COUNT(*) FROM DeviceLogs{whereClause}";
        foreach (var (name, value) in parameters)
        {
            countCommand.Parameters.AddWithValue(name, value);
        }
        totalCount = Convert.ToInt32(countCommand.ExecuteScalar());

        var queryCommand = connection.CreateCommand();
        var offset = (pageNumber - 1) * pageSize;
        queryCommand.CommandText = $"SELECT Id, Timestamp, DeviceId, Data, DataType FROM DeviceLogs{whereClause} ORDER BY Timestamp DESC LIMIT $pageSize OFFSET $offset";
        queryCommand.Parameters.AddWithValue("$pageSize", pageSize);
        queryCommand.Parameters.AddWithValue("$offset", offset);
        foreach (var (name, value) in parameters)
        {
            queryCommand.Parameters.AddWithValue(name, value);
        }

        using var reader = queryCommand.ExecuteReader();
        while (reader.Read())
        {
            logs.Add(new DeviceLog
            {
                Id = reader.GetInt32(0),
                Timestamp = DateTime.Parse(reader.GetString(1)),
                DeviceId = reader.GetString(2),
                Data = reader.GetString(3),
                DataType = reader.GetString(4)
            });
        }

        return (logs, totalCount);
    }

    public List<string> GetDistinctDeviceIds()
    {
        var deviceIds = new List<string>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT DeviceId FROM DeviceLogs ORDER BY DeviceId";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            deviceIds.Add(reader.GetString(0));
        }

        return deviceIds;
    }

    public List<string> GetDistinctDataTypes()
    {
        var dataTypes = new List<string>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT DataType FROM DeviceLogs ORDER BY DataType";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            dataTypes.Add(reader.GetString(0));
        }

        return dataTypes;
    }

    public int DeleteLogs(DateTime? beforeTime = null, string? deviceId = null)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        var queryBuilder = new StringBuilder("DELETE FROM DeviceLogs WHERE 1=1");
        var conditions = new List<string>();

        if (beforeTime.HasValue)
        {
            conditions.Add("Timestamp < $beforeTime");
            command.Parameters.AddWithValue("$beforeTime", beforeTime.Value.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        }

        if (!string.IsNullOrEmpty(deviceId))
        {
            conditions.Add("DeviceId = $deviceId");
            command.Parameters.AddWithValue("$deviceId", deviceId);
        }

        if (conditions.Any())
        {
            queryBuilder.Append(" AND ").Append(string.Join(" AND ", conditions));
        }

        command.CommandText = queryBuilder.ToString();
        return command.ExecuteNonQuery();
    }

    public string ExportToCsv(List<DeviceLog> logs, string filePath)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Id,Timestamp,DeviceId,Data,DataType");

        foreach (var log in logs)
        {
            csv.AppendLine($"{log.Id},{EscapeCsvValue(log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"))},{EscapeCsvValue(log.DeviceId)},{EscapeCsvValue(log.Data)},{EscapeCsvValue(log.DataType)}");
        }

        File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);
        return filePath;
    }

    private string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
        }

        _disposed = true;
    }

    ~SqliteLogStorage()
    {
        Dispose(false);
    }
}
