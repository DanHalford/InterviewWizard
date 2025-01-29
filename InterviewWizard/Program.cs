using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using InterviewWizard;
using System.Configuration;
using Azure.Core;
using Microsoft.Graph;
using NLog;
using NLog.Web;
using AspNetCore.ReCaptcha;
using Microsoft.AspNetCore.Authentication;
using InterviewWizard.Models.Utility;
using InterviewWizard.Helpers;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("Starting application");

try
{
    var builder = WebApplication.CreateBuilder(args);

    AppSettings? appSettings = builder.Configuration.Get<AppSettings>();
    if (appSettings == null)
    {
        throw new ConfigurationErrorsException("AppSettings is null");
    }
    builder.Services.Configure<AppSettings>(builder.Configuration);

    var keyVaultUri = appSettings.KeyVaultUri;
    if (string.IsNullOrEmpty(keyVaultUri))
    {
        throw new ConfigurationErrorsException("KeyVault properties are not set");
    }
    var secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
    builder.Configuration.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());

    var storageContainerUrl = appSettings.StorageContainer.Url;
    var storageContainerName = appSettings.StorageContainer.ContainerName;
    if (string.IsNullOrEmpty(storageContainerUrl) || string.IsNullOrEmpty(storageContainerName))
    {
        throw new ConfigurationErrorsException("StorageContainer properties are not set");
    }
    var blobServiceClient = new BlobServiceClient(new Uri(storageContainerUrl), new DefaultAzureCredential());
    var blobContainerClient = blobServiceClient.GetBlobContainerClient(storageContainerName);

    builder.Services.AddSingleton<BlobServiceClient>(blobServiceClient);
    builder.Services.AddSingleton<BlobContainerClient>(blobContainerClient);

    string[] _scopes = new string[] { "https://graph.microsoft.com/.default" };
    string clientSecret = secretClient.GetSecret("GraphClientSecret").Value.Value;
    TokenCredential tokenCredential = new ClientSecretCredential(appSettings.Graph.TenantId, appSettings.Graph.ClientId, clientSecret);
    GraphServiceClient graphClient = new GraphServiceClient(tokenCredential, _scopes);

    builder.Services.AddSingleton<GraphServiceClient>(graphClient);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = SessionTokenAuthenticationHandler.SchemeName;
        options.DefaultChallengeScheme = SessionTokenAuthenticationHandler.SchemeName;
    })
        .AddScheme<AuthenticationSchemeOptions, SessionTokenAuthenticationHandler>(
            SessionTokenAuthenticationHandler.SchemeName, options => { });

    // Add services to the container.
    builder.Services.AddMemoryCache();
    builder.Services.AddRazorPages();
    builder.Services.AddControllers();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    builder.Services.AddDbContextFactory<ApplicationDbContext>((serviceProvider, options) =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        options.UseSqlServer(appSettings.DataStore.ConnectionString);
    });

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(appSettings.DataStore.ConnectionString));

    builder.Services.AddScoped<SessionService>();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddReCaptcha(options =>
    {
        options.SiteKey = appSettings.ReCaptcha.SiteKey;
        options.SecretKey = appSettings.ReCaptcha.SecretKey;
        options.Version = ReCaptchaVersion.V3;
        options.ScoreThreshold = appSettings.ReCaptcha.ScoreThreshold;
    });

    builder.Services.AddTransient<AssistantDefinitionHelper>();
    builder.Services.AddSingleton<AnswerProcessingService>(); // Register as a singleton
    builder.Services.AddSingleton<IAnswerProcessingService>(sp => sp.GetRequiredService<AnswerProcessingService>()); // Map interface to singleton
    builder.Services.AddHostedService(sp => sp.GetRequiredService<AnswerProcessingService>()); // Use the same instance for the hosted service


    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseSession();
    app.UseAuthorization();
    app.UseAuthentication();
    app.MapRazorPages();
    app.MapControllers();

    app.Run();
} catch (Exception ex)
{
    logger.Error(ex, "An error occurred while starting the application");
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}


