using System;

namespace LiteQMS.Data;

public class CallRecord
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string PatientNumber { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsCNA { get; set; }
}
