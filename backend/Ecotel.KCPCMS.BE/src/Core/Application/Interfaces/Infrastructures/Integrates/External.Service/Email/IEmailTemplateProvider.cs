namespace Application.Interfaces.Infrastructures.Integrates.External.Service.Email;

public interface IEmailTemplateProvider
{
    string GetTemplateByName(string name, string languageCode = "en");

    Task<string> GetTemplateByNameAsync(string name, string languageCode = "en");
}