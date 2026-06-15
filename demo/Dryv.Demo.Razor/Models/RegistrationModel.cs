using Dryv;
using Dryv.Demo.Razor.Services;

namespace Dryv.Demo.Razor.Models;

public class RegistrationModel
{
    public static readonly DryvRules Rules = DryvRules
        .For<RegistrationModel>()
        .Rule(
            m => m.FirstName,
            m => string.IsNullOrWhiteSpace(m.FirstName)
                ? "Please enter your first name."
                : null)
        .Rule(
            m => m.LastName,
            m => string.IsNullOrWhiteSpace(m.LastName)
                ? "Please enter your last name."
                : null)
        .Rule(
            m => m.Email,
            m => string.IsNullOrWhiteSpace(m.Email)
                ? "Please enter your email address."
                : null)
        .Rule(
            m => m.Email,
            m => !m.Email.Contains("@")
                ? "Please enter a valid email address."
                : null)
        .Rule<EmailValidator>(
            m => m.Email,
            (m, emailValidator) => emailValidator.IsDomainValid(m.Email)
                ? DryvValidationResult.Success
                : DryvValidationResult.Error("The email domain does not appear to be valid."))
        .Rule(
            m => m.Password,
            m => string.IsNullOrWhiteSpace(m.Password)
                ? "Please enter a password."
                : null)
        .Rule(
            m => m.Password,
            m => m.Password.Trim().Length < 8
                ? "Password must be at least 8 characters."
                : null)
        .Rule(
            m => m.Password, m => m.ConfirmPassword,
            m => m.Password != m.ConfirmPassword
                ? DryvValidationResult.Error("Passwords do not match.")
                : DryvValidationResult.Success);

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
