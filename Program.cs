using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

public class Program
{
    private DiscordSocketClient _client;
    private CommandService _commands;
    private IConfiguration _config;

    public static Task Main(string[] args) => new Program().MainAsync();

    public async Task MainAsync()
    {
        _client = new DiscordSocketClient();
        _commands = new CommandService();
        _config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        // Register services
        var services = new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_commands)
            .AddSingleton(_config)
            .AddDbContext<DatabaseContext>(options => 
                options.UseSqlite("Data Source=questquokka.db"))
            .BuildServiceProvider();

        // Initialize database
        using (var context = services.GetService<DatabaseContext>())
        {
            await context.Database.MigrateAsync();
        }

        await InstallCommandsAsync(services);
        await _client.LoginAsync(TokenType.Bot, _config["DiscordBot:Token"]);
        await _client.StartAsync();
        await Task.Delay(-1);
    }

    private async Task InstallCommandsAsync(IServiceProvider services)
    {
        _client.MessageReceived += HandleCommandAsync;
        await _commands.AddModuleAsync<TriviaService>(services);
        await _commands.AddModuleAsync<TicTacToeService>(services);
        await _commands.AddModuleAsync<LeaderboardService>(services);
    }

    private async Task HandleCommandAsync(SocketMessage messageParam)
    {
        var message = messageParam as SocketUserMessage;
        if (message == null) return;

        int argPos = 0;
        var prefix = _config["DiscordBot:Prefix"];
        if (!message.HasStringPrefix(prefix, ref argPos)) return;

        var context = new SocketCommandContext(_client, message);
        await _commands.ExecuteAsync(context, argPos, services);
    }
}
