using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuestQuokka.Services
{
    public class TicTacToeService : ModuleBase<SocketCommandContext>
    {
        private static Dictionary<ulong, TicTacToeGame> ActiveGames = new();

        [Command("tictactoe")]
        public async Task TicTacToeCommand(SocketUser opponent)
        {
            if (opponent.IsBot || opponent == Context.User)
            {
                await ReplyAsync("Invalid opponent!");
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
            if (!ActiveGames.TryGetValue(Context.Channel.Id, out var game)) return;

            var result = await game.MakeMove(Context, index);
            if (result) ActiveGames.Remove(Context.Channel.Id);
        }

        public class TicTacToeGame
        {
            private readonly SocketUser _playerX;
            private readonly SocketUser _playerO;
            private SocketUser _currentPlayer;
            private readonly string[] _board = new string[9] { "-", "-", "-", "-", "-", "-", "-", "-", "-" };
            private bool _isGameOver;

            public TicTacToeGame(SocketUser playerX, SocketUser playerO)
            {
                _playerX = playerX;
                _playerO = playerO;
                _currentPlayer = playerX;
            }

            public async Task StartGame(SocketCommandContext context)
            {
                await context.Channel.SendMessageAsync(
                    $"{_playerX.Mention} vs {_playerO.Mention} - **{_currentPlayer.Mention}'s turn (X)**",
                    components: GenerateBoard()
                );
            }

            public async Task<bool> MakeMove(SocketInteractionContext context, int index)
            {
                if (_isGameOver || _board[index] != "-") return false;

                if (context.User.Id != _currentPlayer.Id)
                {
                    await context.Interaction.RespondAsync("Not your turn!", ephemeral: true);
                    return false;
                }

                _board[index] = _currentPlayer == _playerX ? "❌" : "⭕";
                var winner = CheckWinner();

                if (winner != null)
                {
                    await UpdateBoard(context, $"{winner.Mention} **wins!** 🎉");
                    return true;
                }

                if (_board.All(cell => cell != "-"))
                {
                    await UpdateBoard(context, "**Draw!** 🤝");
                    return true;
                }

                // Switch turn and update display based on the new _currentPlayer
                _currentPlayer = _currentPlayer == _playerX ? _playerO : _playerX;
                await UpdateBoard(context, $"**{_currentPlayer.Mention}'s turn** ({(_currentPlayer == _playerX ? "X" : "O")})");
                return false;
            }

            private async Task UpdateBoard(SocketInteractionContext context, string message)
            {
                await context.Interaction.UpdateAsync(msg =>
                {
                    msg.Content = message;
                    msg.Components = GenerateBoard();
                });
            }

            private SocketUser? CheckWinner()
            {
                int[,] winPatterns = 
                {
                    { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 },
                    { 0, 3, 6 }, { 1, 4, 7 }, { 2, 5, 8 },
                    { 0, 4, 8 }, { 2, 4, 6 }
                };

                for (int i = 0; i < winPatterns.GetLength(0); i++)
                {
                    int a = winPatterns[i, 0], b = winPatterns[i, 1], c = winPatterns[i, 2];
                    if (_board[a] != "-" && _board[a] == _board[b] && _board[b] == _board[c])
                        return _board[a] == "❌" ? _playerX : _playerO;
                }
                return null;
            }

            private MessageComponent GenerateBoard()
            {
                var builder = new ComponentBuilder();
                for (int i = 0; i < 9; i++)
                    builder.WithButton(_board[i], $"ttt_{i}", ButtonStyle.Secondary, row: i / 3);
                return builder.Build();
            }
        }
    }
}
