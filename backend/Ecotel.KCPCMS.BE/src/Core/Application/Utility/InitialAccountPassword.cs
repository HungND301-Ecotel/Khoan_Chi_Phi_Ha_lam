namespace Application.Utility;

public static class InitialAccountPassword
{
    public static string Read(string variableName)
    {
        string? password = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(password) || password.Length < 16 ||
            !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) || !password.Any(c => !char.IsLetterOrDigit(c)))
        {
            throw new InvalidOperationException($"{variableName} must contain at least 16 characters, uppercase, lowercase, digits and special characters.");
        }

        string otherVariable = variableName == "BOOTSTRAP_ADMIN_PASSWORD"
            ? "INITIAL_ACCOUNT_PASSWORD" : "BOOTSTRAP_ADMIN_PASSWORD";
        if (password == Environment.GetEnvironmentVariable(otherVariable))
        {
            throw new InvalidOperationException("Bootstrap admin and initial account passwords must be different.");
        }

        return password;
    }
}
