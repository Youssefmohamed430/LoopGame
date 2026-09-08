// Load environment variables from root .env file into System.Environment
using Hangfire;
using Hangfire.PostgreSql;


Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure services (registers AppDbContext with PostgreSQL using connection string from .env)
builder.Services.AddInfrastructure(builder.Configuration);




// Add Application services
builder.Services.AddApplication(builder.Configuration);

// ── Hangfire ─────────────────────────────────────────────────────────────
// Uses the same PostgreSQL connection string as EF Core.
var hangfireConnStr = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(hangfireConnStr)));

builder.Services.AddHangfireServer();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums as names ("VideoCall", "Ideal") instead of numeric values.
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Model validation + domain Result failures share ApiErrorResponse shape.
builder.Services.AddUnifiedApiErrors();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Expose the Hangfire dashboard in development only
    app.MapHangfireDashboard("/hangfire");
}
app.UseHangfireDashboard("/hangfire");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
