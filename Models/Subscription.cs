using System.ComponentModel.DataAnnotations;

namespace bot.Models
{
	public class Subscription
	{
		[Key]
		[Required]
		public int Id { get; set; }
		[Required]
		public long userId { get; set; }
		[Required]
		public DateTime date { get; set; }
		[Required]
		public string query { get; set; }

	}
}