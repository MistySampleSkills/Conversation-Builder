using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ConversationBuilder.DataModels;
using Newtonsoft.Json;

namespace ConversationBuilder.ViewModels
{
	public class SpeechHandlerViewModel
	{
		/// <summary>
		/// Unique id for mapping
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// Data item type
		/// </summary>
		public string ItemType { get; set; } = DataItemType.SpeechHandler.ToString();

		[Required]
		[Display(Name = "Name")]
		[JsonProperty(PropertyName = "Name")]
		/// <summary>
		public string Name { get; set; }

		public string Description { get; set; }

		public IList<string> Utterances { get; set; }

		[Required]
		public string UtteranceString { get; set; }
		
		public bool ExactMatchesOnly { get; set; }
		public string ManagementAccess { get; set; } = "Shared";
		public DateTimeOffset Created { get; set; }
		
		public DateTimeOffset Updated { get; set; }
	}
}