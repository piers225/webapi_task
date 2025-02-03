using MyApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<LongTaskManager>();

var app = builder.Build();

var cancellationTokenFactory = () => new CancellationTokenSource(10 * 1000).Token;

app.MapGet("/", () => 
{
    var htmlContent = File.ReadAllText("/app/wwwroot/index.html");
    return Results.Text(htmlContent, contentType: "text/html");
});

app.MapPut("/api/task", (LongTaskManager longTaskManager) => longTaskManager.CreateTask());
app.MapPost("/api/task/{key}", (LongTaskManager longTaskManager, Guid key) => longTaskManager.RunTask(key, cancellationTokenFactory()));
app.MapGet("/api/task/{key}", (LongTaskManager longTaskManager, Guid key) => longTaskManager.TaskOutput(key));

app.Run();

