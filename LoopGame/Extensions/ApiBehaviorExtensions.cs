using LoopGame.Models;

namespace LoopGame.Extensions;

public static class ApiBehaviorExtensions
{
    /// <summary>
    /// Replaces ASP.NET's default ValidationProblemDetails with the same
    /// <see cref="ApiErrorResponse"/> shape used by domain Result failures.
    /// </summary>
    public static IServiceCollection AddUnifiedApiErrors(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var body = ApiErrorResponse.FromModelState(context.ModelState);
                return new BadRequestObjectResult(body);
            };
        });

        return services;
    }
}
