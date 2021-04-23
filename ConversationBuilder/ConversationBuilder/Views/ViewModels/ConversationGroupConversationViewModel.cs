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

		//TriggerActionOption Id
		public DepartureMap DepartureMap { get; set; }
		
		//Interaction(ViewModel) Id
		public EntryMap EntryMap { get; set; }

		//public IDictionary<string, string> ConversationMappings { get; set; } = new Dictionary<string, string>();
	}
}