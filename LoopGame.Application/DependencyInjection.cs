using Hangfire;
using Hangfire.PostgreSql;
using LoopGame.Application.IServices.EconomyAndProgressionServices;
using LoopGame.Application.Options;
using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Application.Services.EconomyAndProgressionServices;
using LoopGame.Application.Services.LearningAndContentServices;
using LoopGame.Application.Services.SystemAndUtilityServices;
using LoopGame.Infrastructure.Identity;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace LoopGame.Application;

/// <summary>
/// Application layer service registrations.
/// Called from Program.cs: builder.Services.AddApplication();
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration Configuration)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());
        services.AddSingleton(config);

        services.AddScoped<IEconomyService, EconomyService>();
        services.AddScoped<IShopService, ShopService>();
        services.AddScoped<ISahmService, SahmService>();
        services.AddScoped<IPracticeService, PracticeService>();
        services.AddScoped<BackgroundJob, Services.SystemAndUtilityServices.ScenarioGeneratorService>();
        services.AddScoped<INarrativeService, NarrativeService>();
        services.AddScoped<IChoiceService, ChoiceService>();
        services.AddHttpClient<ICodeExecutionService, CodeExecutionService>((sp, client) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var baseUrl = configuration["CodeRunner:BaseUrl"] ?? "http://localhost:5000";
            client.BaseAddress = new Uri(baseUrl);
        });
        services.AddHangfire(config =>
            config.UsePostgreSqlStorage(options =>
            {
                options.UseNpgsqlConnection(
                    Configuration.GetConnectionString("DefaultConnection"));
            }));

        services.AddHangfireServer();
        services.Configure<JwtSettings>(
             Configuration.GetSection("JwtSettings"));

        services.Configure<EmailSettings>(
            Configuration.GetSection("EmailSettings"));

        services.Configure<SupabaseS3Settings>(
            Configuration.GetSection("SupabaseS3Settings"));

        services.AddScoped<ISideTaskService, SideTaskService>();
        services.AddScoped<ISaveService, SaveService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddSingleton<IAssessmentEventEmitter, NoopAssessmentEventEmitter>();


        return services;
    }
}
