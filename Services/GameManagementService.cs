using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuestQuokka.Services
{
    public class GameManagementService
    {
        private readonly IConfiguration _configuration;
        private readonly Dictionary<ulong, TicTacToeService.TicTacToeGame> _activeTicTacToeGames;

        public GameManagementService(IConfiguration configuration)
        {
            _configuration = configuration;
            _activeTicTacToeGames = new Dictionary<ulong, TicTacToeService.TicTacToeGame>();
        }

        public void AddTicTacToeGame(ulong channelId, TicTacToeService.TicTacToeGame game)
        {
            _activeTicTacToeGames[channelId] = game;
        }

        public bool HasActiveGame(ulong channelId)
        {
            return _activeTicTacToeGames.ContainsKey(channelId);
        }

        public bool TryRemoveGame(ulong channelId, out TicTacToeService.TicTacToeGame? game)
        {
            if (_activeTicTacToeGames.TryGetValue(channelId, out var foundGame))
            {
                game = foundGame;
                _activeTicTacToeGames.Remove(channelId);
                return true;
            }
            game = null;
            return false;
        }

        public bool TryGetGame(ulong channelId, out TicTacToeService.TicTacToeGame? game)
        {
            return _activeTicTacToeGames.TryGetValue(channelId, out game);
        }

        public void RemoveGame(ulong channelId)
        {
            _activeTicTacToeGames.Remove(channelId);
        }

        public async Task CancelGameAsync(SocketInteractionContext context)
        {
            try
            {
                var guild = context.Guild;
                if (guild == null || context.User.Id != guild.OwnerId)
                {
                    await context.Interaction.RespondAsync("❌ Only the server owner can cancel games!", ephemeral: true);
                    return;
                }

                if (!_activeTicTacToeGames.TryGetValue(context.Channel.Id, out var game))
                {
                    await context.Interaction.RespondAsync("❌ No active game in this channel!", ephemeral: true);
                    return;
                }

                await game.CancelGame();
                _activeTicTacToeGames.Remove(context.Channel.Id);
                await context.Interaction.RespondAsync("✅ Game successfully canceled!", ephemeral: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cancel Game Error: {ex}");
                await context.Interaction.RespondAsync("❌ Failed to cancel game!", ephemeral: true);
            }
        }

        public async Task GiveRewardAsync(SocketUser member, int amount, SocketInteractionContext context)
        {
            await context.Interaction.RespondAsync(
                $"{member.Mention} has been given a reward of {amount} coins!",
                ephemeral: false);
        }

        public async Task DeductCoinsAsync(SocketUser member, int amount, SocketInteractionContext context)
        {
            await context.Interaction.RespondAsync(
                $"{member.Mention} has had {amount} coins deducted!",
                ephemeral: false);
        }
    }
}
