using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;
using PseudoMarkets.Security.IdentityServer.Core.Exceptions;
using PseudoMarkets.Security.IdentityServer.Web.Infrastructure;

namespace PseudoMarkets.Security.IdentityServer.Web.Tests.Infrastructure;

[TestFixture]
public class IdentityExceptionHandlerTests
{
    [Test]
    public async Task TryHandleAsync_ShouldReturnServiceUnavailable_ForDependencyException()
    {
        var httpContext = CreateHttpContext();
        var sut = new IdentityExceptionHandler(Mock.Of<ILogger<IdentityExceptionHandler>>());

        var handled = await sut.TryHandleAsync(httpContext, new IdentityDependencyException("down"), CancellationToken.None);

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);

        var payload = await ReadResponseBodyAsync(httpContext);
        payload.GetProperty("title").GetString().ShouldBe("Identity data store unavailable");
    }

    [Test]
    public async Task TryHandleAsync_ShouldReturnInternalServerError_ForServiceException()
    {
        var httpContext = CreateHttpContext();
        var sut = new IdentityExceptionHandler(Mock.Of<ILogger<IdentityExceptionHandler>>());

        var handled = await sut.TryHandleAsync(httpContext, new IdentityServiceException("failed"), CancellationToken.None);

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);

        var payload = await ReadResponseBodyAsync(httpContext);
        payload.GetProperty("title").GetString().ShouldBe("Identity service error");
    }

    [Test]
    public async Task TryHandleAsync_ShouldReturnGenericInternalServerError_ForUnexpectedException()
    {
        var httpContext = CreateHttpContext();
        var sut = new IdentityExceptionHandler(Mock.Of<ILogger<IdentityExceptionHandler>>());

        var handled = await sut.TryHandleAsync(httpContext, new InvalidOperationException("failed"), CancellationToken.None);

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);

        var payload = await ReadResponseBodyAsync(httpContext);
        payload.GetProperty("title").GetString().ShouldBe("Unexpected server error");
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        return new DefaultHttpContext
        {
            Response =
            {
                Body = new MemoryStream()
            }
        };
    }

    private static async Task<JsonElement> ReadResponseBodyAsync(DefaultHttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);
        return document.RootElement.Clone();
    }
}
