using Dryv.Demo.GraphQL.Dryvue.Models;

namespace Dryv.Demo.GraphQL.Dryvue;

public class Mutation
{
    public RegistrationPayload Register(RegistrationInput input)
    {
        return new RegistrationPayload
        {
            Success = true,
            Message = $"Welcome, {input.FirstName} {input.LastName}!"
        };
    }
}

public class RegistrationPayload
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
