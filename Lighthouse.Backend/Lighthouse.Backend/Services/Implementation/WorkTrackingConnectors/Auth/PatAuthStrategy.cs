using System.Net.Http.Headers;
using System.Text;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Auth
{
    public class PatAuthStrategy(ICryptoService cryptoService) : IWorkTrackingAuthStrategy
    {
        public Task ApplyAsync(HttpRequestMessage request, WorkTrackingSystemConnection connection, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(connection);

            var encryptedPat = connection.GetWorkTrackingSystemConnectionOptionByKey(AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken);
            var pat = cryptoService.Decrypt(encryptedPat);
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($":{pat}"));

            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encoded);

            return Task.CompletedTask;
        }
    }
}
