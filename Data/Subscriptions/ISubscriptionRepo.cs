using bot.Models;
using Microsoft.EntityFrameworkCore;

namespace bot.Data.Subscriptions
{
	public interface ISubscriptionRepo
	{
		bool SaveChanges();
		IEnumerable<Subscription> GetAllSubscriptions();
		IEnumerable<Subscription> GetUserSubscriptions(long userId);
		void AddSubscription(Subscription sub);
		void DeleteSubscription(long userId, string query);
		bool SubscriptionsExists(long userId, string query);
	}
}
