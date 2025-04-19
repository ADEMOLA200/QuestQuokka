using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QuestQuokka.Services
{
    public class TicTacToeService : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly GameManagementService _gameManager;

        public TicTacToeService(GameManagementService gameManager)
        {
            _gameManager = gameManager;
        }

        [SlashCommand("tictactoe", "Start a new Tic Tac Toe game")]
        public async Task TicTacToeCommand(SocketUser opponent)
        {
            if (opponent.IsBot || opponent == Context.User)
            {
                await RespondAsync("Invalid opponent!", ephemeral: true);
                return;
            }

            if (_gameManager.HasActiveGame(Context.Channel.Id))
            {
                await RespondAsync("A game is already active in this channel!", ephemeral: true);
                return;
            }

            var game = new TicTacToeGame(Context.User, opponent, Context);
            _gameManager.AddTicTacToeGame(Context.Channel.Id, game);
            await game.StartGame();
        }

        [ComponentInteraction("ttt_*")]
        public async Task HandleButtonClick(string id)
        {
            await DeferAsync();

            Console.WriteLine($"[DEBUG] Button clicked with id: {id}");

            if (!int.TryParse(id, out int index))
            {
                await FollowupAsync("❌ Invalid move!", ephemeral: true);
                return;
            }

            if (index < 0 || index >= 9)
            {
                await FollowupAsync("❌ Move out of bounds!", ephemeral: true);
                return;
            }

            if (!_gameManager.TryGetGame(Context.Channel.Id, out var game))
            {
                await FollowupAsync("❌ No active game in this channel!", ephemeral: true);
                return;
            }

            try
            {
                var result = await game!.MakeMove(Context, index);
                if (result)
                {
                    _gameManager.RemoveGame(Context.Channel.Id);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TicTacToe Error: {ex}");
                await FollowupAsync("❌ Error processing move!", ephemeral: true);
            }
        }

        public class TicTacToeGame
        {
            private readonly SocketUser _playerX;
            private readonly SocketUser _playerO;
            private readonly SocketInteractionContext _context;
            private SocketUser _currentPlayer;
            private readonly string[] _board = new string[9] { "-", "-", "-", "-", "-", "-", "-", "-", "-" };
            private bool _isGameOver;

            public TicTacToeGame(SocketUser playerX, SocketUser playerO, SocketInteractionContext context)
            {
                _playerX = playerX;
                _playerO = playerO;
                _context = context;
                _currentPlayer = playerX;
            }

            public async Task StartGame()
            {
                await _context.Interaction.RespondAsync(
                    $"{_playerX.Mention} vs {_playerO.Mention} - **{_currentPlayer.Mention}'s turn (X)**",
                    components: GenerateBoard()
                );
            }

            public async Task<bool> MakeMove(SocketInteractionContext context, int index)
            {
                if (context.User.Id != _currentPlayer.Id)
                {
                    await context.Interaction.ModifyOriginalResponseAsync(msg =>
                    {
                        msg.Content = "Not your turn!";
                        msg.Components = GenerateBoard();
                    });
                    return false;
                }

                if (_isGameOver || _board[index] != "-")
                {
                    await context.Interaction.ModifyOriginalResponseAsync(msg =>
                    {
                        msg.Content = "❌ Invalid move or game already over!";
                        msg.Components = GenerateBoard();
                    });
                    return false;
                }

                _board[index] = _currentPlayer == _playerX ? "❌" : "⭕";
                var winner = CheckWinner();

                if (winner != null)
                {
                    _isGameOver = true;
                    await UpdateBoard($"{winner.Mention} **wins!** 🎉");
                    return true;
                }

                if (_board.All(cell => cell != "-"))
                {
                    _isGameOver = true;
                    await UpdateBoard("**Draw!** 🤝");
                    return true;
                }

                _currentPlayer = _currentPlayer == _playerX ? _playerO : _playerX;
                await UpdateBoard($"**{_currentPlayer.Mention}'s turn** ({(_currentPlayer == _playerX ? "X" : "O")})");
                return false;
            }

            public async Task CancelGame()
            {
                _isGameOver = true;
                await _context.Interaction.ModifyOriginalResponseAsync(msg =>
                {
                    msg.Content = "❌ Game canceled by server owner";
                    msg.Components = new ComponentBuilder().Build();
                });
            }

            private async Task UpdateBoard(string message)
            {
                await _context.Interaction.ModifyOriginalResponseAsync(msg =>
                {
                    msg.Content = message;
                    msg.Components = GenerateBoard();
                });
            }

            private SocketUser? CheckWinner()
            {
                int[,] winPatterns =
                {
                    {0,1,2}, {3,4,5}, {6,7,8},
                    {0,3,6}, {1,4,7}, {2,5,8},
                    {0,4,8}, {2,4,6}
                };

                for (int i = 0; i < winPatterns.GetLength(0); i++)
                {
                    int a = winPatterns[i, 0];
                    int b = winPatterns[i, 1];
                    int c = winPatterns[i, 2];

                    if (_board[a] != "-" && _board[a] == _board[b] && _board[b] == _board[c])
                    {
                        return _board[a] switch
                        {
                            "❌" => _playerX,
                            "⭕" => _playerO,
                            _ => null
                        };
                    }
                }
                return null;
            }

            private MessageComponent GenerateBoard()
            {
                var builder = new ComponentBuilder();
                bool disableButtons = _isGameOver;
                for (int i = 0; i < 9; i++)
                {
                    builder.WithButton(
                        label: _board[i],
                        customId: $"ttt_{i}",
                        style: ButtonStyle.Secondary,
                        row: i / 3,
                        disabled: disableButtons
                    );
                }
                return builder.Build();
            }
        }
    }
}
