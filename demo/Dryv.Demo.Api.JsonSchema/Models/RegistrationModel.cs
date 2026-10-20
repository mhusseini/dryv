using System.Text.RegularExpressions;

namespace Dryv.Demo.Api.JsonSchema.Models;

public class RegistrationModel
{
    public static readonly DryvRules Rules = DryvRules
        .For<RegistrationModel>()

        .Rule(m => m.FirstName, rule => rule
            .Validate(m => string.IsNullOrWhiteSpace(m.FirstName)
                ? "Please enter your first name."
                : null))

        .Rule(m => m.LastName, rule => rule
            .Validate(m => string.IsNullOrWhiteSpace(m.LastName)
                ? "Please enter your last name."
                : null))

        .Rule(m => m.Email, rule => rule
            .Validate(m => string.IsNullOrWhiteSpace(m.Email)
                ? "Please enter your email address."
                : null))

        .Rule(m => m.Email, rule => rule
            .Validate(m => !new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$").IsMatch(m.Email)
                ? "Please enter a valid email address."
                : DryvValidationResult.Success))

        .Rule(m => m.Password, rule => rule
            .Validate(m => string.IsNullOrWhiteSpace(m.Password)
                ? "Please enter a password."
                : null))

        .Rule(m => m.Password, rule => rule
            .Validate(m => m.Password.Length < 8
                ? "Password must be at least 8 characters."
                : null))

        .Rule(m => m.Age, rule => rule
            .Validate(m => m.Age < 18
                ? "You must be at least 18 years old."
                : null))

        .Rule(m => m.Age, rule => rule
            .Validate(m => m.Age > 120
                ? "Please enter a valid age."
                : null));

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Age { get; set; }
}
