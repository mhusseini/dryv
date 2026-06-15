namespace Dryv.Demo.Api.Dryvue.Services;

public class EmailValidator
{
    private static readonly HashSet<string> ValidDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "gmail.com",
        "outlook.com",
        "hotmail.com",
        "yahoo.com",
        "protonmail.com",
        "example.com"
    };

    public bool IsDomainValid(string email)
    {
        // Mocked MX record validation: in a real application,
        // this would perform a DNS lookup for MX records.
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return false;
        }

        var domain = email.Split('@').Last();
        return ValidDomains.Contains(domain);
    }
}
