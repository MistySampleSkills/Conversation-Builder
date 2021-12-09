# Animation Commands

Parameters are comma delimited, that means for now if you want misty to speak for a multi parameter command (SPEAK-AND-WAIT, etc), don't use commas in the text or make sure you wrap the speech with "quotes".

All commands are semi-colon delimited  (;)

## Commands

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

VOLUME-OFFSET:volumeOffset;

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

CLEAR-IMAGE;

TEXT:text to display on the screen[,size][,weight][,height][,r][,g][,b][,wrap][,font];

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

STOP-FOLLOW; //just person or face, not audio

FOLLOW-VOICE;

FOLLOW-NOISE;

STOP-AUDIO-FOLLOW;

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

END-SKILL; //ends this skill

AWAIT-ANY:timeoutMs/-1;  // await any sync event

AWAIT-SYNC:syncName,timeoutMs/-1; // await a specific sync event

SYNC:syncName; //send a sync event

EVENT:trigger,triggerFilter,text;// send an event

RECORD:filename; //start recording

STOP-RECORDING; //start recording

TIMED-TRIGGER:timeMs,trigger,triggerFilter,text; //sends the trigger as per the time

SET-MAX-SILENCE:timeInMs;  //reset the max speech silence value in ms

SET-MAX-LISTEN:timeInMs;  //reset the max listen value in ms

SET-SPEECH-PITCH:speechPitch;  //where 1.0 is default, does not work for all speech configurations

SET-VOICE:voiceToUse;  //does not work for all speech configurations - skill options are Mark, David and Zira.

SET-SPEECH-STYLE:speechStyle;  //does not work for all speech configurations

SET-LANGUAGE:newLanguage;  //does not work for all speech configurations

SET-SPEECH-RATE:speechRate;  //where 1.0 is default, does not work for all speech configurations

ANIMATE: animation name or id; //attempts to run a different animation in this script, very experimental and may mess with interaction flow. use at own risk ;)

## UI Commands
These commands will send events to the Interaction dashboard so you can include it as part of the interaction.

UI-TEXT:text;

UI-IMAGE:url of image to display;

UI-WEB:url iframe to display;

UI-AUDIO:url of audio to play;

UI-SPEECH:text of speech to say;

UI-LED:red,green,blue; //color to set the border box


## Animation Triggering

TRIGGER:trigger,triggerFilter,text; //manually invoke a trigger from the script

GOTO-ACTION:interactionName or id; //manually go to an interaction from an animation script

T-ACTION:Name,OverrideAction,Trigger, TriggerFilter;

T-ON:Name,Trigger,TriggerFilter;

T-ACTION!:Name,OverrideAction,Trigger, TriggerFilter;

T-ON!:Name,Trigger,TriggerFilter;

T-OFF:name;

Any commands starting with # are ignored during 'normal' animation and treated as "clean up" commands to run when animation is complete 

since it is possible the interaction may move on and choose a new animation before this one actually completes all of it's actions.

eg:

#RESET-EYES;

#RESET-LAYERS;

Any commands starting with * are guaranteed to run only one time at the start of the interaction

eg:

*HAZARDS-OFF;

*IMAGE:e_Terror.jpg;

other experimental decorators

{x} if the animation flag is set to loop, will only run these commands in the loop x many times (unless a * command)

$ send command to other bots in shared group, but not self.  Do not await an ack from the rbot

$% send command to other bots in shared group, including self.  Do not await an ack from the robot before continuing

$$ send command to other bots in shared group, but not self.  Await an ack from the robot before continuing

$$% send command to other bots in shared group, including self.  Await an ack from the robot before continuing

[robotName1,roboName2] only send to group bots in ip list, otherwise sends to all when using $ notation

eg:

$$%[Zoinks,Jinkies]HAZARDS-ON;

In the animation there is a RepeatScript flag you can check to have the animation repeat until the interaction is over.


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



