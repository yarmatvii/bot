using bot.Data.Subscriptions;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

public class BackgroundWorker : IHostedService, IDisposable
{
	private readonly ILogger<BackgroundWorker> _logger;
	private readonly HttpClient _httpClient;
	private readonly PeriodicTimer _timer;
	private readonly ITelegramBotClient _botClient;
	private readonly ISubscriptionRepo _repository;
	private Task _timerTask;
	private readonly List<long> _ids = new();

	public BackgroundWorker(ILogger<BackgroundWorker> logger, HttpClient httpClient, ITelegramBotClient botClient, ISubscriptionRepo repository)
	{
		_logger = logger;
		_httpClient = httpClient;
		_timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
		_botClient = botClient;
		_repository = repository;
		_ids.Add(547515846);
		_ids.Add(821200544);

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

			while (await _timer.WaitForNextTickAsync())
			{
				foreach (var id in _ids)
				{
					await _botClient.SendPhotoAsync(
						chatId: id,
						photo: "https://ireland.apollo.olxcdn.com/v1/files/j2kmfko25oov3-UA/image;s=200x200",
						caption: $"<b>USB Лампа для кемпінгу 30 ,60 , 180 ват {id}</b>. <i>Джерело</i>: <a href=\"https://www.olx.ua/d/uk/obyavlenie/usb-lampa-dlya-kempngu-30-60-180-vat-IDQJGtr.html\">Olx</a>",
						parseMode: ParseMode.Html
						//cancellationToken: _cts.Token
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
