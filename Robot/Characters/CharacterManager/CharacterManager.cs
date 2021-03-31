using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CharacterTemplates;
using Conversation.Common;
using MistyCharacter;
using MistyRobotics.SDK.Messengers;

namespace MistyConversation
{
	/// <summary>
	/// TODO This stinks to have to update to add characters
	/// </summary>
	public class CharacterManagerLoader : IDisposable
	{
		public const string Experimental = "experimental";
		public const string Basic = "basic";

		private static IRobotMessenger _misty;
		private static CharacterManagerLoader _characterManager;
		private static ParameterManager _parameterManager;
		private static ManagerConfiguration _managerConfiguration = new ManagerConfiguration();

		public static IBaseCharacter Character { get; protected set; }		
		public static CharacterParameters CharacterParameters { get; protected set; } = new CharacterParameters();
		
		public static async Task<CharacterManagerLoader> InitializeCharacter(IDictionary<string, object> parameters, IRobotMessenger misty)
		{
			_misty = misty;			
			_parameterManager = new ParameterManager(misty, parameters);
			if (_parameterManager == null)
			{
				_misty.DisplayText($"Failed initialization.", "Errors", null);
				_misty.SkillLogger.Log($"Failed misty conversation skill initialization.  Cancelling skill.");
				_misty.SkillCompleted();
				return null;
			}
			await _parameterManager.Initialize();
			CharacterParameters = _parameterManager.CharacterParameters;
			if (CharacterParameters != null)
			{
				switch (CharacterParameters.Character?.ToLower())
				{
					case Experimental:					
						Character = new ExperimentalMisty(_misty, CharacterParameters, parameters);
						await Character.Initialize();
						break;
					case Basic:
					default:
						Character = new EventTemplate(_misty, CharacterParameters, parameters);
						await Character.Initialize();
						break;
				}

				_characterManager = new CharacterManagerLoader(CharacterParameters, misty, _managerConfiguration);
				return _characterManager;
			}
			
			_misty.DisplayText($"Failed initialization.", "Errors", null);
			_misty.SkillLogger.Log($"Failed misty conversation skill initialization.  Cancelling skill.");
			_misty.SkillCompleted();
			return null;
		}

		public CharacterManagerLoader(CharacterParameters characterParameters, IRobotMessenger misty, ManagerConfiguration managerConfiguration = null)
		{
			CharacterParameters = characterParameters;
			_misty = misty;
			_managerConfiguration = managerConfiguration;
		}
		
		public async Task<bool> StartConversation()
		{
			return await Character.StartConversation();
		}
		
		#region IDisposable Support

		private bool _isDisposed = false;

		private void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					Character?.Dispose();
				}

				_isDisposed = true;
			}
		}
		public void Dispose()
		{
			Dispose(true);
		}

		#endregion
	}
}