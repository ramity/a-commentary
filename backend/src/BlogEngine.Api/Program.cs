using System.Text.Json.Serialization;
using BlogEngine.Core.Interfaces;
using BlogEngine.Infrastructure.Data;
using BlogEngine.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BlogEngine API - Stream of Consciousness & AI Commentary",
        Version = "v1",
        Description = "Backend API for full-stack AI blog post editor, static HTML compiler, and Chess-style commentary reactions."
    });
});

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? "Server=db;Port=3306;Database=blog_db;User=blog_user;Password=blog_secret;";

var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));

builder.Services.AddDbContext<BlogDbContext>(options =>
{
    options.UseMySql(connectionString, serverVersion, mySqlOptions =>
    {
        mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    });
});

// Register Domain Services
builder.Services.AddHttpClient<ILlmShareImporter, LlmShareImporter>();
builder.Services.AddScoped<IStaticPageCompiler, StaticPageCompiler>();

// CORS configuration for local dev and frontend access
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Auto-migrate & seed with retry loop on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var dbContext = services.GetRequiredService<BlogDbContext>();
    var compiler = services.GetRequiredService<IStaticPageCompiler>();

    int retries = 0;
    const int maxRetries = 15;
    while (retries < maxRetries)
    {
        try
        {
            logger.LogInformation("Attempting database connection and schema initialization (attempt {Attempt}/{Max})...", retries + 1, maxRetries);
            await DbInitializer.InitializeAsync(dbContext, logger);

            // Pre-compile all published posts to static files on startup
            var publishedPosts = await dbContext.BlogPosts
                .Include(p => p.Sections)
                .Include(p => p.Reactions)
                .Where(p => p.IsPublished)
                .ToListAsync();

            foreach (var post in publishedPosts)
            {
                await compiler.CompileAndSaveAsync(post);
            }

            logger.LogInformation("Primed {Count} static posts into static directory.", publishedPosts.Count);
            break;
        }
        catch (Exception ex)
        {
            retries++;
            logger.LogWarning(ex, "Database connection attempt {Attempt} failed. Retrying in 4 seconds...", retries);
            if (retries >= maxRetries)
            {
                logger.LogError(ex, "Max retry limit reached. Continuing app startup; database may need inspection.");
                break;
            }
            await Task.Delay(4000);
        }
    }
}

// Configure HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BlogEngine API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");

app.UseRouting();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", time = DateTime.UtcNow }));

app.Run();
