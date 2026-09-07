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

        services.AddScoped<IMapper, Mapper>();

        services.AddScoped<IEconomyService, EconomyService>();
        services.AddScoped<IShopService, ShopService>();
        services.AddScoped<ISahmService, SahmService>();
        services.AddScoped<INarrativeService, NarrativeService>();
        services.AddScoped<IChoiceService, ChoiceService>();


        // ── Practice Layer ────────────────────────────────────────────────────
        services.AddScoped<IPracticeAccessService, PracticeAccessService>();
        services.AddScoped<IAttemptPolicy, MaxAttemptsPolicy>();
        services.AddScoped<ITierCalculationPolicy, PracticeTierCalculationPolicy>();
        services.AddScoped<IPracticeAttemptService, PracticeAttemptService>();
        services.AddScoped<IProgressionService, ProgressionService>();
        services.AddScoped<IPracticeService, PracticeService>();
        services.AddScoped<IScenarioGeneratorService, ScenarioGeneratorService>();
        services.AddScoped<IFileStorageService, SupabaseS3StorageService>();
        services.AddScoped<IFileContentReaderService, FileContentReaderService>();



        services.AddHttpClient<ICodeExecutionService, CodeExecutionService>((sp, client) =>
        {
            var cfg = configuration ?? sp.GetRequiredService<IConfiguration>();
            var baseUrl = cfg["CodeRunner:BaseUrl"] ?? "http://localhost:5000";
            client.BaseAddress = new Uri(baseUrl);
        });

        services.AddHttpClient<IAiSideTaskClient, AiSideTaskHttpClient>((sp, client) =>
        {
            var cfg = configuration ?? sp.GetRequiredService<IConfiguration>();
            var baseUrl = cfg["AiServiceSettings:BaseUrl"] ?? "http://localhost:8000";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(60);
        });



        if (configuration is not null)
        {
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.Configure<SupabaseS3Settings>(configuration.GetSection("SupabaseS3Settings"));
            services.Configure<AiServiceSettings>(configuration.GetSection("AiServiceSettings"));
        }

        // ── Auth Layer ────────────────────────────────────────────────────────
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IEmailService, EmailService>();

        services.AddScoped<ISideTaskService, SideTaskService>();
        services.AddScoped<IAdminService, AdminService>();

        // ── Event Publishing Layer ────────────────────────────────────────────
        services.AddScoped<IEventPublisher, InProcessEventPublisher>();
        services.AddScoped<IEventHandler, AssessmentEventHandler>();
        services.AddScoped<IEventHandler, SideTaskGenerationEventHandler>();

        // ── Assessment Layer ───────────────────────────────────────────────────
        services.AddScoped<IAssessmentService, AssessmentService>();
        services.AddScoped<IAssessmentJobScheduler, AssessmentJobScheduler>();
        services.AddScoped<AssessmentJobs>();
        services.AddScoped<SideTaskGenerationJobs>();

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var settings = sp
                .GetRequiredService<IOptions<SupabaseS3Settings>>()
                .Value;

            var config = new AmazonS3Config
            {
                ServiceURL = settings.Endpoint,
                ForcePathStyle = true,
                AuthenticationRegion = settings.Region
            };

            return new AmazonS3Client(
                settings.AccessKey,
                settings.SecretKey,
                config);
        });
        

        return services;
    }
}
