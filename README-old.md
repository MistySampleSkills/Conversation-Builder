# Conversation Builder

The conversation builder lets you create interesting conversations for Misty that utilize most of her sensors and inputs, without writing code.

Using the builder, you can set up conversations where the response to Misty can be speech, bumper pressing, object detection, QR codes, and a variety of other triggers.

You can also use user specific data and inline speech parameters to further expand on how Misty interacts with her environment.

Finally, if you do need more options, skill messages can be set up to call other optional skills to do anything they are programmed to do, and optionally respond as a valid trigger in the conversation.

------------

The Conversation Builder consists of two main parts.  The UI and the Misty Conversation skill.

The UI allows you to build and manage key parts of conversations, like what is said or displayed on the screen, how Misty moves, and what reactions and triggers she expects.

The skill takes the data created in the UI and orchestrates the conversation, starting and stopping helper skills for more capabilities if needed.

It is a work in progress, but hopefully what is presented here will help you get your Misty talking, and make it easy to create conversations that appeal to you.

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
Continue on to the website and login.

## Running the conversation group after creation

Install the MistyConversation skill by building the skill in ARM mode and deploying onto Misty following the instructions on the website (https://docs.mistyrobotics.com/misty-ii/dotnet-sdk/overview/).

If you are going to perform the presentation demo, you should also install the ExampleHandlerSkill.

If the UI is running locally, go to the Conversation Group details page and enter your robot ip in the box and start the skill from this page.

If the UI is not running locally, you can start the skill by going to the Skill Runner (http://sdk.mistyrobotics.com/skill-runner/index.html) and choosing the gear near the skill name.  Enter in the ConversationGroupId and the Endpoint keys and their values as specified on the Conversation Group details page.  

Don't start the helper skills, the conversation skill will start them with the proper parameters.

**
See the included (but somewhat dated) presentation for detailed use at this time.**

# Release Notes 7.14.21

#### Beta retrigger functionality added
If an interaction is marked as a retrigger interaction while selecting it in the interaction option list, the previous trigger that caused it will be rechecked immediately to see if there is a match. This can be used to setup interactions that can auto check the detail in the next interactions

So if interaction is to ask for Directions and then interaction two is where, if they say "Directions to the bathroom", it will auto select bathroom on the second trigger.

#### Prespeech added to character and interactions
Allows Misty to say "thinking" phrases while getting speech data to avoid long pauses.  "Let me see..", "Okay." etc

#### Animation Script use
Many beta animation script commands that can be added in different locations throughout interaction. Probably will end up replacing separate movement and led animations.

#### More distinct animation points added - prespeech and data user data matches 
More places where you can choose a new set of animations to perform within one interaction.

#### Bug fixed where speech intents caused hung interactions

#### Added Sync and TOFRange Triggers
Filter format and options
<SensorName> <Equality> <value> <timesTrue> <checkDurationMs>
Sensor Name options: FrontRange (all front range sensors), BackRange (all back range sensors), FrontRight, FrontCenter, FrontLeft, BackLeft, BackRight
TOF events come about every 100 ms, using that you can look for range triggers over time

eg: FrontRange <= 0.1 5 1000   //if the tofs are found to be closer than 0.1 meter, 5 times in 1 second, send out tof range trigger - currently expects error code 0
    FrontRange == X 3 750     //if the tofs are found to be OUT of status 0, 3 times in 750 ms, send out tof range trigger


#### Added speech handler match rule options and better matching
Starts With, Contains, Ends With, Exact and Plurals

#### Added ability to only show your items in UI lists, not all public items

#### More startup details and warnings spkoen to user and displayed on screen

#### Added robot list under users and started integration of auth codes for cross robot communication
Beta and directions on their way

#### Added simple cross robot communication and syncing functionality
Beta and directions on their way

#### Started integration of recipes and waypoints (incomplete)
Under development


### Beta Animation Script Commands

Animation Script Commands in CB

NOTE!  Parameters are currently comma delimited, that means for now if you want misty to speak for a multi parameter command (SPEAK-AND-SYNC, etc), don't use commas in the text.

Semi-colon delimited  (;)

ARMS:leftDegrees,rightDegrees,timeMs;

ARMS-V:leftDegrees,rightDegrees,velocity;

ARMS-OFFSET:leftDegrees,rightDegrees,timeMs; //offset commands are based off current actuator values

ARMS-OFFSET-V:leftDegrees,rightDegrees,velocity;

ARM:left/right,degrees,timeMs;

ARM-V:left/right,degrees,velocity;

ARM-OFFSET:left/right,degrees,timeMs;

ARM-OFFSET-V:left/right,degrees,velocity;

HEAD:pitch,roll,yaw,timeMs;  //use null to not change a degree

HEAD-OFFSET:pitch,roll,yaw,timeMs; //use 0 to not change a degree

HEAD-V:pitch,roll,yaw,velocity; //use null to not change a degree

HEAD-OFFSET-V:pitch,roll,yaw,velocity; //use 0 to not change a degree

PAUSE:timeMs;

VOLUME:newDefaultVolume;

DEBUG: User websocket message to send if skill is debug level;

PUBLISH: User websocket message to send;

LIGHT:true/false/on/off;

PICTURE:image-name-to-save-to,display-on-screen[,width,height]; //optional width and height resize

SERIAL:write to the serial stream;

STOP;

RESET-LAYERS;  //clear user defined web, video, text and image layers

RESET-EYES; //reset eyes and blinking to system defaults

HALT;

IMAGE:imageNameToDisplay.jpg;  //displays on default eye layer

IMAGE-URL:http://URL-to-display.jpg;  //displays on default eye layer

TEXT:text to display on the screen;

CLEAR-TEXT;

SPEAK:What to say;  //can use generic data and inline speech, like 'Speak' in animations

AUDIO:audio-file-name.wav;

VIDEO:videoName.mp4;

VIDEO-URL:http://videoName-to-play.mp4;

CLEAR-VIDEO;

WEB:http://site-name;

CLEAR-WEB;

LED:red,green,blue;

LED-PATTERN:red1,green1,blue1,red2,green2,blue2,durationMs,blink/breathe/transit;

START-LISTEN;  //starts trying to capture speech

SPEAK-AND-LISTEN;  

AllOW-KEYPHRASE; //"Allows" keyphrase to work, but won't start if Misty is speaking or already listening and will wait until she can to allow keyphrase for the interaction

CANCEL-KEYPHRASE; //turn off keyphrase rec

SPEAK-AND-WAIT:What to say, timeoutMs;

SPEAK-AND-SYNC:What to say,SyncName;

SPEAK-AND-EVENT:What to say,trigger,triggerFilter,text;

SPEAK-AND-LISTEN:What to say; //starts listening after speaking the text

FOLLOW-FACE;

FOLLOW-OBJECT:objectName;

STOP-FOLLOW;

DRIVE:distanceMeters,timeMs,true/false(reverse);

HEADING:heading,distanceMeters,timeMs,true/false(reverse);

TURN:degrees,timeMs,right/left;

ARC:heading,radius,timeMs,true/false(reverse);

TURN-HEADING:heading,timeMs,right/left;

RESPONSIVE-STATE:true/on/false/off;  //if true, this interaction will respond to external bot events and commands, defaults to on

HAZARDS-OFF;

HAZARDS-ON;

START-SKILL: skillId;

STOP-SKILL: skillId;

AWAIT-ANY:timeoutMs/-1;  // await any sync event

AWAIT-SYNC:syncName,timeoutMs/-1; // await a specific sync event

SYNC:syncName; //send a sync event

EVENT:trigger,triggerFilter,text;// send an event


Any commands starting with # are ignored during 'normal' animation and treated as "clean up" commands to run when animation is complete 

since it is possible the interaction may move on and choose a new animation before this one actually completes all of it's actions.

eg:

#RESET-EYES;

#RESET-LAYERS;


Any commands starting with * are guaranteed to run only one time at the start

eg:

*HAZARDS-OFF;

*IMAGE:e_Terror.jpg;

other decorators

{x} if the animation flag is set to loop, will only run these commands in the loop x many times (unless a * command)

$ send command to other bots in shared group, but not self.  Do not await an ack from the rbot

$% send command to other bots in shared group, including self.  Do not await an ack from the robot before continuing

$$ send command to other bots in shared group, but not self.  Await an ack from the robot before continuing

$$% send command to other bots in shared group, including self.  Await an ack from the robot before continuing

[robotName1,roboName2] only send to group bots in ip list, otherwise sends to all when using $ notation

eg:

$$%[Zoinks,Jinkies]HAZARDS-ON;

In the animation there is a RepeatScript flag you can check to have the animation repeat until the interaction is over.

------------

# Initial Release Notes 4.30.21

## Repository Structure
This repository is currently structured as follows:

### ConversationBuilder
Contains the code for the conversation builder NET Core UI.

### Robot
Contains the skills and libraries needed to run the conversations.

#### MistyConversation
The main conversation skill that is run by a user.

#### MistyCharacter
Library used by Misty Conversation skill to do most of the interaction work.

#### MessageHandlers
Optional helper skills that are used in our examples by adding Skill Messages to conversations.
The skills in use here are based upon the extra functionality added to our conversations.
The misty conversation skill starts and stops these helper skills as specified in the interactions.

#### Managers
Example of a manager overriding library.        
This area is somewhat experimental at this time and may change.

#### Characters
Allows a developer to add more built in character to a conversation and access event and action data without a separate skill.
This area is somewhat experimental at this time and may change.

#### ConversationLibraries
A variety of optional libraries that do different things like check the weather, get jokes, etc.  Used by the example trigger skill in this repository.
Also includes the required Conversation.Common library which is shared across most of the conversation code.
This area will change based upon the helper skills in use and the extra functionality added to your conversation (if any).

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