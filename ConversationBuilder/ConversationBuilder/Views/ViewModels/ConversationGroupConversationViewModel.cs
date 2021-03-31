using System.Collections.Generic;
using ConversationBuilder.DataModels;

namespace ConversationBuilder.ViewModels
{
	public class ConversationGroupConversationViewModel

	{
		public IList<Conversation> Conversations { get; set; }
		public string Handler { get; set; }
		public string ConversationGroupId { get; set; }
		public string ConversationGroupName { get; set; }
	}
}