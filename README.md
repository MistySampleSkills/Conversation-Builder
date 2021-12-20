# Conversation Builder

The conversation builder lets you create interesting interactions for Misty that utilize most of her sensors and inputs, including conversational speech, without writing code.

Using the builder, you can set up conversations where the response to Misty can be speech, bumper pressing, object detection, QR codes, and a variety of other triggers from the robot or external systems.

You can also use user specific data and inline speech parameters to further expand on how Misty interacts with her environment.

Finally, if you do need more capabilities, there are multiple ways to add them. 

Skill messages can be set up to call other optional skills to do anything they are programmed to do, and optionally respond as a valid trigger in the conversation.
You can also extend the animation commands by adding capabilities to the command manager at skill start.

It is a work in progress, and is being refactored as new ideas are tried, but hopefully what is presented here will help you get your Misty talking, and make it easy to create conversations that appeal to you.

------------

## The Conversation Builder consists of three main parts.  The Admin UI, the Misty Conversation skill, and the Interaction UI.

The admin UI allows you to build and manage key parts of conversations, like what is said or displayed on the screen, how Misty moves, and what reactions and triggers she expects.

The skill takes the data created in the UI and orchestrates the conversation, starting and stopping helper skills for more capabilities if needed.

The Interaction UI dashboard allows a skill user to install conversations and authorization files, so the conversation can be run stand-alone, without access to the admin UI. The Interaction dashboard also displays state and interaction information, and allows basic interaction with the skill by presenting the trigger options as buttons as the conversation progresses.Finally, the interaction dashboard can also respond to animation commands sent from the robot, to make it an active participant of the interaction.

------------

## To run the UI locally on Windows.

You will need the Azure Cosmos DB Emulator installed and running (https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator-release-notes).

