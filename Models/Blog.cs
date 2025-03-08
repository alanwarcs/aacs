using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

public class Blog
{
    [BsonId]  // MongoDB primary key
    public ObjectId Id { get; set; }  // Use ObjectId instead of int for MongoDB

    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Author is required.")]
    [MaxLength(100, ErrorMessage = "Author cannot exceed 100 characters.")]
    public string Author { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required.")]
    public string Content { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Header Image URL cannot exceed 500 characters.")]
    public string? HeaderImageUrl { get; set; }

    [MaxLength(200, ErrorMessage = "Tags cannot exceed 200 characters.")]
    public string? Tags { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    [RegularExpression("^(Draft|Published)$", ErrorMessage = "Status must be either 'Draft' or 'Published'.")]
    public string Status { get; set; } = "Draft"; // Default to 'Draft'

    public DateTime? DatePublished { get; set; }

    [Required]
    public DateTime DateCreated { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Description is required.")]
    public string Description { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

}