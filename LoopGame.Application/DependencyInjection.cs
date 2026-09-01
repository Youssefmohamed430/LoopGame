using System.Reflection;
using Hangfire;
using Hangfire.PostgreSql;
using LoopGame.Application.BackgroundJobs;
using LoopGame.Application.IServices.EconomyAndProgressionServices;
using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Application.IServices.SystemAndUtilityServices;
using LoopGame.Application.Options;
using LoopGame.Application.Services.EconomyAndProgressionServices;
using LoopGame.Application.Services.LearningAndContentServices;
using LoopGame.Application.Services.SystemAndUtilityServices;
using LoopGame.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;

namespace LoopGame.Application;

/// <summary>
/// Application layer service registrations.
/// Called from Program.cs: builder.Services.AddApplication(builder.Configuration);
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());
        services.AddSingleton(config);

        services.AddScoped<IEconomyService, EconomyService>();
        services.AddScoped<IShopService, ShopService>();
        services.AddScoped<ISahmService, SahmService>();
        services.AddScoped<IPracticeService, PracticeService>();
        services.AddScoped<IScenarioGeneratorService, ScenarioGeneratorService>();
        services.AddScoped<INarrativeService, NarrativeService>();
        services.AddScoped<IChoiceService, ChoiceService>();

        services.AddHttpClient<ICodeExecutionService, CodeExecutionService>((sp, client) =>
        {
            var cfg = configuration ?? sp.GetRequiredService<IConfiguration>();
            var baseUrl = cfg["CodeRunner:BaseUrl"] ?? "http://localhost:5000";
            client.BaseAddress = new Uri(baseUrl);
        });

        if (configuration is not null)
        {
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.Configure<SupabaseS3Settings>(configuration.GetSection("SupabaseS3Settings"));
        }

        services.AddScoped<ISideTaskService, SideTaskService>();
        services.AddScoped<ISaveService, SaveService>();
        services.AddScoped<IAdminService, AdminService>();

        // ── Assessment Layer ───────────────────────────────────────────────────
        services.AddScoped<IAssessmentService, AssessmentService>();
        services.AddScoped<IAssessmentEventEmitter, HangfireAssessmentEventEmitter>();
        services.AddScoped<IAssessmentJobScheduler, AssessmentJobScheduler>();
        services.AddScoped<AssessmentJobs>();

        return services;
    }
}
