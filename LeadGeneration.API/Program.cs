using LeadGeneration.Application.Services;
using LeadGeneration.Domain.Interfaces;
using LeadGeneration.Infrastructure;
using LeadGeneration.Infrastructure.Integrations;
using LeadGeneration.Infrastructure.Repository;

var builder = WebApplication.CreateBuilder(args);

// ---------- Config ----------
var cfg = builder.Configuration;

// MongoDB settings
var mongoConn = cfg.GetSection("MongoSettings")["ConnectionString"];
var mongoDb = cfg.GetSection("MongoSettings")["Database"];

// Apollo settings
builder.Services.AddSingleton(new ApolloOptions
{
    BaseUrl = cfg.GetSection("Apollo")["BaseUrl"] ?? "https://api.apollo.io/",
    ApiKey = cfg.GetSection("Apollo")["ApiKey"] ?? ""
});

// ---------- Core DI ----------
// MongoDbContext should be singleton (one connection pool for the app)
builder.Services.AddSingleton(new MongoDbContext(mongoConn, mongoDb));

// Repository layer (scoped to each request)
builder.Services.AddScoped<ILeadRepository, LeadRepository>();

// Application Services (scoped to each request)
builder.Services.AddScoped<ProviderRouter>();
builder.Services.AddScoped<LeadService>();
builder.Services.AddScoped<ListService>();
builder.Services.AddScoped<SequenceService>();
builder.Services.AddScoped<ImportService>();
builder.Services.AddScoped<ExportService>();

// HTTP clients
builder.Services.AddHttpClient<ILeadProvider, ApolloLeadProvider>();

// ---------- API / Swagger / CORS ----------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p =>
        p.AllowAnyOrigin()
         .AllowAnyHeader()
         .AllowAnyMethod());
});

var app = builder.Build();

// ---------- Ensure MongoDB Indexes ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
    await db.EnsureIndexesAsync();
}

// ---------- Middleware ----------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();
app.Run();
