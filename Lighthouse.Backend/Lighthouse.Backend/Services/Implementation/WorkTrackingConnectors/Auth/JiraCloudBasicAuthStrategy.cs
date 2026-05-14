using System.Net.Http.Headers;
using System.Text;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Auth
{
    public class JiraCloudBasicAuthStrategy(ICryptoService cryptoService) : IWorkTrackingAuthStrategy
    {
        public Task ApplyAsync(HttpRequestMessage request, WorkTrackingSystemConnection connection, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(connection);

            var encryptedApiToken = connection.GetWorkTrackingSystemConnectionOptionByKey(JiraWorkTrackingOptionNames.ApiToken);
            var apiToken = cryptoService.Decrypt(encryptedApiToken);

            request.Headers.Authorization = BuildAuthorizationHeader(connection, apiToken);

            return Task.CompletedTask;
        }

        private static AuthenticationHeaderValue BuildAuthorizationHeader(WorkTrackingSystemConnection connection, string apiToken)
        {
            if (connection.AuthenticationMethodKey is AuthenticationMethodKeys.JiraCloud or AuthenticationMethodKeys.JiraScopedToken)
            {
                var username = connection.GetWorkTrackingSystemConnectionOptionByKey(JiraWorkTrackingOptionNames.Username);
                var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{apiToken}"));
                return new AuthenticationHeaderValue("Basic", encoded);
            }

            return new AuthenticationHeaderValue("Bearer", apiToken);
        }
    }
}
