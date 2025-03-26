using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class AdminLog
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("adminid")]
    public string AdminId { get; set; } = string.Empty;

    [BsonElement("adminname")]
    public string AdminName { get; set; } = string.Empty;

    [BsonElement("action")]
    public string Action { get; set; } = string.Empty;

    [BsonElement("performedBy")]
    public string PerformedBy { get; set; } = string.Empty;

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.MinValue;
}