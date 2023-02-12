using bot.Data.Subscriptions;
using bot.Models;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot.Data.Bot
{
	public class UpdateHandlers
	{
		private readonly ITelegramBotClient _botClient;
		private readonly ILogger<UpdateHandlers> _logger;
		private readonly ISubscriptionRepo _repository;

		public UpdateHandlers(ITelegramBotClient botClient, ILogger<UpdateHandlers> logger, ISubscriptionRepo repo)
		{
			_botClient = botClient;
			_logger = logger;
			_repository = repo;
		}

		public Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
		{
			var ErrorMessage = exception switch
			{
				ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
				_ => exception.ToString()
			};

			_logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);
			return Task.CompletedTask;
		}

		public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
		{
			var handler = update switch
			{
				{ Message: { } message } => BotOnMessageReceived(message, cancellationToken),
				{ EditedMessage: { } message } => BotOnMessageReceived(message, cancellationToken),
				{ CallbackQuery: { } callbackQuery } => BotOnCallbackQueryReceived(callbackQuery, cancellationToken),
				_ => UnknownUpdateHandlerAsync(update, cancellationToken)
			};

			await handler;
		}

		private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
		{
			_logger.LogInformation("Receive message type: {MessageType}", message.Type);
			if (message.Text is not { } messageText)
				return;
			var action = messageText.Split(' ')[0] switch
			{
				"/menu" => SendMenuInlineKeyboard(_botClient, message, cancellationToken),
				_ => Usage(_botClient, message, cancellationToken)
			};
			Message sentMessage = await action;
			_logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);

			// Send inline keyboard
			// You can process responses in BotOnCallbackQueryReceived handler
			static async Task<Message> SendMenuInlineKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
			{
				await botClient.SendChatActionAsync(
					chatId: message.Chat.Id,
					chatAction: ChatAction.Typing,
					cancellationToken: cancellationToken);

				// Simulate longer running task
				//await Task.Delay(500, cancellationToken);

				InlineKeyboardMarkup inlineKeyboard = new(
					new[]
					{
						InlineKeyboardButton.WithCallbackData("New Template", "new"),
						InlineKeyboardButton.WithCallbackData("My Templates", "list")
					});

				return await botClient.SendTextMessageAsync(
						chatId: message.Chat.Id,
						text: "Menu:",
						replyMarkup: inlineKeyboard,
						cancellationToken: cancellationToken);
			}

			//static async Task<Message> SendReplyKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
			//{
			//	ReplyKeyboardMarkup replyKeyboardMarkup = new(
			//		new[]
			//		{
			//			new KeyboardButton[] { "/NewTemplate", "/MyTemplates" },
			//		})
			//	{
			//		ResizeKeyboard = true
			//	};

			//	return await botClient.SendTextMessageAsync(
			//		chatId: message.Chat.Id,
			//		text: "Menu:",
			//		replyMarkup: replyKeyboardMarkup,
			//		cancellationToken: cancellationToken);
			//}

			async Task<Message> Usage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
			{
				_repository.AddSubscription(new Subscription { userId = message.From.Id, date = DateTime.Now, query = message.Text });
				_repository.SaveChanges();

				const string usage = "Click to open menu:\n" +
									 "/menu";

				return await botClient.SendTextMessageAsync(
					chatId: message.Chat.Id,
					text: usage,
					replyMarkup: new ReplyKeyboardRemove(),
					cancellationToken: cancellationToken);
			}
		}

		// Process Inline Keyboard callback data
		private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
		{
			_logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);

			InlineKeyboardMarkup inlineKeyboard = new(
					new[]
					{
						InlineKeyboardButton.WithCallbackData("Delete", "delete")
					});

			ForceReplyMarkup forceReplyMarkup = new ForceReplyMarkup();
			forceReplyMarkup.InputFieldPlaceholder = "Write your template query:";

			switch (callbackQuery.Data)
			{
				case "new":
					await _botClient.SendTextMessageAsync(
							chatId: callbackQuery.Message!.Chat.Id,
							text: "Write your template query:",
							replyMarkup: forceReplyMarkup,
							cancellationToken: cancellationToken);
					//await _botClient.SendChatActionAsync(
					//	chatId: callbackQuery.Message!.Chat.Id,
					//	chatAction: ChatAction.Typing,
					//	cancellationToken: cancellationToken);
					break;
				case "list":
					foreach (var subscription in _repository.GetUserSubscriptions(callbackQuery.Message!.Chat.Id))
						await _botClient.SendTextMessageAsync(
							chatId: callbackQuery.Message!.Chat.Id,
							text: subscription.query,
							replyMarkup: inlineKeyboard,
							cancellationToken: cancellationToken);
					break;
				case "delete":
					if (_repository.SubscriptionsExists(callbackQuery.Message!.Chat.Id, callbackQuery.Message.Text))
					{
						_repository.DeleteSubscription(callbackQuery.Message!.Chat.Id, callbackQuery.Message.Text);
						_repository.SaveChanges();
						await _botClient.AnswerCallbackQueryAsync(
							callbackQueryId: callbackQuery.Id,
							text: "Successfully deleted",
							cancellationToken: cancellationToken);
					}
					else
					{
						await _botClient.AnswerCallbackQueryAsync(
							callbackQueryId: callbackQuery.Id,
							text: "Error: There is no such template",
							cancellationToken: cancellationToken);
					}
					break;
			}
		}

		private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
		{
			_logger.LogInformation("No such command: {UpdateType}", update.Type);
			return Task.CompletedTask;
		}
	}
}
