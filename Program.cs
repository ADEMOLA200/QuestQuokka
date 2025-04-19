using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestQuokka.Models;
using QuestQuokka.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuestQuokka
{
    public class Program
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactionService;
        private readonly IConfiguration _config;
        private readonly IServiceProvider _services;
        private Timer _statusTimer;

        public static Task Main(string[] args) => new Program().MainAsync();

        public Program()
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Init] Creating Discord client and services...");
            
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds
                               | GatewayIntents.GuildMessages
                               | GatewayIntents.MessageContent
                               | GatewayIntents.GuildMembers,
                LogLevel = LogSeverity.Verbose,
                AlwaysDownloadUsers = true
            });

            _interactionService = new InteractionService(_client.Rest);
            _config = BuildConfiguration();
            _services = BuildServices();
            
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Init] Services initialized successfully");
        }

        public async Task MainAsync()
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Startup] Starting bot initialization...");
            
            var token = _config["DiscordBot:Token"] ??
                       Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");

            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Fatal] No bot token found in configuration or environment variables");
                return;
            }

            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Startup] Token found, setting up logging...");
            
            _client.Log += LogAsync;
            _interactionService.Log += LogAsync;

            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Startup] Setting up database...");
            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                try 
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Database] Running migrations...");
                    await dbContext.Database.MigrateAsync();
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Database] Migrations complete");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Database Error] Migration failed: {ex}");
                    throw;
                }
            }

            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Startup] Setting up event handlers...");
            _client.Ready += ClientReady;
            _client.InteractionCreated += HandleInteraction;

            try
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Startup] Logging in to Discord...");
                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();
                
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Startup] Bot is now running");
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Fatal] {ex}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Shutdown] Cleaning up resources...");
                _statusTimer?.Dispose();
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Shutdown] Bot has stopped");
            }
        }

        private async Task RotateStatusAsync()
        {
            try
            {
                var statuses = new[]
                {
                    new Game("🎮 /tictactoe", ActivityType.Playing),
                    new Game("❓ /trivia", ActivityType.Playing),
                    new Game("🏆 /leaderboard", ActivityType.Watching),
                    new Game("💰 /daily", ActivityType.Playing),
                    new Game("@QuestQuokka help", ActivityType.Listening)
                };

                var randomStatus = statuses[new Random().Next(statuses.Length)];
                await _client.SetActivityAsync(randomStatus);
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Status] Changed to: {randomStatus.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Status Error] {ex}");
            }
        }

        private async Task ClientReady()
        {
            try
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Ready] Bot connected as {_client.CurrentUser}");
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Ready] Guild count: {_client.Guilds.Count}");
                
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Ready] Starting status rotation timer...");
                _statusTimer = new Timer(async _ => await RotateStatusAsync(), 
                    null, TimeSpan.Zero, TimeSpan.FromMinutes(5));

                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Ready] Loading command modules...");
                await _interactionService.AddModulesAsync(
                    assembly: System.Reflection.Assembly.GetEntryAssembly(),
                    services: _services
                );
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Ready] Modules loaded successfully");

                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Ready] Registering global commands...");
                await RegisterCommandsGloballyAsync();
                
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Ready] Verifying command registration...");
                await VerifyCommandsRegistered();

                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Ready] Bot is fully operational");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Ready Error] {ex}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private async Task RegisterCommandsGloballyAsync()
        {
            try
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Commands] Beginning global command registration...");
                await _interactionService.RegisterCommandsGloballyAsync();
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Commands] Global commands registered successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Commands Error] Registration failed: {ex}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        private async Task VerifyCommandsRegistered()
        {
            try
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Verify] Fetching registered commands...");
                var globalCommands = await _client.GetGlobalApplicationCommandsAsync();
                
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Verify] Found {globalCommands.Count} registered commands:");
                foreach (var cmd in globalCommands)
                {
                    Console.WriteLine($"- {cmd.Name} (ID: {cmd.Id})");
                    Console.WriteLine($"  Options: {cmd.Options.Count}");
                    Console.WriteLine($"  Description: {cmd.Description}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Verify Error] Verification failed: {ex}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private IConfiguration BuildConfiguration()
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Config] Building configuration...");
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();
                
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Config] Configuration built successfully");
                return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Config Error] Failed to build configuration: {ex}");
                throw;
            }
        }

        private IServiceProvider BuildServices()
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Services] Building service provider...");
            try
            {
                var services = new ServiceCollection()
                    .AddSingleton(_client)
                    .AddSingleton(_interactionService)
                    .AddSingleton(_config)
                    .AddDbContext<DatabaseContext>(options =>
                    {
                        Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Services] Configuring database...");
                        options.UseSqlite("Data Source=questquokka.db");
                    })
                    .AddSingleton<DatabaseService>()
                    .AddSingleton<TriviaService>()
                    .AddSingleton<GameManagementService>()
                    .AddSingleton<TicTacToeService>()
                    .AddSingleton<LeaderboardService>();

                var provider = services.BuildServiceProvider();
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Services] Service provider built successfully");
                return provider;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Services Error] Failed to build services: {ex}");
                throw;
            }
        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Interaction] Received {interaction.Type} interaction");
                
                var context = new SocketInteractionContext(_client, interaction);
                var result = await _interactionService.ExecuteCommandAsync(context, _services);

                if (!result.IsSuccess)
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Interaction Error] {result.ErrorReason}");
                    
                    if (interaction.Type == InteractionType.ApplicationCommand)
                    {
                        await interaction.RespondAsync($"❌ Command failed: {result.ErrorReason}", ephemeral: true);
                    }
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Interaction] Successfully handled {interaction.Type}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Interaction Error] {ex}");
                Console.WriteLine(ex.StackTrace);

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
            var severity = log.Severity.ToString().PadRight(8);
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [{severity}] {log.Source}: {log.Message}");
            
            if (log.Exception != null)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Exception] {log.Exception}");
                Console.WriteLine(log.Exception.StackTrace);
            }
            
            return Task.CompletedTask;
        }
    }
}
