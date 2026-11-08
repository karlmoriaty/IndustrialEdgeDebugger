using IndustrialEdgeDebugger.Comm.Models;

namespace IndustrialEdgeDebugger.Comm.Services;

public interface ILogStorage : IDisposable
{
    void Initialize();
    int AddLog(DeviceLog log);
    List<DeviceLog> GetLogs(DateTime? startTime = null, DateTime? endTime = null, string? deviceId = null, string? dataType = null, int? limit = null);
    (List<DeviceLog> logs, int totalCount) GetLogsPaged(DateTime? startTime = null, DateTime? endTime = null, string? deviceId = null, string? dataType = null, string? keyword = null, int pageNumber = 1, int pageSize = 50);
    int DeleteLogs(DateTime? beforeTime = null, string? deviceId = null);
    string ExportToCsv(List<DeviceLog> logs, string filePath);
    List<string> GetDistinctDeviceIds();
    List<string> GetDistinctDataTypes();
}
