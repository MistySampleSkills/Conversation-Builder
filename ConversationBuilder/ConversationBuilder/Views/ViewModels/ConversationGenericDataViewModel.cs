using System.Collections.Generic;
using ConversationBuilder.DataModels;

namespace ConversationBuilder.ViewModels
{
	public class ConversationGenericDataViewModel
	{
		public IList<GenericDataStore> GenericDataStores { get; set; }
		public string Handler { get; set; }
		public string ConversationId { get; set; }
		public string ConversationName { get; set; }
	}
}