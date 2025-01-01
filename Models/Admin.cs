using System;
using System.ComponentModel.DataAnnotations;

public class Admin
{
    public int AdminId { get; set; }

    [Required(ErrorMessage = "Username is required.")]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid Phone Number.")]
    [MaxLength(15)]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid Email Address.")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;


    [Required(ErrorMessage = "Status is required.")]
    [MaxLength(50)] // Example: Active, Inactive
    public string Status { get; set; } = "Active"; // Default value
}
