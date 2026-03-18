using System.Globalization;
using Application.Common.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Shared.Constants;

namespace EfCore.Persistence.ConnectionString;

public class ConnectionStringSecurer(IOptions<DatabaseSettings> dbSettings) : IConnectionStringSecurer
{
    private const string HiddenValueDefault = "*******";
    private readonly DatabaseSettings _dbSettings = dbSettings.Value;

    public string? MakeSecure(string? connectionString, string? dbProvider)
    {
        if (connectionString == null || string.IsNullOrEmpty(connectionString))
        {
            return connectionString;
        }

        if (string.IsNullOrWhiteSpace(dbProvider))
        {
            dbProvider = _dbSettings.DbProvider;
        }

        return dbProvider.ToLower(CultureInfo.CurrentCulture) switch
        {
            DbProviderKeys.SqlServer => MakeSecureSqlConnectionString(connectionString),
            _ => connectionString
        };
    }

    private static string MakeSecureSqlConnectionString(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);

        if (!string.IsNullOrEmpty(builder.Password) || !builder.IntegratedSecurity)
        {
            builder.Password = HiddenValueDefault;
        }

        if (!string.IsNullOrEmpty(builder.UserID) || !builder.IntegratedSecurity)
        {
            builder.UserID = HiddenValueDefault;
        }

        return builder.ToString();
    }
}