using System.Text.Json;
using CodeRunner.Options;
using CodeRunner.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure CodeRunner Options
builder.Services.Configure<CodeRunnerOptions>(
    builder.Configuration.GetSection(CodeRunnerOptions.SectionName));

// Add services to the container
builder.Services.AddScoped<ISandboxService, DockerSandboxService>();
builder.Services.AddScoped<ICodeExecutionService, CodeExecutionService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
