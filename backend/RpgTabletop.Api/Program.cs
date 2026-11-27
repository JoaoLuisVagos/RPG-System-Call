using RpgTabletop.Api;
using RpgTabletop.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);
var dataPath = Path.Combine(builder.Environment.ContentRootPath, "data", "campaign.json");

builder.Services.AddSingleton(new GameStore(dataPath));
builder.Services.AddSignalR();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(
            "http://localhost:5173",
            "http://127.0.0.1:5173",
            "http://localhost:5500",
            "http://127.0.0.1:5500")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();
app.MapStateEndpoints();
app.MapCampaignEndpoints();
app.MapMapEndpoints();
app.MapCharacterEndpoints();
app.MapTokenEndpoints();
app.MapDiceEndpoints();
app.MapChatEndpoints();
app.MapInitiativeEndpoints();
app.MapHub<SessionHub>("/hubs/session");

app.Run();
