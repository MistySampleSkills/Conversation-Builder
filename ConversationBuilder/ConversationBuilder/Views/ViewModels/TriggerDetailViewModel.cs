using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ConversationBuilder.DataModels;
using Newtonsoft.Json;

namespace ConversationBuilder.ViewModels
{
	public class TriggerDetailViewModel
	{
		/// <summary>
		/// Unique id for mapping
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// Data item type
		/// </summary>
		public string ItemType { get; set; } = DataItemType.TriggerDetail.ToString();

		[Required]
		[Display(Name = "Name")]
		[JsonProperty(PropertyName = "Name")]
		/// <summary>
		public string Name { get; set; }

		public string TriggerFilter { get; set; }
		public string UserDefinedTrigger { get; set; }
		public string Trigger { get; set; }
		
		public string StartingTrigger { get; set; }
		public string StartingTriggerFilter { get; set; }
		public string UserDefinedStartingTrigger { get; set; }
		public int StartingTriggerDelay { get; set; }

		public string StoppingTrigger { get; set; }
		public string StoppingTriggerFilter { get; set; }
		public string UserDefinedStoppingTrigger { get; set; }
		public int StoppingTriggerDelay { get; set; }
		public string ManagementAccess { get; set; } = "Shared";
		public DateTimeOffset Created { get; set; }
		
		public DateTimeOffset Updated { get; set; }
	}
}