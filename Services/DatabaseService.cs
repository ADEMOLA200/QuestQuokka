using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using QuestQuokka.Models;
using System.Threading.Tasks;

namespace QuestQuokka.Services;

public class DatabaseService : ModuleBase<SocketCommandContext>
{
    private readonly DatabaseContext _db;

    public DatabaseService(DatabaseContext db)
    {
        _db = db;
    }

    public async Task UpdateScore(ulong userId, int points)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            user = new User { UserId = userId };
            _db.Users.Add(user);
        }
        user.Score += points;
        await _db.SaveChangesAsync();
    }
}
