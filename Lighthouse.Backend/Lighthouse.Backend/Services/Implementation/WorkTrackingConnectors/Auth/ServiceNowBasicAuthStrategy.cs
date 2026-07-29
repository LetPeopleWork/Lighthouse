using System.Net.Http.Headers;
using System.Text;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Auth
{
    // Its own class rather than a reuse of JiraCloudBasicAuthStrategy: that one reads Jira option
    // keys by name and falls through to Bearer for the token methods, so reusing it would put
    // ServiceNow knowledge inside a Jira-named class.
    public class ServiceNowBasicAuthStrategy(ICryptoService cryptoService) : IWorkTrackingAuthStrategy
    {
        public Task ApplyAsync(HttpRequestMessage request, WorkTrackingSystemConnection connection, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(connection);

            var username = connection.GetWorkTrackingSystemConnectionOptionByKey(ServiceNowWorkTrackingOptionNames.Username);
            var encryptedPassword = connection.GetWorkTrackingSystemConnectionOptionByKey(ServiceNowWorkTrackingOptionNames.Password);
            var password = cryptoService.Decrypt(encryptedPassword);

            var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encoded);

            return Task.CompletedTask;
        }
    }
}
