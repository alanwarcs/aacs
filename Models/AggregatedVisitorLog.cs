using MongoDB.Bson;
using System;
using System.Collections.Generic;

public class AggregatedVisitorLog
{
    public string IpAddress { get; set; } = string.Empty;
    public DateTime LastVisitDate { get; set; }
    public string Country { get; set; } = "Unknown";
    public string Browser { get; set; } = "Unknown";
    public bool Blocked { get; set; }
    public int VisitCount { get; set; }
    public List<VisitorsLog> Sessions { get; set; } = new List<VisitorsLog>();
}