You will also need to install the NET Core 3.1 SDK from Microsoft (https://dotnet.microsoft.com/download/dotnet/3.1).

Install and start Visual Studio Code (https://code.visualstudio.com/Download) and select to open a folder.  Select the top level ConversationBuilder folder.

On the first load, it may tell you that you need to restore references.  Allow it to do so.

There is a default email and password setup for the administration in Startup.cs, you should probably change it in the code before creating a database.  
The email in use now is not valid or monitored and you can't do password recovery with it.  

If you do not want to change it right now, to login, use:
hello-misty@mistyrobotics.com
P@ssw0rd!  

If you want to set up forgot password, email verification and other emailing functionality, update the information in the appSettings files with an appropriately configured email account.

You should be able to select the Run/Debug button (the arrow that looks like a Play button) which should take you to the debug section where you can hit another arrow to run it (arrow says .NET Core Launch next to it) and the system should start up locally and attach to the database.

It will start your default Visual Studio Code browser and drop you on a warning page.  
Continue on to the website and log in.

If the animation, speech, character, trigger, and speech handler tables are empty, such as on the first run of the system, the system will populate seed common data and example animations into the database.

------------

## Running the conversation group after creation

Install the MistyConversation skill by building the skill in ARM mode and deploying onto Misty following the instructions on the website (https://docs.mistyrobotics.com/misty-ii/dotnet-sdk/overview/).

If you are going to call any skills from your conversation, make sure those skills are also installed.

### From the Admin UI

If the UI is running locally, go to the Conversation Group details page and enter your robot ip in the box and start the skill from this page.

There are many other options available as well. If you choose animation creation mode, it will halt the arms and/or head as selected, and will use the entered debounce to create an arm and head movement file at C$/Data/Misty/SDK/SDKData. Smooth animation recording will only record changes, and should be used in most cases.

If you have multiple Misty's and add an IP to the puppeting ip list (comma separated), it will mimic the movements on that Misty, using REST commands, while recording.

Choosing Retranslate will recreate any audio files for speech that it attempts to say during this run, instead of using previously created files.

### From the Skill Runner

If the UI is not running locally, you can start the skill by going to the Skill Runner (http://sdk.mistyrobotics.com/skill-runner/index.html) and choosing the gear near the skill name.  Enter in the ConversationGroupId and the Endpoint keys and their values as specified on the Conversation Group details page.  

Don't start the helper skills, the conversation skill will start them with the proper parameters.

### From the Interaction Dashboard

Enter the robot's IP, and select Connect. 

Select Conversations and start the skill.

Import any auth json files you have, if the conversation requires them. I have included example auth files that can be used for onboard speech-recognition and text-to-speech.

Import any extra conversations you would like.

Start the skill, after it loads, it should present the conversation options on the Conversations popup. Select the conversation to start it.

------------

## Quick start

Start by creating a new Conversation. Then make and attach at a minimum, a starting Interaction and "no trigger" Interaction (which can be the same one). Add animations, scripted actions, triggers and trigger handlers to each interaction as needed.

Create a new speech configuration in the Administrative section, or use one of the seeded options, using Vosk for ASR (requires recently released nuget and home robot updates), and Misty or Skill for TTS if you don't have Google or Azure authorization.

Create a character and attach your speech configuration to the character. The system will seed two characters for you upon deployment which you can also use.

Make a Conversation Group and attach this conversation and character. Once everything is saved and the skill is loaded on the robot, you should be able to start it from the Conversation Group after entering in your robot's IP.

------------

## Action Library

The action library consists of reusable animations and movements, that can be used in different conversations.

### Animations

Movements and other animations that are created using an animation script or a combination of existing arm, head, and led actions. 

### LED Transition Actions

Basic LED transitions that can be added to animations. May be deprecated in favor of animation LED commands.

### Arm Movements

Simple arm movements that can be added to animations. May be deprecated in favor of animation arm commands.

### Head Movements

Head movements that can be added to animations. May be deprecated in favor of animation head commands.

------------

## Trigger Creation

An interaction listens for triggers to know how to respond. There are built in triggers, like bumper press, cap touch, and object detection; external triggers, like REST calls into the robot; and speech triggers which are built by associating a speech handler with a speech trigger.

### Triggers

Triggers can be added to an interaction so that interaction listens for the event. The trigger is also associated with one or more interactions to be performed when it happends.

### Speech Handlers

Speech handlers are set up to allow phrases to be associated with a speech trigger.

------------

## Conversation Creation

### Conversation Group

A conversation group contains one or more conversations that can be linked together. 

### Conversation

The conversation consists of a variety of interactions linked through triggers.

### Speech Configurations

Different speech configurations to be used in character configurations. This can also contain subscription keys, which should probably not be populated for any exported conversation configuration file.  Users can upload their own auth file as needed through the interaction dashboard, or you can simply use `Vosk` for ASR and `Misty` or `Skill` for text-to-speech, to avoid needing subscriptions.

### Character Configurations

Contains options for displaying spoken words on screen, using prespeech, log level and other details. Also where you select a speech configuration. Character configurations are then used in conversation groups.

------------

## Advanced

### Skill Messages

Allows the addition of skills to the interaction. Interactions can trigger events in different skills, which can process as needed and return a trigger response.

### User Lookup Data

Allows the addition of speech customization based on certain state and entered data. Can also be used as an FAQ board when treating the input as speech.

### Add New User

For admin users, you can invite new users assuming you have setup the email app settings appropriately.

------------

## User Configuration

Basic user control of the theme and what data and options are shown. Some experimental starts to simpler cross robot communication and mapping recipes.

------------

## Text Replacement

Basic text replacement using common state data or user entered data.

## Simple Text Replacement

wrap replacement data with {{}} and use || for chaining responses. Will move to next item if the previous item does not exist.

"Hello! Secret agent {{robotname}} here. Good to see you {{face:level||here}} {{face||today}}. Good to see you this {{partofday}}} on {{day}}. What's the news?"

The above uses User Data with the name of the user label given to the face when it was trained, so, in most cases, the user name.
Then a data map is added under the name with the key of 'level'. In the Value field add whatever your level is... mine is Agent.

so, misty says to me...

"Hello! Secret agent Misty here. Good to see you Agent Brad. Good to see you this afternoon on Thursday. What's the news?"

## Intent Text Replacement

When creating the user data, select Treat Key as utterance and then when you add the data map Keys, treat them as comma delimited utterances. In the Value field, set what you want Misty to say if it hears that utterance. Then, when creating your interactions, create an Unknown Speech Heard trigger, and set it to go to an interaction with the speech set simply as:

{{news:text}}

You can add a key of 'no-match' as a catch all for no matches.

------------

# For more details, see the presentation doc for older examples and more detail; and the previous ReadMe and the Animation Commands files in this repo.

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


