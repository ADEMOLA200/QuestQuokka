using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace QuestQuokka.Services
{
    public class GameCommandsModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly GameManagementService _gameManager;
        private readonly TriviaService _triviaService;

        public GameCommandsModule(GameManagementService gameManager, TriviaService triviaService)
        {
            _gameManager = gameManager;
            _triviaService = triviaService;
        }

        [SlashCommand("canclegame", "Cancel the active game in this channel (Server Owner Only)")]
        public async Task CancelGameCommand()
        {
            await _gameManager.CancelGameAsync(Context);
        }

        [SlashCommand("givedailyreward", "Give a daily reward of a specified amount to a member")]
        public async Task GiveDailyRewardCommand(SocketUser member, int amount)
        {
            await _gameManager.GiveRewardAsync(member, amount, Context);
        }

        [SlashCommand("deductcoins", "Deduct a specified amount of coins from a member")]
        public async Task DeductCoinsCommand(SocketUser member, int amount)
        {
            await _gameManager.DeductCoinsAsync(member, amount, Context);
        }

        [SlashCommand("trivia", "Start a trivia game")]
        public async Task TriviaCommand()
        {
            await _triviaService.TriviaCommand(Context.Interaction);
        }

        [ComponentInteraction("trivia_answer_*")]
        public async Task HandleTriviaAnswer(string id)
        {
            await _triviaService.HandleTriviaAnswer(Context.Interaction, id);
        }
    }
}
