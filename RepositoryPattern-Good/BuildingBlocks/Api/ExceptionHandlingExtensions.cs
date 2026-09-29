using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace WebShop.BuildingBlocks.Api;

public static class ExceptionHandlingExtensions
{
    // Every context's command/query handlers signal invalid input by throwing ArgumentException -
    // this is the one place that turns that into a 400 instead of letting it surface as an
    // unhandled 500.
    public static WebApplication UseArgumentExceptionAsBadRequest(this WebApplication app)
    {
        app.UseExceptionHandler(handler => handler.Run(async context =>
        {
            var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            if (error is ArgumentException)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = error.Message });
                return;
            }

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }));

        return app;
    }
}
