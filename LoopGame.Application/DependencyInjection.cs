using LoopGame.Application.IServices.EconomyAndProgressionServices;
using LoopGame.Application.Services.EconomyAndProgressionServices;

namespace LoopGame.Application;

/// <summary>
/// Application layer service registrations.
/// Called from Program.cs: builder.Services.AddApplication();
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());
        services.AddSingleton(config);

        services.AddScoped<IEconomyService, EconomyService>();
        services.AddScoped<IShopService, ShopService>();
        services.AddScoped<ISahmService, SahmService>();

        services.AddSingleton<IAssessmentEventEmitter, NoopAssessmentEventEmitter>();

        return services;
    }
}
