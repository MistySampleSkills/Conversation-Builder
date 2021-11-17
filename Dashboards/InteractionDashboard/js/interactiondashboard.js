/*
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
*/


$(document).ready(function () {

    var _lightSocket;
	var _fetchClient;
	var _toastMessageCounter = 0;
    var _robotDemoEventName = "";
    var _triggers;
    var _utterances;
	
    var ipInStorage = sessionStorage.getItem("ip");
    var ip = ipInStorage ? ipInStorage : "";

	const sleep = (milliseconds) => {
		return new Promise(resolve => setTimeout(resolve, milliseconds));
	};

	if (ip !== "") {
		ConnectToSockets();
		$("#ip-address").val(ip);
    }    

	$('[data-toggle="tooltip"]').tooltip();
	document.getElementById("led").style.borderColor = "#ff0000";
	function ReEnableConnectionButton() {

		$("#connect-button").html("Connect");
		$("#connect-button").addClass("connect");
        $("#ip-address").prop("disabled", false);
		$("#connect-button").prop("disabled", false);
	}

	async function ConnectToSockets() {

		$("#connect-button").html("Connecting ...");
		$("#connect-button").prop("disabled", true);
        $("#ip-address").prop("disabled", true);

		disconnectRequested = false;

		try {
			$("#connect-button").html("Disconnect");
			$("#connect-button").removeClass("connect");
			$("#connect-button").prop("disabled", false);
            $("#ip-address").prop("disabled", false);

			_fetchClient = new FetchClient(ip, 10000);
			ConnectToSocket();

			ShowToastMessage("Taking a picture... Smile!", 3000);
			await sleep(2000);
			$("#photo").attr("src", "http://" + ip + "/api/cameras/rgb?OverwriteExisting=true&Base64=false&CacheBreak=" + Math.floor(Math.random() * 1000));

		}
		catch {
			ShowToastMessage("Unable to connect to Misty!", 10000);
			ReEnableConnectionButton();
			return;
		}
	}

	function UnsubscribeAndDisconnect() {

		if (_lightSocket) {
			if (_robotDemoEventName) {
                _lightSocket.Unsubscribe(_robotDemoEventName);
			}

			_lightSocket.Disconnect(); 
		}
	}

	function DisconnectFromRobot() {

		connected = false;
		disconnectRequested = true;
		UnsubscribeAndDisconnect();

		$("#connect-button").html("Connect");
		$("#connect-button").addClass("connect");
        $("#ip-address").prop("disabled", false);
		$("#connect-button").prop("disabled", false);

		ShowToastMessage("Disconnected from Misty.", 5000);
	}

	async function ConnectToSocket() {

		try {
            _lightSocket = new LightSocket(ip, SocketOpenCallback, SocketCloseCallback, SocketErrorCallback);            
            _lightSocket.Connect();
		}
		catch
		{
			ShowToastMessage("Unable to connect to robot websocket!", 10000);
			ReEnableConnectionButton();
        }
    }

    //TODO This should update more than buttons
    function ProcessEvent(data) {
        
        //Change by type

        var eventMsg;
        if (data.hasOwnProperty('message')) {
            eventMsg = data.message;
        }

        if (eventMsg.hasOwnProperty('message')) {

			//TODO data is wrapped a little wrong
            var eventObject = JSON.parse(eventMsg.message);

            $("#data-type").val(eventObject.DataType);
			if(eventObject.DataType === "ui") {
				var action = eventObject.Action.trim().toLowerCase();

				if(action === "ui-text") {
					$('#text-ui').html(eventObject.Data.trim());
				}
				else if(action === "ui-image") {	
					$('#whiz-bang').html('<img style="max-width:100%;" src="' + eventObject.Data.trim() + '" alt="whiz-bang-image">');							
				}
				else if(action === "ui-audio") {
					var audio = new Audio(eventObject.Data.trim());
    				audio.play();
				}
				else if(action === "ui-speech") {
					var message = new SpeechSynthesisUtterance(eventObject.Data.trim()); 
					window.speechSynthesis.speak(message);
				}
				else if(action === "ui-led") {
					
  					document.getElementById("led").style.borderColor = "#" + eventObject.Data.trim();
					//$('#led').style.border("10px solid " + eventObject.Data.trim() + ";";
				}
			}
			else if(eventObject.DataType === "state") {
				//update robot state info
				
				var robotState = eventObject.State;

				$("#last-trigger").val(robotState.LastTrigger);
				$("#right-arm").val(robotState.RightArmActuatorEvent.ActuatorValue);
				$("#left-arm").val(robotState.LeftArmActuatorEvent.ActuatorValue);
				
				$("#head-pitch").val(robotState.HeadPitchActuatorEvent.ActuatorValue);
				$("#head-roll").val(robotState.HeadRollActuatorEvent.ActuatorValue);
				$("#head-yaw").val(robotState.HeadYawActuatorEvent.ActuatorValue);
				
				$("#scruff").val(robotState.Scruff.IsContacted);
				$("#chin").val(robotState.Chin.IsContacted);
				$("#front-cap").val(robotState.FrontCap.IsContacted);
				$("#back-cap").val(robotState.BackCap.IsContacted);
				$("#left-cap").val(robotState.LeftCap.IsContacted);
				$("#right-cap").val(robotState.RightCap.IsContacted);

				$("#front-right-bumper").val(robotState.LocomotionState.FrontRightBumpContacted);
				$("#front-left-bumper").val(robotState.LocomotionState.FrontLeftBumpContacted);
				$("#back-right-bumper").val(robotState.LocomotionState.BackRightBumpContacted);
				$("#back-left-bumper").val(robotState.LocomotionState.BackLeftBumpContacted);

				$("#robot-pitch").val(robotState.LocomotionState.RobotPitch);
				$("#robot-roll").val(robotState.LocomotionState.RobotRoll);
				$("#robot-yaw").val(robotState.LocomotionState.RobotYaw);
				
				$("#battery").val(robotState.BatteryChargeEvent.ChargePercent*100);
				$("#face-seen").val(robotState.LastKnownFaceSeen);
				$("#object-seen").val(robotState.ObjectEvent.RobotYaw);
				$("#serial-message").val(robotState.SerialMessageEvent.Message);
				$("#ar-tag").val(robotState.ArTagEvent.TagId);
				$("#qr-tag").val(robotState.QrTagEvent.DecodedInfo);
				
				$("#saying").val(robotState.LastSaid);
				$("#heard").val(robotState.LastHeard);
				$("#screen-text").val(robotState.DisplayedScreenText);

			}
			else if(eventObject.DataType === "interaction") {

				$("#robot-name").val(eventObject.RobotName);
				$("#current-interaction").val(eventObject.CurrentInteraction.Name);
				$("#current-conversation").val(eventObject.Conversation);
				$("#current-conversation-group").val(eventObject.ConversationGroup);
				
				_triggers = [];
				_utterances = [];
				_triggers  = eventObject.Triggers;
				_utterances = eventObject.Utterances;
				$('#button-container').html("");
				
				for(let i = 0; i < _triggers.length; ++i)
				{
						var button = document.createElement('button');
						button.type = 'button';
						button.innerHTML = _triggers[i].Name;
						button.className = 'btn-styled';
						button.value = _triggers[i].Id;
						
						button.onclick = function() {
							//or Id???
							SendTriggerEvent(_triggers[i].Trigger, _triggers[i].TriggerFilter, _triggers[i].Text)
						};
					 
						var container = document.getElementById('button-container');
						container.appendChild(button);
	
				}
	
				for(let i = 0; i < _utterances.length; ++i)
				{
					var button = document.createElement('button');
					button.type = 'button';
					button.innerHTML = "Speech: " + _utterances[i].Name;
					button.className = 'btn-styled';
					button.value = _utterances[i].Id;
					
					button.onclick = function() {
						//or Id???
						SendTriggerEvent(_utterances[i].Trigger, _utterances[i].TriggerFilter, _utterances[i].Text)
					};
				
					var container = document.getElementById('button-container');
					container.appendChild(button);
				}
			}
        }
    }

	async function SocketOpenCallback() {

        ShowToastMessage("Connected to robot, subscribing to robot demo event.", 5000);
        _robotDemoEventName = "RobotDemoEvent" + Math.floor(Math.random() * 1000);
        
		//eventName, msgType, debounceMs, property, inequality, value, returnProperty, eventCallback
        _lightSocket.Subscribe(_robotDemoEventName, "UserSkillData", 0, null, null, null, null, ProcessEvent);

		await sleep(200); 
	}

	function SocketCloseCallback() {

		console.log("Websocket closed");
	}

	function SocketErrorCallback(data) {

		console.log("Error connecting to websocket: ", data);
	}

    function SendTriggerEvent(trigger, filter, text)
    {
        //send event back up to skill
        if (!ip) {
            need2ConnectMessage();
            return;
        }
        
       // var command = $('#trigger-button').value;

        var payload = {
			"Skill": "8be20a90-1150-44ac-a756-ebe4de30689e",
			"EventName": "ExternalEvent",
			"Payload": { "Trigger": trigger, "TriggerFilter":filter, "Text": text}
		};

		ShowToastMessage("Sending command to Misty.");

		_fetchClient.PostCommand("skills/event", JSON.stringify(payload), function (data) {
			console.log("User event response for event :", data);
		});
    }

	$("#connect").submit(async function (e) {

		e.preventDefault();

		if ($("#connect-button").hasClass("connect")) {

            ip = $("#ip-address").val();

			if (!ip) {
				ShowToastMessage("Invalid Robot IP address.");
				ReEnableConnectionButton();
				return;
            }

            sessionStorage.setItem("ip", ip);
            ConnectToSockets();
		}
		else {

			DisconnectFromRobot();
		}
	});

    $("#send-speech").submit(async function (e) {

		e.preventDefault();

		if ($("#send-speech-button").hasClass("send-speech")) {

            text = $("#text").val();
            SendTriggerEvent("SpeechHeard", "HeardUnknownSpeech", text);            
            ShowToastMessage("Sending speech event..");
            return;
		}
        
        ShowToastMessage("Failed sending speech event...");		
	});


    $("#take-picture").on("click", function (e) {

        if (!ip) {
            need2ConnectMessage();
            return;
        }

        ShowToastMessage("Taking and displaying the picture...");
        $("#photo").attr("src", "http://" + ip + "/api/cameras/rgb?OverwriteExisting=true&Base64=false&CacheBreak=" + Math.floor(Math.random() * 1000));

    });
    
	$("#load-interactiondashboard").on("click", function (e) {

		if (!ip) {
			need2ConnectMessage();
			return;
		}
       
		ShowToastMessage("Loading interaction dashboard.");
    });
   

	function HandleCallResponse(successMsg, failMsg, data) {

		if (data.result && data.result === true) {
			ShowToastMessage(successMsg);
		} else {
			ShowToastMessage(failMsg);
		}
	}

	function need2ConnectMessage() {
		ShowToastMessage("Please connect to the robot first!");
		return null;
	}

	async function ShowToastMessage(message, timeInMs = 4000) {

		if (message) {

			console.log(message);

			var element = document.getElementById("toast");
			element.innerHTML = message;
			element.className = "show";

			_toastMessageCounter++; // increment counter for each message sent.
			await sleep(timeInMs);
			_toastMessageCounter--; // decrement counter for each message done.

			if (_toastMessageCounter === 0) {
				// when the last ShowToastMessage is done, then remove the toast message.
				element.className = element.className.replace("show", "");
			}
		}
	}

	window.onbeforeunload = function () {

		UnsubscribeAndDisconnect();
	};
});