using System.ComponentModel.DataAnnotations;

namespace bot.Models
{
	public class User
	{
		[Key]
		[Required]
		public long Id { get; set; }
		public List<Subscription> Subscriptions { get; set; } = new();
	}
}
