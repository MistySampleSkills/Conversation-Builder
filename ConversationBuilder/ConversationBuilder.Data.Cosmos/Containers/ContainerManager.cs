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

using Microsoft.Azure.Cosmos;

namespace ConversationBuilder.Data.Cosmos
{
	public class ContainerManager
	{
		private CosmosClient _dbClient;
		private Container _container;
		private string _databaseName;

		public IAnimationData AnimationData {get; private set;}

		public IConversationData ConversationData {get; private set;}

		public IConversationGroupData ConversationGroupData {get; private set;}
		
		public ITriggerDetailData TriggerDetailData {get; private set;}

		public ISpeechHandlerData SpeechHandlerData {get; private set;}
		public IInteractionData InteractionData {get; private set;}
		public ICharacterConfigurationData CharacterConfigurationData {get; private set;}
		public ISkillMessageData SkillMessageData {get; private set;}
		public IHeadLocationData HeadLocationData {get; private set;}
		public IArmLocationData ArmLocationData {get; private set;}
		public ISpeechConfigurationData SpeechConfigurationData {get; private set;}
		public ILEDTransitionActionData LEDTransitionActionData {get; private set;}
		public IGenericDataStoreData GenericDataStoreData {get; private set;}
		public IUserConfigurationData UserConfigurationData {get; private set;}
		
		public ContainerManager(CosmosClient dbClient, string databaseName, ContainerType containerType)
		{
			_container = dbClient.GetContainer(databaseName, containerType.ToString());	
			_dbClient = dbClient;
			_databaseName = databaseName;

			AnimationData = new AnimationData(_container);
			ConversationData = new ConversationData(_container);
			ConversationGroupData = new ConversationGroupData(_container);
			TriggerDetailData = new TriggerDetailData(_container);
			SpeechHandlerData = new SpeechHandlerData(_container);
			InteractionData = new InteractionData(_container);
			CharacterConfigurationData = new CharacterConfigurationData(_container);			
			SkillMessageData = new SkillMessageData(_container);
			HeadLocationData = new HeadLocationData(_container);
			ArmLocationData = new ArmLocationData(_container);
			SpeechConfigurationData = new SpeechConfigurationData(_container);
			LEDTransitionActionData = new LEDTransitionActionData(_container);
			GenericDataStoreData = new GenericDataStoreData(_container);
			UserConfigurationData = new UserConfigurationData(_container);
		}
	}
}