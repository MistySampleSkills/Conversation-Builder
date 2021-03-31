using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ConversationBuilder.DataModels;
using Newtonsoft.Json;

namespace ConversationBuilder.ViewModels
{
	public class ConversationViewModel
	{
		/// <summary>
		/// Unique id for mapping
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// Data item type
		/// </summary>
		public string ItemType { get; set; } = DataItemType.Conversation.ToString();

		[Required]
		[Display(Name = "Name")]
		public string Name { get; set; }
		public string Description { get; set; }
		
		public string StartupInteraction { get; set; }
		
		public string NoTriggerInteraction { get; set; }

		public string StartingEmotion { get; set; } = DefaultEmotions.Joy;

		//IList<Interaction>
		public IList<string> Interactions { get; set; } = new List<string>();

		//IList<Animation>
		public IList<string> Animations { get; set; } = new List<string>();

		public IList<string> Triggers { get; set; } = new List<string>();

		//IList<SpeechHandler>
		public IList<string> SpeechHandlers = new List<string>();

		public bool InitiateSkillsAtConversationStart { get; set; }

		public IDictionary<TriggerDetail, IList<TriggerActionOption>> ConversationTriggerMap { get; set; } = new Dictionary<TriggerDetail, IList<TriggerActionOption>>();

		public string Handler { get; set; }
		public IList<SkillMessage> SkillMessages { get; set; } = new List<SkillMessage>();

		public int Weight { get; set; } = 1;
		public string GoToConversation { get; set; }
		public string GoToInteraction { get; set; }
		public bool InterruptCurrentAction { get; set; } = true;
		public IList<TriggerDetailViewModel> TriggerDetails { get; set; } = new List<TriggerDetailViewModel>();
		public string SelectedTrigger { get; set; }
		public string RemovedTriggerAction { get; set; }
		public string Animation { get; set; }
		public DateTimeOffset Created { get; set; }
		
		public DateTimeOffset Updated { get; set; }

	}
}