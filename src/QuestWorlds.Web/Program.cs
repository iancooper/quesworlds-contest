using QuestWorlds.DiceRoller;
using QuestWorlds.InMemorySessionStore;
using QuestWorlds.Outcome;
using QuestWorlds.Resolution;
using QuestWorlds.Session;
using QuestWorlds.Web.Hubs;
using QuestWorlds.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Choosing a store is the host's job, and a host that forgets should find out at startup rather
// than on the first hub call, mid-game. Validation names the missing IAmASessionStore (ADR-0008 D3).
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateOnBuild = true;
    options.ValidateScopes = true;
});

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

// Register QuestWorlds modules
builder.Services.AddSessionModule();
builder.Services.AddInMemorySessionStore();
builder.Services.AddDiceRollerModule();
builder.Services.AddResolutionModule();
builder.Services.AddOutcomeModule();

// Register web services
builder.Services.AddSingleton<IContestFrameStore, InMemoryContestFrameStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapHub<ContestHub>("/contestHub");

app.Run();

// Enable WebApplicationFactory for integration testing
public partial class Program { }
