using LoopGame.Application.IServices.EconomyAndProgressionServices;
using LoopGame.Application.Services.EconomyAndProgressionServices;
using System.Reflection;
using LoopGame.Application.Services.LearningAndContentServices;
using Microsoft.Extensions.Configuration;

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
        services.AddScoped<IPracticeService, PracticeService>();
        services.AddHttpClient<ICodeExecutionService, CodeExecutionService>((sp, client) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var baseUrl = configuration["CodeRunner:BaseUrl"] ?? "http://localhost:5000";
            client.BaseAddress = new Uri(baseUrl);
        });

        services.AddSingleton<IAssessmentEventEmitter, NoopAssessmentEventEmitter>();

        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        return services;
    }
}
