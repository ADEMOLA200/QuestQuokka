using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestQuokka.Models;
using QuestQuokka.Services;
using System;
using System.Threading.Tasks;

namespace QuestQuokka
{
    public class Program
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactionService;
        private readonly IConfiguration _config;
        private readonly IServiceProvider _services;

        public static Task Main(string[] args) => new Program().MainAsync();

        public Program()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig 
            {
                GatewayIntents = GatewayIntents.Guilds 
                               | GatewayIntents.GuildMessages 
                               | GatewayIntents.MessageContent 
                               | GatewayIntents.GuildMembers,
                LogLevel = LogSeverity.Info
            });
            
            _interactionService = new InteractionService(_client);
            _config = BuildConfiguration();
            _services = BuildServices();
        }

        public async Task MainAsync()
        {
            var token = _config["DiscordBot:Token"] ?? 
                       Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");

            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("Error: No bot token found in configuration or environment variables");
                return;
            }

            _client.Log += LogAsync;
            _interactionService.Log += LogAsync;

            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                await dbContext.Database.MigrateAsync();
            }

            _client.Ready += ClientReady;
            _client.InteractionCreated += HandleInteraction;

            try
            {
                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Fatal] {ex}");
            }
        }

        private IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        private IServiceProvider BuildServices()
        {
            return new ServiceCollection()
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
        }

        private async Task ClientReady()
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Error] ClientReady: {ex}");
            }
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
                {
                    try
                    {
                        await interaction.RespondAsync("❌ An error occurred processing that command", ephemeral: true);
                    }
                    catch
                    {
                        if (interaction is SocketSlashCommand slashCommand)
                        {
                            await slashCommand.FollowupAsync("❌ An error occurred processing that command", ephemeral: true);
                        }
                    }
                }
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
}
