using Dryv.Demo.Razor.Services;

namespace Dryv.Demo.Razor.Models;

public class RegistrationModel
{
    public static readonly DryvRules Rules = DryvRules
        .For<RegistrationModel>()

        // ── Parameter: expose a server-computed value for use in client-side rules ──
        .Parameter("minPasswordLength", () => 8)

        // ── DisableRules: disable the CompanyName field when account type is not "business" ──
        .DisableRules(
            m => m.CompanyName,
            m => m.AccountType != "business")

        // ── Name + Group: categorize rules for structured access ──
        .Rule(m => m.FirstName, rule => rule
            .Validate(m => string.IsNullOrWhiteSpace(m.FirstName)
                ? "Please enter your first name."
                : null)
            .Name("first-name-required")
            .Group("personal-info"))
        .Rule(m => m.LastName, rule => rule
            .Validate(m => string.IsNullOrWhiteSpace(m.LastName)
                ? "Please enter your last name."
                : null)
            .Name("last-name-required")
            .Group("personal-info"))

        // ── Warning: non-blocking result type (not an error) ──
        .Rule(m => m.PhoneNumber, rule => rule
            .Validate(m => string.IsNullOrWhiteSpace(m.PhoneNumber)
                ? DryvValidationResult.Warning("Providing a phone number is recommended for account recovery.")
                : DryvValidationResult.Success)
            .Name("phone-recommended")
            .Group("contact"))

        // ── ClientRule: client-only validation (expression-based, translatable to JS) ──
        .ClientRule(
            m => m.PhoneNumber,
            m => !string.IsNullOrWhiteSpace(m.PhoneNumber) && m.PhoneNumber.Trim().Length < 7
                ? DryvValidationResult.Warning("Phone number seems too short.")
                : DryvValidationResult.Success)

        // ── Email: required ──
        .Rule(m => m.Email, rule => rule
            .Validate(m => string.IsNullOrWhiteSpace(m.Email)
                ? "Please enter your email address."
                : null)
            .Name("email-required")
            .Group("contact"))

        // ── Disable on builder: skip format check when email is empty ──
        .Rule(m => m.Email, rule => rule
            .Validate(m => !m.Email.Contains("@")
                ? "Please enter a valid email address."
                : null)
            .Disable(m => string.IsNullOrWhiteSpace(m.Email))
            .Name("email-format")
            .Group("contact"))

        // ── ServerRule: server-only validation (delegate-based, not translatable to JS) ──
        .ServerRule<EmailValidator>(
            m => m.Email,
            (m, emailValidator) => emailValidator.IsDomainValid(m.Email)
                ? DryvValidationResult.Success
                : DryvValidationResult.Error("The email domain does not appear to be valid."))

        // ── Password: required ──
        .Rule(m => m.Password, rule => rule
            .Validate(m => string.IsNullOrWhiteSpace(m.Password)
                ? "Please enter a password."
                : null)
            .Name("password-required")
            .Group("credentials"))

        // ── Disable + EvaluateOn: skip length check when empty, run on both client and server ──
        .Rule(m => m.Password, rule => rule
            .Validate(m => m.Password.Trim().Length < 8
                ? "Password must be at least 8 characters."
                : null)
            .Disable(m => string.IsNullOrWhiteSpace(m.Password))
            .EvaluateOn(DryvEvaluationLocation.Server | DryvEvaluationLocation.Client)
            .Name("password-length")
            .Group("credentials"))

        // ── Multi-property rule + RelatedTo ──
        .Rule(m => m.Password, m => m.ConfirmPassword, rule => rule
            .Validate(m => m.Password != m.ConfirmPassword
                ? DryvValidationResult.Error("Passwords do not match.")
                : DryvValidationResult.Success)
            .RelatedTo(m => m.ConfirmPassword)
            .Name("password-match")
            .Group("credentials"))

        // ── Disable on builder: conditionally required field ──
        .Rule(m => m.CompanyName, rule => rule
            .Validate(m => string.IsNullOrWhiteSpace(m.CompanyName)
                ? "Please enter your company name."
                : null)
            .Disable(m => m.AccountType != "business")
            .Name("company-required")
            .Group("business"))

        // ── Annotate: attach custom metadata to a rule ──
        .Rule(m => m.AcceptsTerms, rule => rule
            .Validate(m => !m.AcceptsTerms
                ? "You must accept the terms and conditions."
                : null)
            .Name("terms-required")
            .Annotate("severity", "blocking")
            .Annotate("helpUrl", "/terms"));

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string AccountType { get; set; } = "personal";
    public string CompanyName { get; set; } = string.Empty;
    public bool AcceptsTerms { get; set; }
}