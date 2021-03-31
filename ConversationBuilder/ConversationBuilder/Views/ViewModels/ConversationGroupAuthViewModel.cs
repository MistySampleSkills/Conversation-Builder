using System;
using System.Collections.Generic;
using ConversationBuilder.DataModels;

namespace ConversationBuilder.ViewModels
{

	public class SkillAuthorizationViewModel
	{
		public SkillAuthorizationViewModel(SkillAuthorization skillAuthorization)
		{
			Name = skillAuthorization.Name;
			Description = skillAuthorization.Description;
			AccountId = skillAuthorization.AccountId;
			Key = skillAuthorization.Key;
		}


		public string Name { get; set; }

		public string Description { get; set; }

		public string AccountId { get; set; }

		public string Key { get; set; }
	}

	public class AuthViewModel
	{
		public string Handler { get; set; }
		public string ParentId { get; set; }
		public string ParentName { get; set; }

		public string Name { get; set; }

		public string Description { get; set; }

		public string AccountId { get; set; }

		public string Key { get; set; }

		public IDictionary<string, SkillAuthorizationViewModel> Data { get; set; } = new Dictionary<string, SkillAuthorizationViewModel>();

		public DateTimeOffset Expires { get; set; }
	}
}