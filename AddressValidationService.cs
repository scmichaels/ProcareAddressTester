//-----------------------------------------------------------------------
// <copyright file="AddressValidationService.cs" company="Procare Software, LLC">
//     Copyright © 2021-2023 Procare Software, LLC. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Procare.AddressValidation.Tester
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class AddressValidationService : BaseHttpService
    {
        public AddressValidationService(IHttpClientFactory httpClientFactory, bool disposeFactory, Uri baseUrl)
            : this(httpClientFactory, disposeFactory, baseUrl, null, false)
        {
        }

        protected AddressValidationService(IHttpClientFactory httpClientFactory, bool disposeFactory, Uri baseUrl, HttpMessageHandler? httpMessageHandler, bool disposeHandler)
            : base(httpClientFactory, disposeFactory, baseUrl, httpMessageHandler, disposeHandler)
        {
        }

        public async Task<string> GetAddressesAsync(AddressValidationRequest request, CancellationToken token = default)
        {
            int retryCount = 0;
            string errorMessage = "Address validation request has failed.";

            while (retryCount < 3)
            {
                try
                {
                    using var httpRequest = request.ToHttpRequest(this.BaseUrl);
                    using var response = await this.CreateClient()
                        .SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, token)
                        .ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();

                    return await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                }
                catch (HttpRequestException ex)
                {
                    if (!ex.StatusCode.HasValue)
                    {
                        return ex.InnerException?.ToString() ?? errorMessage;
                    }

                    switch ((int)ex.StatusCode.Value)
                    {
                        case >= 500 and < 600:
                            retryCount++;
                            Console.WriteLine($"{errorMessage} Server returned: {ex.StatusCode}. Retry attempt {retryCount}...");
                            continue;

                        default:
                            errorMessage += $" Server returned: {ex.StatusCode}";
                            throw new HttpRequestException(errorMessage);
                    }
                }
                catch (TaskCanceledException)
                {
                    retryCount++;
                    Console.WriteLine($"{errorMessage} Retry attempt {retryCount}...");
                    continue;
                }
            }

            throw new HttpRequestException($"{errorMessage} Server response timed out.");
        }
    }
}
