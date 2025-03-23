using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

public class LeaderboardService : ModuleBase<SocketCommandContext>
{
    private readonly DatabaseContext _db;

    public LeaderboardService(DatabaseContext db)
    {
        _db = db;
    }

    [Command("leaderboard")]
    public async Task LeaderboardCommand()
    {
        var topUsers = await _db.Users
            .OrderByDescending(u => u.Score)
            .Take(10)
            .ToListAsync();

        var embed = new EmbedBuilder()
            .WithTitle("🏆 Leaderboard")
            .WithColor(Color.Green);

        foreach (var user in topUsers)
        {
            var discordUser = Context.Client.GetUser(user.UserId);
            embed.AddField($"{discordUser?.Username ?? "Unknown"}", $"Score: {user.Score}");
        }

        await ReplyAsync(embed: embed.Build());
    }
}
