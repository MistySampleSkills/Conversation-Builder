using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ConversationBuilder.DataModels;
using Newtonsoft.Json;

namespace ConversationBuilder.ViewModels
{
	public class GenericDataViewModel
	{
		/// <summary>
		/// Unique id for mapping
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// Data item type
		/// </summary>
		public string ItemType { get; set; } = DataItemType.GenericDataStore.ToString();

		[Required]
		[Display(Name = "Name")]
		[JsonProperty(PropertyName = "Name")]
		/// <summary>
		public string Name { get; set; }

		public string Description { get; set; }
		
		//public IDictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
		public IDictionary<string, GenericData> Data { get; set; } = new Dictionary<string, GenericData>();

		public string DataId { get; set; }
		public string Key { get; set; }
		public string Value { get; set; }
		public string Image { get; set; }
		public string ScreenText { get; set; }
		public bool TreatKeyAsUtterance { get; set; }
		public bool ExactMatchesOnly { get; set; }

		public DateTimeOffset Created { get; set; }
		
		public DateTimeOffset Updated { get; set; }
	}
}