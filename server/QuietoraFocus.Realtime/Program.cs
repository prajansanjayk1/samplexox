using QuietoraFocus.Realtime.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

app.MapGet("/", () => Results.Ok(new
{
    Service = "Quietora Together — Real-Time Study Collaboration API",
    Version = "4.0.0",
    Status = "Healthy",
    Documentation = "Connect via SignalR to /hubs/study"
}));

app.MapGet("/api/health", () => Results.Ok(new { status = "UP", time = DateTime.UtcNow }));

app.MapHub<StudyHub>("/hubs/study");

app.Run();
