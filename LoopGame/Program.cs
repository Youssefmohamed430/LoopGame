// Load environment variables from root .env file into System.Environment
using LoopGame.Infrastructure.Email;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure services (registers AppDbContext with PostgreSQL using connection string from .env)
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));


// Add Application services
builder.Services.AddApplication();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
