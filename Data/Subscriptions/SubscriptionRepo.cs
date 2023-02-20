using bot.Models;
using Microsoft.EntityFrameworkCore;

namespace bot.Data.Subscriptions
{
	public class SubscriptionRepo : ISubscriptionRepo
	{
		private readonly SubscriptionsContext _context;

		public SubscriptionRepo(SubscriptionsContext context)
		{
			_context = context;
		}

		public void AddSubscription(Subscription sub)
		{
			if (sub == null)
				throw new ArgumentNullException(nameof(sub));
			_context.Subscriptions.Add(sub);
		}

		public void DeleteSubscription(long userId, string query)
		{
			var subscription = _context.Subscriptions.Where(x => x.userId == userId && x.query == query);
			_context.Subscriptions.RemoveRange(subscription);
		}

		public IQueryable<Subscription> GetAllSubscriptions()
		{
			return _context.Subscriptions.OrderByDescending(x => x.date);
		}

		public IQueryable<Subscription> GetUserSubscriptions(long userId)
		{
			return _context.Subscriptions.Where(x => x.userId == userId);
		}

		public bool SaveChanges()
		{
			return _context.SaveChanges() >= 0;
		}

		public bool SubscriptionsExists(long userId, string query)
		{
			return (_context.Subscriptions?.Any(x => x.userId == userId && x.query == query)).GetValueOrDefault();
		}
	}
}
