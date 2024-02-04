using Microsoft.AspNetCore.Mvc;

var userList = new List<User>();
var skiPassList = new List<SkiPass>();
var userKeyCardsDictionary = new Dictionary<long, int>();
var passages = new Dictionary<long, List<LiftPassage>>();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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

//app.UseHttpsRedirection();

app.MapPost("/validate", ([FromBody] ValidationEvent validationEvent) =>
{
    // Validate that the user has a valid lift pass
    var liftPass = skiPassList.Where(t => t.KeyCardId == validationEvent.KeyCardId).FirstOrDefault();
    if (liftPass != null && liftPass.ValidTo <= DateTime.Now)
    {
        passages[validationEvent.KeyCardId].Add(new LiftPassage(DateTime.Now, validationEvent.KeyCardId));
        return Results.Ok();
    }

    return Results.NotFound();
})
.WithDisplayName("Validate ski pass")
.WithOpenApi();

app.MapPost("/users", ([FromBody] UserRegistration userRegistration) =>
{
    // Validate that the user is not already registered
    if (userRegistration == null)
    {
        return Results.BadRequest();
    }
    else
    {
        var userId = Random.Shared.NextInt64();
        var user = new User(userRegistration.Name, userRegistration.DateOfBirth, userId);
        userList.Add(user);
        return Results.Created("/users", userId);
    }
})
.WithOpenApi();

app.MapPost("/skipass", ([FromBody] SkiPass skiPass) =>
{
    skiPassList.Add(skiPass);
})
.WithDisplayName("Add new ski pass")
.WithOpenApi();

app.MapGet("/config", (IConfiguration config) =>
{
    return (config as IConfigurationRoot)!.GetDebugView();
});

app.MapGet("/isdev", () => app.Environment.IsDevelopment());
app.MapGet("/env", () => app.Environment.EnvironmentName);

app.Run();

internal record LiftPassage(DateTimeOffset DateTimeOffset, int KeyCardId);

internal record UserRegistration(string Name, DateOnly DateOfBirth);

internal record SkiPass(int KeyCardId, DateTimeOffset ValidFrom, DateTimeOffset ValidTo);

internal record User(string Name, DateOnly DateOfBirth, long UserId);

internal record ValidatorInfo(string Id, string Name, GeoPosition GeoPosition);

internal record ValidationEvent(int KeyCardId, string ValidatorId);

internal record struct GeoPosition(double Latitude, double Longitude, double? Altitude);
