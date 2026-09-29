using System.Net;
using System.Net.Http.Json;

namespace WebShop.Web.Tests.Fakes;

internal static class FakeResponses
{
    public static HttpResponseMessage Json(HttpStatusCode statusCode, object value) =>
        new(statusCode) { Content = JsonContent.Create(value) };
}
