using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class VisitorsLog
{
    [BsonId]
    public ObjectId Id { get; set; }
    
    [BsonElement("sessionId")]
    public string SessionId { get; set; } = string.Empty;
    
    [BsonElement("visitDate")]
    public DateTime VisitDate { get; set; } = DateTime.UtcNow;
    
    [BsonElement("country")]
    public string Country { get; set; } = "Unknown";

    [BsonElement("city")]
    public string City { get; set; } = "Unknown";
    
    [BsonElement("device")]
    public string Device { get; set; } = "Unknown";
    
    [BsonElement("browser")]
    public string Browser { get; set; } = "Unknown";
    
    [BsonElement("os")]
    public string OS { get; set; } = "Unknown";
    
    [BsonElement("pagesVisited")]
    public List<string> PagesVisited { get; set; } = new List<string>();
    
    [BsonElement("ipAddress")]
    public string IpAddress { get; set; } = "Unknown";

    [BsonElement("blocked")]
    public bool Blocked { get; set; } = false;

    [BsonElement("userType")]
    public string UserType { get; set; } = "Visitor";
}
