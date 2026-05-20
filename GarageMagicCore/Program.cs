using Microsoft.EntityFrameworkCore;
using GarageMagicCore.Data;
using GarageMagicCore.Services;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Add CORS for the React UI dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Add services to the container.
// Configure SQLite database
builder.Services.AddDbContext<GarageMagicDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add controllers
builder.Services.AddControllers();

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Register application services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDeckService, DeckService>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<ISeasonService, SeasonService>();
builder.Services.AddScoped<IStatsService, StatsService>();
builder.Services.AddScoped<IBetrayalService, BetrayalService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GarageMagicDbContext>();
    await DbSeeder.SeedAsync(dbContext);
}

app.Run();

