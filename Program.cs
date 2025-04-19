using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestQuokka.Models;
using QuestQuokka.Services;
using System;
using System.IO;
using System.Threading.Tasks;

public class Program
{
    private DiscordSocketClient _client = null!;
    private InteractionService _interactionService = null!;
    private IConfiguration _config = null!;
    private IServiceProvider _services = null!;

    public static Task Main(string[] args) => new Program().MainAsync();

    public async Task MainAsync()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig 
        {
            GatewayIntents = GatewayIntents.Guilds 
                           | GatewayIntents.GuildMessages 
                           | GatewayIntents.MessageContent 
                           | GatewayIntents.GuildMembers,
            LogLevel = LogSeverity.Debug
        });
        
        _interactionService = new InteractionService(_client);
        
        _client.Log += LogAsync;
        _interactionService.Log += LogAsync;

        _config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        _services = new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_interactionService)
            .AddSingleton(_config)
            .AddDbContext<DatabaseContext>(options => 
                options.UseSqlite("Data Source=questquokka.db"))
            .AddSingleton<DatabaseService>()
            .AddSingleton<TriviaService>()
            .AddSingleton<GameManagementService>()
            .AddSingleton<TicTacToeService>()
            .AddSingleton<LeaderboardService>()
            .BuildServiceProvider();

        using var dbContext = _services.GetRequiredService<DatabaseContext>();
        await dbContext.Database.MigrateAsync();

        _client.Ready += ClientReady;
        _client.InteractionCreated += HandleInteraction;

        await _client.LoginAsync(TokenType.Bot, _config["DiscordBot:Token"]);
        await _client.StartAsync();
        await Task.Delay(-1);
    }

    private async Task ClientReady()
    {
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Info] Bot connected as {_client.CurrentUser}");

        await _interactionService.RegisterCommandsGloballyAsync(true);
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Info] Cleared existing global commands");

        await _interactionService.AddModulesAsync(
            assembly: System.Reflection.Assembly.GetEntryAssembly(),
            services: _services
        );
        await _interactionService.RegisterCommandsGloballyAsync();
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Info] Commands registered globally");
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(_client, interaction);
            await _interactionService.ExecuteCommandAsync(context, _services);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Error] {ex}");
            if (interaction.Type == InteractionType.ApplicationCommand)
                await interaction.RespondAsync("❌ An error occurred processing that command", ephemeral: true);
        }
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} [{log.Severity}] {log.Source}: {log.Message}");
        if (log.Exception != null)
            Console.WriteLine(log.Exception);
        return Task.CompletedTask;
    }
}
