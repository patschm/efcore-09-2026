using System.Net;

namespace WebShop.Web.Tests.Fakes;

// Stands in for the real Catalog/Pricing/Reviews/Search APIs - each test programs OnRequest with
// exactly the responses its scenario needs. A request nothing was set up for comes back 404,
// which fails the test loudly instead of hanging on a real network call.
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, HttpResponseMessage>? OnRequest { get; set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(OnRequest?.Invoke(request) ?? new HttpResponseMessage(HttpStatusCode.NotFound));
}
