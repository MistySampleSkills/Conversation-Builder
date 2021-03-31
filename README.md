# Conversation Builder

The conversation builder lets you create interesting conversations for Misty that utilize most of her sensors and inputs, without writing code.

Using the builder, you can set up conversations where the response to Misty can be speech, bumper pressing, object detection, QR codes, and a variety of other triggers.

You can also use user specific data and inline speech parameters to further expand on how Misty interacts with her environment.

Finally, if you do need more options, skill messages can be set up to call other optional skills to do anything they are programmed to do, and optionally respond as a valid trigger in the conversation.

------------

The Conversation Builder consists of two main parts.  The UI and the Misty Conversation skill.

The UI allows you to build and manage key parts of conversations, like what is said or displayed on the screen, how Misty moves, and what reactions and triggers is she looking for?   

The skill takes the data created in the UI and orchestrates the conversation, starting and stopping helper skills for more capabilities as needed.

It is a work in progress, but hopefully what is presented here will help you get your Misty talking, and make it easy to create conversations that appeal to you.

## Repository Structure
The repository is currently structured as follows:

### ConversationBuilder
Contains the code for the conversation builder NET Core UI.

### Robot
Contains the skills and libraries needed to run the conversation builder.

#### MistyConversation
The main conversation skill that is ran by a user.

#### MistyCharacter
Library used by Misty Conversation skill to do most of the interaction work.

#### MessageHandlers
Optional helper skills that are used by adding Skill Messages to conversations.
The skills in use here are based upon the extra functionality added to your conversation (if any).  In these examples, the skill uses the libraries in the ConversationLibraries folder for many of the requests.
There are some other useful/fun libraries in here that aren't in the the example skill at this time.
The misty conversation skill starts and stops these helper skills as specified in the interactions.

#### Managers
Optional override managers that can be used by different Characters to override the built in conversation functionality for arm movement, head movement, and other basic conversation actions.
This area is somewhat experimental at this time and may change.

#### Characters
Allows a developer to add more built in character to a conversation and access event and action data without a separate skill.
This area is somewhat experimental at this time and may change.

#### ConversationLibraries
A variety of optional libraries that do different things like check the weather, get jokes, etc.  Used by the example trigger skill in this repository.
Also includes the required Conversation.Common library which is shared across most of the conversation code.
This area will change based upon the helper skills in use and the extra functionality added to your conversation (if any).

## To run the UI locally on Windows.

You will need Azure Cosmos DB Emulator installed and running (https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator-release-notes).

You will also need to install the NET Core 3.1 SDK from Microsoft (https://dotnet.microsoft.com/download/dotnet/3.1).

Start Visual Studio Code (https://code.visualstudio.com/Download) and select to open a folder.  Select the top level ConversationBuilder folder.

On the first load, it may tell you that you need to restore references.  Allow it to do so.

There is a default email and password setup for the administration in Startup.cs, you should probably change it in the code before creating a database.  
The email in use now is not valid or monitored.  

If you do not want to change it right now, to login, use:
hello-misty@mistyrobotics.com
P@ssw0rd!  

If you want to set up forgot password, email verification and other emailing functionality, update the information in the appSettings files with an appropriately configured email account.

You should be able to select the Run/Debug button (the arrow that looks like a Play button) which should  take you to the debug section where you can hit another arrow to run it (arrow says .NET Core Launch next to it) and the system should start up locally and attach to the database.

It will start your default Visual Studio Code browser and drop you on a warning page.  
Continue on to the website and login.

## Running the conversation group after creation

Install the MistyConversation skill by building the skill in ARM mode and deploying onto Misty following the instructions on the website (https://docs.mistyrobotics.com/misty-ii/dotnet-sdk/overview/).

If you are going to perform the presentation demo, you should also install the ExampleHandlerSkill.

If the UI is running locally, go to the Conversation Group details page and enter your robot ip in the box and start the skill from this page.

If the UI is not running locally, you can start the skill by going to the Skill Runner (http://sdk.mistyrobotics.com/skill-runner/index.html) and choosing the gear near the skill name.  Enter in the ConversationGroupId and the Endpoint keys and their values as specified on the Conversation Group details page.  

Don't start the helper skills, the conversation skill will start them with the proper parameters.

**
See the included presentation for detailed use at this time.**


------------


## Building Blocks

### Animation
Animations contain the information for how Misty should present herself in an interaction. For example, what she displays on her screen, and what she says. You can also map user deﬁned head, arm and LED transition actions for the animation.

### Trigger
Triggers for Misty to look for during an interaction, such as hearing speech, sensing a bumper press or cap touch, receiving a message from another skill, seeing an object, and more.

### Interaction
A mapping of the animation, the expected triggers for that interaction, and the actions to take when a speciﬁc trigger in that interaction  happens.

### Conversation
A group of interactions is saved to a conversation where you also set the starting interaction and a few other conversation speciﬁc ﬁelds.

### Conversation Trigger
Triggers that are listened to during the entire conversation, not just within assigned interactions. They can be turned off as desired within each interaction.

### Character Configuration
Extra optional and required  ﬁelds to map for a conversation, eg: speech conﬁguration, character, starting volume. You can also pass in an optional user deﬁned json payload that will be sent to any helper skills you call through the skill message functionality.

### Speech Configuration
Used for current conversation speech capabilities to set the Azure or Google subscription and speech information.  Azure or Google Speech Recognition services required at this time for any speech recognition triggers.

### Speech Handler
Used for current free and beta onboard speech handler mappings that interprets text to intent. 

### Arm Action/Movement
Arm action for an animation.  Arm actions can consist of a single movement or continuous movement during the animation.

### Head Action/Movement
Head action for an animation.  Head actions can consist of a single movement or continuous movement during the animation, within a range or following a face or object.

### LED Transition
LED action to use during the animation.

### Skill Message
Can be created to send events to running helper skills during an interaction.  These skills can perform actions as needed and respond with trigger responses as part of the conversation.

### Inline Speech
Optionally used for creating more dynamic and personalized speech.  Uses built in speech ﬁelds and user lookup data to allow ﬂexible responses to users.

### User Lookup Data
Optional grouped key-value data pairs that can be referenced in helper skills and inline speech.

### ConversationGroup
Multiple conversations can be mapped to a conversation group, allowing interactions to move to  different conversations based upon the trigger action (beta).

## Other Odds and Ends Not In Presentation
### User Configuration Css Files
Experimental theme functionality can be used in the UI by selecting the gear icon at the top and then Edit.
Enter in the Override CSS file name
Current options are blue-theme.css, dark-theme.css, lite-theme.css, natural-theme.css and bright-theme.css.  You will probably want to make your own. Add them to ConversationBuilder/wwwroot/css folder to use them by name.


**********************************************************************
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
**********************************************************************