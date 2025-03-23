using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuestQuokka.Modules
{
    public class TicTacToeService : ModuleBase<SocketCommandContext>
    {
        private static Dictionary<ulong, TicTacToeGame> ActiveGames = new();

        [Command("tictactoe")]
        public async Task TicTacToeCommand(SocketUser opponent)
        {
            if (opponent.IsBot)
            {
                await ReplyAsync("You can't play against a bot.");
                return;
            }

            if (ActiveGames.ContainsKey(Context.Channel.Id))
            {
                await ReplyAsync("A game is already active in this channel!");
                return;
            }

            var game = new TicTacToeGame(Context.User, opponent);
            ActiveGames[Context.Channel.Id] = game;

            await game.StartGame(Context);
        }

        [ComponentInteraction("ttt_*")]
        public async Task HandleButtonClick(string id)
        {
            int index = int.Parse(id.Split('_')[1]);

            if (!ActiveGames.TryGetValue(Context.Channel.Id, out var game))
                return;

            var result = await game.MakeMove(Context, index);
            if (result)
                ActiveGames.Remove(Context.Channel.Id);
        }
    }

    public class TicTacToeGame
    {
        private SocketUser PlayerX;
        private SocketUser PlayerO;
        private SocketUser CurrentPlayer;
        private string[] Board = { "⠀", "⠀", "⠀", "⠀", "⠀", "⠀", "⠀", "⠀", "⠀" };
        private bool IsGameOver = false;

        public TicTacToeGame(SocketUser playerX, SocketUser playerO)
        {
            PlayerX = playerX;
            PlayerO = playerO;
            CurrentPlayer = PlayerX;
        }

        public async Task StartGame(SocketCommandContext context)
        {
            await context.Channel.SendMessageAsync(
                $"{PlayerX.Mention} vs {PlayerO.Mention} - **{CurrentPlayer.Mention} (X)** starts!",
                components: GenerateBoard()
            );
        }

        public async Task<bool> MakeMove(SocketInteractionContext context, int index)
        {
            if (IsGameOver || Board[index] != "⠀")
                return false;

            var user = context.User;
            if (user.Id != CurrentPlayer.Id)
            {
                await context.Interaction.RespondAsync("It's not your turn!", ephemeral: true);
                return false;
            }

            Board[index] = CurrentPlayer == PlayerX ? "❌" : "⭕";

            var winner = CheckWinner();

            if (winner != null)
            {
                await context.Interaction.UpdateAsync(msg =>
                {
                    msg.Content = $"{winner.Mention} **wins the game! 🎉**";
                    msg.Components = GenerateBoard();
                });
                IsGameOver = true;
                return true;
            }

            if (Board.All(cell => cell != "⠀"))
            {
                await context.Interaction.UpdateAsync(msg =>
                {
                    msg.Content = "**It's a draw! 🤝**";
                    msg.Components = GenerateBoard();
                });
                IsGameOver = true;
                return true;
            }

            CurrentPlayer = (CurrentPlayer == PlayerX) ? PlayerO : PlayerX;

            await context.Interaction.UpdateAsync(msg =>
            {
                msg.Content = $"**{CurrentPlayer.Mention}'s turn ({(CurrentPlayer == PlayerX ? "X" : "O")})**";
                msg.Components = GenerateBoard();
            });

            return false;
        }

        private SocketUser CheckWinner()
        {
            int[,] winPatterns =
            {
                { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 }, // Rows
                { 0, 3, 6 }, { 1, 4, 7 }, { 2, 5, 8 }, // Columns
                { 0, 4, 8 }, { 2, 4, 6 }              // Diagonals
            };

            for (int i = 0; i < winPatterns.GetLength(0); i++)
            {
                int a = winPatterns[i, 0];
                int b = winPatterns[i, 1];
                int c = winPatterns[i, 2];

                if (Board[a] != "⠀" && Board[a] == Board[b] && Board[b] == Board[c])
                {
                    return Board[a] == "❌" ? PlayerX : PlayerO;
                }
            }
            return null;
        }

        private MessageComponent GenerateBoard()
        {
            var builder = new ComponentBuilder();
            for (int i = 0; i < 9; i++)
            {
                builder.WithButton(Board[i], $"ttt_{i}", ButtonStyle.Secondary, row: i / 3);
            }
            return builder.Build();
        }
    }
}
