using System.Text.Json;
using AuthSystem.Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AuthSystem.UnitTests.Api.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task Invoke_WhenNextSucceeds_DoesNotTouchResponse()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        RequestDelegate next = _ => Task.CompletedTask;

        var mw = new ExceptionHandlingMiddleware(next, NullLogger<ExceptionHandlingMiddleware>.Instance);

        await mw.Invoke(context);

        context.Response.StatusCode.Should().Be(200);
        context.Response.Body.Length.Should().Be(0);
    }

    [Fact]
    public async Task Invoke_WhenNextThrows_WritesProblemDetails500()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        RequestDelegate next = _ => throw new InvalidOperationException("boom");

        var mw = new ExceptionHandlingMiddleware(next, NullLogger<ExceptionHandlingMiddleware>.Instance);

        await mw.Invoke(context);

        context.Response.StatusCode.Should().Be(500);
        context.Response.ContentType.Should().Be("application/problem+json");

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(500);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Internal Server Error");
        doc.RootElement.GetProperty("instance").GetString().Should().Be("/api/test");
    }

    [Fact]
    public async Task Invoke_ClearsExistingResponseBeforeWritingError()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = async ctx =>
        {
            ctx.Response.StatusCode = 200;
            await ctx.Response.WriteAsync("partial");
            throw new Exception("boom");
        };

        var mw = new ExceptionHandlingMiddleware(next, NullLogger<ExceptionHandlingMiddleware>.Instance);

        await mw.Invoke(context);

        context.Response.StatusCode.Should().Be(500);
    }
}
