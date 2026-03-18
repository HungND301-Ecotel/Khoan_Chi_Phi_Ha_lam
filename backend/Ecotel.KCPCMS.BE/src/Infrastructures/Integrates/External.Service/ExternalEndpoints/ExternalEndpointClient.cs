using Application.Common.UnitOfWork;
using Application.Interfaces.Infrastructures.Integrates.Externals;

namespace External.Service.ExternalEndpoints;
public class ExternalEndpointClient : IExternalEndpointClient
{
    private readonly HttpClient _httpClient;


    public ExternalEndpointClient(HttpClient httpClient, IUnitOfWork unitOfWork)
    {
        _httpClient = httpClient;
    }
}
