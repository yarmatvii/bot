using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using bot.Models;
using System.Diagnostics.Metrics;

public class SubscriptionsContext : DbContext
{
	public SubscriptionsContext(DbContextOptions<SubscriptionsContext> options)
		: base(options)
	{
		//Database.EnsureDeleted();
		//if (!Subscriptions.Any())
		//{
		//	Subscription s1 = new Subscription { userId = 547515846, query = "test", date = DateTime.Now };
		//	Subscriptions.AddRange(s1);

		//	SaveChanges();
		//}
	}

	public DbSet<bot.Models.Subscription> Subscriptions { get; set; } = default!;
}
