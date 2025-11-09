using DeviceHubMini.Client.Contracts;
using DeviceHubMini.Client.GraphQL.Types;
using DeviceHubMini.Client.Security;
using DeviceHubMini.Client.Services;

using Serilog;

// Add Serilog before building

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();


builder.Host.UseSerilog(Log.Logger);


// Add GraphQL server
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
   //.AddTypeExtensionsFromFile() // harmless; allows SDL exts if added later
    .AddFiltering()
    .AddSorting()
    .AddProjections()
    .AddType<DateTimeType>()   // enable DateTime scalars
    .ModifyRequestOptions(o => o.IncludeExceptionDetails = builder.Environment.IsDevelopment());

// App services
builder.Services.AddSingleton<IConfigService, ConfigService>();
builder.Services.AddSingleton<IEventStore, InMemoryEventStore>();

var app = builder.Build();

// Health
app.MapGet("/healthz", () => Results.Ok("ok"));

// API key middleware for /graphql
app.UseMiddleware<ApiKeyMiddleware>();

// GraphQL endpoint (Banana Cake Pop IDE)
if (app.Configuration.GetValue("GraphQL:EnableIde", true) && app.Environment.IsDevelopment())
{
    app.MapGraphQL("/graphql").WithOptions(new HotChocolate.AspNetCore.GraphQLServerOptions
    {
        Tool = { Enable = true }
    });
}
else
{
    app.MapGraphQL("/graphql");
}

app.Run();
//Log.CloseAndFlush();