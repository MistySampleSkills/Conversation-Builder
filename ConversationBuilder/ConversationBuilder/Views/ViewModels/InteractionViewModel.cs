using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ConversationBuilder.DataModels;
using Newtonsoft.Json;

namespace ConversationBuilder.ViewModels
{
	public class InteractionViewModel
	{
		/// <summary>
		/// Unique id for mapping
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// Data item type
		/// </summary>
		public string ItemType { get; set; } = DataItemType.Interaction.ToString();

		public string Name { get; set; }

		public string Animation { get; set; }
		public string PreSpeechAnimation { get; set; }

		public Animation AnimationData { get; set; }
		
		public Animation PreSpeechAnimationData { get; set; }
		
		public double InteractionFailedTimeout { get; set; } = 120; //2 minutes with no trigger response

		public DateTimeOffset Created { get; set; }
		
		public DateTimeOffset Updated { get; set; }
		
		public IDictionary<TriggerDetail, IList<TriggerActionOption>> TriggerMap { get; set; } = new Dictionary<TriggerDetail, IList<TriggerActionOption>>();
		public IList<TriggerDetailViewModel> TriggerDetails { get; set; } = new List<TriggerDetailViewModel>();

		public IList<SkillMessage> SkillMessages { get; set; } = new List<SkillMessage>();
		
		public string Handler { get; set; }

		public string ConversationId { get; set; }

		public int Weight { get; set; } = 1;
		public bool Retrigger { get; set; }
		public string GoToConversation { get; set; }
		public string GoToInteraction { get; set; }
		public bool InterruptCurrentAction { get; set; } = true;
		
		public string SelectedTrigger { get; set; }
		public string RemovedTriggerAction { get; set; }

		public bool StartListening { get; set; } = false;

		public bool AllowKeyPhraseRecognition { get; set; }

		public bool AllowConversationTriggers { get; set; } = true;

		public bool AllowVoiceProcessingOverride { get; set; } = true;

		public double ListenTimeout { get; set; } = 6;

		public double SilenceTimeout { get; set; } = 6;

		[Display(Name = "Conversation Entry Point")]
		public bool ConversationEntryPoint { get; set; }
		public string ConversationName { get; set; }

		[Display(Name = "Use prespeech for speech intent processing")]
		public bool UsePreSpeech { get; set; } = true;
		

		[Display(Name = "Override PreSpeech Phrases")]
		public string PreSpeechPhrases { get; set; }

	}
}