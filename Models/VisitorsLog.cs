using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class VisitorsLog
{
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    [BsonElement("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [BsonElement("visitDate")]
    public DateTime VisitDate { get; set; } = DateTime.UtcNow;

    [BsonElement("country")]
    public string Country { get; set; } = string.Empty;

    [BsonElement("city")]
    public string City { get; set; } = string.Empty;

    [BsonElement("device")]
    public string Device { get; set; } = string.Empty;

    [BsonElement("browser")]
    public string Browser { get; set; } = string.Empty;

    [BsonElement("os")]
    public string OS { get; set; } = string.Empty;

    [BsonElement("pagesVisited")]
    public List<string> PagesVisited { get; set; } = new List<string>();

    [BsonElement("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;

    [BsonElement("blocked")]
    public bool Blocked { get; set; } = false;

    [BsonElement("userType")]
    public string UserType { get; set; } = string.Empty;
}
