using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

public class Contact
{
    [BsonId]  // MongoDB primary key
    public ObjectId Id { get; set; }  // Use ObjectId instead of int for MongoDB

    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public required string Email { get; set; }

    [Required]
    [StringLength(15)]
    [Phone]
    public required string Phone { get; set; }

    [Required]
    [StringLength(500)]
    public required string Message { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}