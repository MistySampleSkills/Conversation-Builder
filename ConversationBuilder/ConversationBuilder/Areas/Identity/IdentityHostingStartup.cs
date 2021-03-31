using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(ConversationBuilder.Areas.Identity.IdentityHostingStartup))]
namespace ConversationBuilder.Areas.Identity
{
	public class IdentityHostingStartup : IHostingStartup
	{
		public void Configure(IWebHostBuilder builder)
		{
			builder.ConfigureServices((context, services) =>
			{
			});
		}
	}
}