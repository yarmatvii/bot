using bot.Data.Subscriptions;
using bot.Models;
using bot.SyncDataServices.Http;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System.Linq.Expressions;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

public class BackgroundWorker : IHostedService, IDisposable
{
	private readonly ILogger<BackgroundWorker> _logger;
	private readonly IDataParserDataClient _dataParserDataClient;
	private readonly PeriodicTimer _timer;
	private readonly ITelegramBotClient _botClient;
	private readonly IUOW _UOW;
	private Task _timerTask;

	public BackgroundWorker(ILogger<BackgroundWorker> logger, IDataParserDataClient dataParserDataClient, ITelegramBotClient botClient, IUOW UOW)
	{
		_logger = logger;
		_dataParserDataClient = dataParserDataClient;
		_timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
		_botClient = botClient;
		_UOW = UOW;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Background worker starting.");

		_timerTask = DoWorkAsync();

		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Background worker stopping.");

		// Stop the periodic timer
		Dispose();

		return Task.CompletedTask;
	}

	public void Dispose()
	{
		_timer?.Dispose();
	}

	private async Task DoWorkAsync()
	{
		try
		{
			_logger.LogInformation("Running background task.");

			string? result;

			while (await _timer.WaitForNextTickAsync())
			{
				foreach (var s in _UOW.Subscriptions.GetAll())
				{
					result = "";

					if (!_UOW.Subscriptions.Exists(s.User.Id, s.query))
					{
						_logger.LogWarning($"Subscription {s.query} was deleted before ParseData");
						continue;
					}

					// Send Sync Message
					try
					{
						result = await _dataParserDataClient.ParseData(s.query);
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"--> Could not send synchronously: {ex.Message}");
						continue;
					}

					if (!_UOW.Subscriptions.Exists(s.User.Id, s.query))
					{
						_logger.LogWarning($"Subscription {s.query} was deleted before DeserializeObject");
						continue;
					}

					List<Post> posts;
					posts = JsonConvert.DeserializeObject<List<Post>>(result);

					if (posts is null)
					{
						_logger.LogWarning($"NULL for {s.query}");
						continue;
					}

					if (posts.Count == 0)
					{
						_logger.LogWarning($"No data found for {s.query}");
						continue;
					}

					if (!_UOW.Subscriptions.Exists(s.User.Id, s.query))
					{
						_logger.LogWarning($"Subscription {s.query} was deleted before DB.Create");
						continue;
					}

					foreach (Post post in posts)
					{
						if (post.Price is not null)    //REMOVE
						{
							if (!_UOW.Subscriptions.Exists(s.User.Id, s.query))
							{
								_logger.LogWarning($"Subscription {s.query} was deleted during foreach");
								continue;
							}

							post.Subscription = _UOW.Subscriptions.GetByUser(s.User.Id).Where(x => x.query == s.query).FirstOrDefault();
						}
					}

					var substructedPosts = posts.Except(_UOW.Posts.GetByUser(s.Id));

					foreach (var p in substructedPosts)
						_UOW.Posts.Create(p);

					if (!_UOW.Subscriptions.Exists(s.User.Id, s.query))
					{
						_logger.LogWarning($"Subscription {s.query} was deleted before DB.Save");
						continue;
					}

					try
					{
						_UOW.Save();
					}
					catch (Exception ex)
					{
						_logger.LogError($"BackgroundWorker_UOW.Save(); {ex.Message}");
					}

					foreach (var p in substructedPosts)
						await _botClient.SendPhotoAsync(
							chatId: s.User.Id,
							photo: p.Image,
							caption: $"{p.Title}</br>{p.Price}</br>{p.Date}</br> <i>Джерело</i>: <a href={p.Uri}>LINK</a>",
							parseMode: ParseMode.Html
							);
				}
			}
		}
		catch (Exception ex)
		{
			_logger.LogError("Error running background task: {0}", ex.Message);
		}
	}
}
