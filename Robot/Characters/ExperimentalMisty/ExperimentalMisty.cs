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

using System.Collections.Generic;
using Conversation.Common;
using MistyCharacter;
using MistyRobotics.SDK.Messengers;

namespace CharacterTemplates
{
    /// <summary>
    /// Example of character overloading the default head manager of the conversation character    
    /// Experimental, concepts may change or be deprecated
    /// </summary>
	public class ExperimentalMisty : BaseCharacter
	{
		public ExperimentalMisty(IRobotMessenger misty, CharacterParameters characterParameters, IDictionary<string, object> originalParameters)
			: base(misty, characterParameters, originalParameters, 
				new ManagerConfiguration
				(
                    //Using default speech manager to control recorded speech to text translations and speaking
                    null,
                    //Using default time manager, in English
                    null,
                    //Using default arm manager                    
                    null, 
                    // The head manager controls head actions including following faces and objects, starting experiment for better following
                    new NewHeadManager(misty, originalParameters, characterParameters),
                    //very beta and experimental Emotional system where Misty will traverse her emotional spectrum based upon animation emotions and other input
                    null,
                    //Allows users to plug in their own intent handling for the character
                    null
                 )
		) { }

		#region IDisposable Support

		private bool _isDisposed = false;

		private void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					base.Dispose();
				}

				_isDisposed = true;
			}
		}

		public new void Dispose()
		{
			Dispose(true);
		}

		#endregion
	}
}