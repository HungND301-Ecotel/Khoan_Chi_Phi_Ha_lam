namespace Application.Utility;

public static class InitialAccountPassword
{
    public static string Read(string variableName)
    {
        string? password = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException($"{variableName} must be configured in the backend environment.");
        }

        return password;
    }
}
