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

		[Display(Name = "User Defined Filter")]
		public string UserDefinedTriggerFilter { get; set; }
		public string Trigger { get; set; }
		
		public string StartingTrigger { get; set; }
		public string StartingTriggerFilter { get; set; }
		
		[Display(Name = "User Defined Staring Filter")]
		public string UserDefinedStartingTriggerFilter { get; set; }
		public int StartingTriggerDelay { get; set; }

		public string StoppingTrigger { get; set; }
		public string StoppingTriggerFilter { get; set; }
		
		[Display(Name = "User Defined Stopping Filter")]
		public string UserDefinedStoppingTriggerFilter { get; set; }
		public int StoppingTriggerDelay { get; set; }
		public string ManagementAccess { get; set; } = "Shared";
		public DateTimeOffset Created { get; set; }
		
		public DateTimeOffset Updated { get; set; }
	}
}