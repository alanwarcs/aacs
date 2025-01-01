using System;
using System.ComponentModel.DataAnnotations;

public class Service
{
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Status is required.")]
    [RegularExpression("^(Draft|Published)$", ErrorMessage = "Status must be either 'Draft' or 'Published'.")]
    public string Status { get; set; } = "Draft"; // Default to 'Draft'

    public DateTime? DatePublished { get; set; }

    [Required]
    public DateTime Date { get; set; } = DateTime.Now;
}
