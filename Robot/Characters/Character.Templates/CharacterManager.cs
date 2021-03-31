/**********************************************************************
	Copyright 2021 Misty Robotics
	Licensed under the Apache License, Version 2.0 (the "License");
	you may not use this file except in compliance with the License.
	You may obtain a copy of the License at
		http://www.apache.org/licenses/LICENSE-2.0
	Unless required by applicable law or agreed to in writing, software
	distributed under the License is distributed on an "AS IS" BASIS,
	WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	See the License for the specific language governing permissions and
	limitations under the License.
	**WARRANTY DISCLAIMER.**
	* General. TO THE MAXIMUM EXTENT PERMITTED BY APPLICABLE LAW, MISTY
	ROBOTICS PROVIDES THIS SAMPLE SOFTWARE "AS-IS" AND DISCLAIMS ALL
	WARRANTIES AND CONDITIONS, WHETHER EXPRESS, IMPLIED, OR STATUTORY,
	INCLUDING THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
	PURPOSE, TITLE, QUIET ENJOYMENT, ACCURACY, AND NON-INFRINGEMENT OF
	THIRD-PARTY RIGHTS. MISTY ROBOTICS DOES NOT GUARANTEE ANY SPECIFIC
	RESULTS FROM THE USE OF THIS SAMPLE SOFTWARE. MISTY ROBOTICS MAKES NO
	WARRANTY THAT THIS SAMPLE SOFTWARE WILL BE UNINTERRUPTED, FREE OF VIRUSES
	OR OTHER HARMFUL CODE, TIMELY, SECURE, OR ERROR-FREE.
	* Use at Your Own Risk. YOU USE THIS SAMPLE SOFTWARE AND THE PRODUCT AT
	YOUR OWN DISCRETION AND RISK. YOU WILL BE SOLELY RESPONSIBLE FOR (AND MISTY
	ROBOTICS DISCLAIMS) ANY AND ALL LOSS, LIABILITY, OR DAMAGES, INCLUDING TO
	ANY HOME, PERSONAL ITEMS, PRODUCT, OTHER PERIPHERALS CONNECTED TO THE PRODUCT,
	COMPUTER, AND MOBILE DEVICE, RESULTING FROM YOUR USE OF THIS SAMPLE SOFTWARE
	OR PRODUCT.
	Please refer to the Misty Robotics End User License Agreement for further
	information and full details:
		https://www.mistyrobotics.com/legal/end-user-license-agreement/
**********************************************************************/

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
    /// Manages selection of the conversation character templates
    /// TODO Update so characters are pulled in separately and don't need to be compiled in
    /// Experimental, concepts may change or be deprecated
    /// </summary>
    public sealed class CharacterManagerLoader : IDisposable
	{
		private const string Experimental = "experimental";
		private const string Event = "event";
		private const string Basic = "basic";

		private static IRobotMessenger _misty;
		private static CharacterManagerLoader _characterManagerLoader;
		private static ParameterManager _parameterManager;
		private static ManagerConfiguration _managerConfiguration = new ManagerConfiguration();

		public static IBaseCharacter Character { get; private set; }		
		public static CharacterParameters CharacterParameters { get; private set; } = new CharacterParameters();
		
		public static async Task<CharacterManagerLoader> InitializeCharacter(IDictionary<string, object> parameters, IRobotMessenger misty)
		{
			_misty = misty;			
			_parameterManager = new ParameterManager(misty, parameters);
			if (_parameterManager == null)
			{
				_misty.DisplayText($"Failed initialization.", "Text", null);
				_misty.SkillLogger.Log($"Failed misty conversation skill initialization.  Cancelling skill.");
				_misty.SkillCompleted();
				return null;
			}

			_misty.DisplayText($"Loading conversation.", "Text", null);
			CharacterParameters = await _parameterManager.Initialize();			
			if (CharacterParameters != null && string.IsNullOrWhiteSpace(CharacterParameters.InitializationError))
			{
				switch (CharacterParameters.Character?.ToLower())
				{
					case Experimental:					
						Character = new ExperimentalMisty(_misty, CharacterParameters, parameters);
						break;
					case Event:
						Character = new EventTemplate(_misty, CharacterParameters, parameters);
						break;
					case Basic:
					default:
						Character = new BasicMisty(_misty, CharacterParameters, parameters);
						break;
				}

				await Character.Initialize();
				_characterManagerLoader = new CharacterManagerLoader(CharacterParameters, misty, _managerConfiguration);
				return _characterManagerLoader;
			}

			_misty.DisplayText(CharacterParameters.InitializationError ?? "Failed initialization.", "Text", null);
			_misty.SkillLogger.Log(CharacterParameters.InitializationError);
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