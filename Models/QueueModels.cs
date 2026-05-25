namespace LiteQMS.Models;

public record CallState(
    string RoomNumber,
    string PatientNumber,
    DateTime Timestamp,
    List<RecentCall> RecentCalls,
    int CallCount,
    bool IsRecall
);

public record RecentCall(
    int Id,
    string RoomNumber,
    string PatientNumber,
    DateTime Timestamp,
    bool IsCNA
);

public record CallResult(bool Success, string Error, int CallCount);
