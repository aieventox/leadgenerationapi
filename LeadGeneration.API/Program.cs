using LeadGeneration.Application.Services;
using LeadGeneration.Domain.Interfaces;
using LeadGeneration.Infrastructure;
using LeadGeneration.Infrastructure.Integrations;
using LeadGeneration.Infrastructure.Repository;

var builder = WebApplication.CreateBuilder(args);

// ---------- Config ----------
var cfg = builder.Configuration;

var mongoConn = cfg.GetSection("Mongo")["ConnectionString"] ?? "mongodb://localhost:27017";
var mongoDb = cfg.GetSection("Mongo")["Database"] ?? "LeadGenDb";

builder.Services.AddSingleton(new ApolloOptions
{
    BaseUrl = cfg.GetSection("Apollo")["BaseUrl"] ?? "https://api.apollo.io/",
    ApiKey = cfg.GetSection("Apollo")["ApiKey"] ?? ""
});

// ---------- Core DI ----------
builder.Services.AddSingleton(new MongoDbContext(mongoConn, mongoDb));
builder.Services.AddSingleton<ILeadRepository, LeadRepository>();

builder.Services.AddHttpClient<ILeadProvider, ApolloLeadProvider>();

builder.Services.AddSingleton<ProviderRouter>();
builder.Services.AddSingleton<LeadService>();
builder.Services.AddSingleton<ListService>();
builder.Services.AddSingleton<SequenceService>();
builder.Services.AddSingleton<ImportService>();

// NEW: Import / Export
builder.Services.AddSingleton<ImportService>();
builder.Services.AddSingleton<ExportService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
    await db.EnsureIndexesAsync();
}

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
