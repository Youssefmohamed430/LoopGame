// Load environment variables from root .env file into System.Environment
using Hangfire;
using LoopGame.Application.Options;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure services (registers AppDbContext with PostgreSQL using connection string from .env)
builder.Services.AddInfrastructure(builder.Configuration);




// Add Application services
builder.Services.AddApplication(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHangfireDashboard("/hangfire");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
