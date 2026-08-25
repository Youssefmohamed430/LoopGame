namespace LoopGame.Infrastructure;

/// <summary>
/// Infrastructure layer service registrations.
/// Called from Program.cs: builder.Services.AddInfrastructure(builder.Configuration)
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(
                    typeof(AppDbContext).Assembly.FullName)));

        return services;
    }
}
