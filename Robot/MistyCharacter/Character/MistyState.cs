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
using Conversation.Common;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;

namespace MistyCharacter
{
	public interface IMistyState
	{
		//TODO Complete!
		event EventHandler<IFaceRecognitionEvent> FaceRecognitionEvent;
		event EventHandler<ICapTouchEvent> CapTouchEvent;
		event EventHandler<IBumpSensorEvent> BumperEvent;
		event EventHandler<IBatteryChargeEvent> BatteryChargeEvent;
		event EventHandler<IQrTagDetectionEvent> QrTagEvent;
		event EventHandler<IArTagDetectionEvent> ArTagEvent;
		event EventHandler<ITimeOfFlightEvent> TimeOfFlightEvent;
		event EventHandler<ISerialMessageEvent> SerialMessageEvent;
		event EventHandler<IUserEvent> ExternalEvent;
		event EventHandler<IObjectDetectionEvent> ObjectEvent;
		event EventHandler<TriggerData> SpeechIntentEvent;
		event EventHandler<TriggerData> ResponseEventReceived;
		event EventHandler<string> StartedSpeaking;
		event EventHandler<IAudioPlayCompleteEvent> StoppedSpeaking;
		event EventHandler<DateTime> StartedListening;
		event EventHandler<IVoiceRecordEvent> StoppedListening;
		event EventHandler<bool> KeyPhraseRecognitionOn;
		event EventHandler<IDriveEncoderEvent> DriveEncoder;
		event EventHandler<IKeyPhraseRecognizedEvent> KeyPhraseRecognized;
		event EventHandler<IVoiceRecordEvent> CompletedProcessingVoice;
		event EventHandler<IVoiceRecordEvent> StartedProcessingVoice;


		event EventHandler<IActuatorEvent> LeftArmActuatorEvent;
		event EventHandler<IActuatorEvent> RightArmActuatorEvent;
		event EventHandler<IActuatorEvent> HeadPitchActuatorEvent;
		event EventHandler<IActuatorEvent> HeadYawActuatorEvent;
		event EventHandler<IActuatorEvent> HeadRollActuatorEvent;

		Task<bool> Initialize();
		void RegisterEvent(string trigger);
		void UnregisterEvent(string trigger);
	}

	public class MistyState : IMistyState
	{
		private IRobotMessenger _misty;
		private IDictionary<string, object> _parameters = new Dictionary<string, object>();
		private CharacterParameters _characterParameters = new CharacterParameters();
		private CharacterState _currentCharacterState = new CharacterState();
		
		//Allow others to register for these... pass on the event data

		//TODO Add missing and integrate private items into CharacterState
		public event EventHandler<IFaceRecognitionEvent> FaceRecognitionEvent;
		public event EventHandler<ICapTouchEvent> CapTouchEvent;
		public event EventHandler<IBumpSensorEvent> BumperEvent;
		public event EventHandler<IBatteryChargeEvent> BatteryChargeEvent;
		public event EventHandler<IQrTagDetectionEvent> QrTagEvent;
		public event EventHandler<IArTagDetectionEvent> ArTagEvent;
		public event EventHandler<ITimeOfFlightEvent> TimeOfFlightEvent;
		public event EventHandler<ISerialMessageEvent> SerialMessageEvent;
		public event EventHandler<IUserEvent> ExternalEvent;
		public event EventHandler<IObjectDetectionEvent> ObjectEvent;
		public event EventHandler<TriggerData> SpeechIntentEvent;
		public event EventHandler<TriggerData> ResponseEventReceived;
		public event EventHandler<string> StartedSpeaking;
		public event EventHandler<IAudioPlayCompleteEvent> StoppedSpeaking;
		public event EventHandler<DateTime> StartedListening;
		public event EventHandler<IVoiceRecordEvent> StoppedListening;
		public event EventHandler<bool> KeyPhraseRecognitionOn;
		public event EventHandler<IDriveEncoderEvent> DriveEncoder;
		public event EventHandler<IKeyPhraseRecognizedEvent> KeyPhraseRecognized;
		public event EventHandler<IVoiceRecordEvent> CompletedProcessingVoice;
		public event EventHandler<IVoiceRecordEvent> StartedProcessingVoice;

		
		public event EventHandler<IActuatorEvent> LeftArmActuatorEvent;
		public event EventHandler<IActuatorEvent> RightArmActuatorEvent;
		public event EventHandler<IActuatorEvent> HeadPitchActuatorEvent;
		public event EventHandler<IActuatorEvent> HeadYawActuatorEvent;
		public event EventHandler<IActuatorEvent> HeadRollActuatorEvent;

		//TODO Add missing and integrate private items into CharacterState

		private IObjectDetectionEvent _lastObjectEvent;
		private IObjectDetectionEvent _lastPersonEvent;
		private HeadLocation _currentHeadRequest = new HeadLocation(null, null, null);
		private DateTime _followedObjectLastSeen = DateTime.Now;
		private DateTime _lastMovementCommand = DateTime.Now;

		private double? _lastPitch = 0;
		private double? _lastYaw = 0;
		private double? _lastActuatorYaw;
		private double? _lastActuatorPitch;

		private bool _bumpSensorRegistered;
		private bool _capTouchRegistered;
		private bool _arTagRegistered;
		private bool _tofRegistered;
		private bool _qrTagRegistered;
		private bool _serialMessageRegistered;
		private bool _faceRecognitionRegistered;
		private bool _objectDetectionRegistered;
		
		public MistyState(IRobotMessenger misty, IDictionary<string, object> parameters,  CharacterParameters characterParameters)
		{
			_misty = misty;
			_parameters = parameters;
			_characterParameters = characterParameters;
		}
		
		public async Task<bool> Initialize()
		{
			try
			{
				_misty.UnregisterAllEvents(null); //in case last run was stopped abnormally (via debugger)
				await Task.Delay(2000); //time for unreg to happen before we rereg
				RegisterStartingEvents();
				return true;
			}
			catch
			{
				return false;
			}
		}

		#region Event Management

		private void RegisterStartingEvents()
		{
			//TODO Only register as needed
			RegisterArmEvents();
			RegisterHeadEvents();
			RegisterLocomotionEvents();

			LogEventDetails(_misty.RegisterBatteryChargeEvent(BatteryChargeCallback, 1000 * 60, true, null, "Battery", null));
			LogEventDetails(_misty.RegisterUserEvent("ExternalEvent", ExternalEventCallback, 0, true, null));
			LogEventDetails(_misty.RegisterUserEvent("SyncEvent", SyncEventCallback, 0, true, null));
			LogEventDetails(_misty.RegisterUserEvent("CrossRobotCommand", RobotCommandCallback, 0, true, null));
		}

		private void RegisterArmEvents()
		{

			//Arm Actuators
			IList<ActuatorPositionValidation> actuatorLeftArmValidations = new List<ActuatorPositionValidation>();
			actuatorLeftArmValidations.Add(new ActuatorPositionValidation(ActuatorPositionFilter.SensorName, ComparisonOperator.Equal, ActuatorPosition.LeftArm));
			LogEventDetails(_misty.RegisterActuatorEvent(LeftArmCallback, 50, true, actuatorLeftArmValidations, "LeftArm", null));

			IList<ActuatorPositionValidation> actuatorRightArmValidations = new List<ActuatorPositionValidation>();
			actuatorRightArmValidations.Add(new ActuatorPositionValidation(ActuatorPositionFilter.SensorName, ComparisonOperator.Equal, ActuatorPosition.RightArm));
			LogEventDetails(_misty.RegisterActuatorEvent(RightArmCallback, 50, true, actuatorRightArmValidations, "RightArm", null));
		}
		
		//TODO Only register as needed
		private void RegisterHeadEvents()
		{
			_currentHeadRequest = new HeadLocation(null, null, null);

			//Person object, used for following face
			List<ObjectValidation> personValidations = new List<ObjectValidation>();
			personValidations.Add(new ObjectValidation { Name = ObjectFilter.Description, Comparison = ComparisonOperator.Equal, ComparisonValue = "person" });
			LogEventDetails(_misty.RegisterObjectDetectionEvent(ObjectDetectionCallback, (int)Math.Abs(_characterParameters.ObjectDetectionDebounce * 1000), true, personValidations, "ODEventForFace", null));

			List<ObjectValidation> objectValidations = new List<ObjectValidation>();
			objectValidations.Add(new ObjectValidation { Name = ObjectFilter.Description, Comparison = ComparisonOperator.NotEqual, ComparisonValue = "person" });
			LogEventDetails(_misty.RegisterObjectDetectionEvent(ObjectDetectionCallback, (int)Math.Abs(_characterParameters.ObjectDetectionDebounce * 1000), true, objectValidations, "GenericODEvent", null));

			//Head Actuators for following actions.
			IList<ActuatorPositionValidation> actuatorYawValidations = new List<ActuatorPositionValidation>();
			actuatorYawValidations.Add(new ActuatorPositionValidation(ActuatorPositionFilter.SensorName, ComparisonOperator.Equal, ActuatorPosition.HeadYaw));
			//LogEventDetails(Misty.RegisterActuatorEvent(ActuatorCallback, (int)Math.Abs(CharacterParameters.ObjectDetectionDebounce *1000), true, actuatorYawValidations, "HeadYaw", null));
			LogEventDetails(_misty.RegisterActuatorEvent(ActuatorCallback, 0, true, actuatorYawValidations, "HeadYaw", null));

			IList<ActuatorPositionValidation> actuatorPitchValidations = new List<ActuatorPositionValidation>();
			actuatorPitchValidations.Add(new ActuatorPositionValidation(ActuatorPositionFilter.SensorName, ComparisonOperator.Equal, ActuatorPosition.HeadPitch));
			//LogEventDetails(Misty.RegisterActuatorEvent(ActuatorCallback, (int)Math.Abs(CharacterParameters.ObjectDetectionDebounce *1000), true, actuatorPitchValidations, "HeadPitch", null));
			LogEventDetails(_misty.RegisterActuatorEvent(ActuatorCallback, 0, true, actuatorPitchValidations, "HeadPitch", null));

			IList<ActuatorPositionValidation> actuatorRollValidations = new List<ActuatorPositionValidation>();
			actuatorRollValidations.Add(new ActuatorPositionValidation(ActuatorPositionFilter.SensorName, ComparisonOperator.Equal, ActuatorPosition.HeadRoll));
			//LogEventDetails(Misty.RegisterActuatorEvent(ActuatorCallback, (int)Math.Abs(CharacterParameters.ObjectDetectionDebounce *1000), true, actuatorPitchValidations, "HeadPitch", null));
			LogEventDetails(_misty.RegisterActuatorEvent(ActuatorCallback, 0, true, actuatorRollValidations, "HeadRoll", null));

			_misty.StartObjectDetector(_characterParameters.PersonConfidence, 0, _characterParameters.TrackHistory, null);

		}

		public void RegisterLocomotionEvents()
		{
			//Register Bump Sensors with a callback
			//_misty.RegisterBumpSensorEvent(BumpCallback, 0, true, null, null, null);

			//Front Right Time of Flight
			List<TimeOfFlightValidation> tofFrontRightValidations = new List<TimeOfFlightValidation>();
			tofFrontRightValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.SensorName, Comparison = ComparisonOperator.Equal, ComparisonValue = TimeOfFlightPosition.FrontRight });
			_misty.RegisterTimeOfFlightEvent(TOFFRRangeCallback, 0, true, tofFrontRightValidations, "FrontRight", null);

			//Front Left Time of Flight
			List<TimeOfFlightValidation> tofFrontLeftValidations = new List<TimeOfFlightValidation>();
			tofFrontLeftValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.SensorName, Comparison = ComparisonOperator.Equal, ComparisonValue = TimeOfFlightPosition.FrontLeft });
			_misty.RegisterTimeOfFlightEvent(TOFFLRangeCallback, 0, true, tofFrontLeftValidations, "FrontLeft", null);

			//Front Center Time of Flight
			List<TimeOfFlightValidation> tofFrontCenterValidations = new List<TimeOfFlightValidation>();
			tofFrontCenterValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.SensorName, Comparison = ComparisonOperator.Equal, ComparisonValue = TimeOfFlightPosition.FrontCenter });
			_misty.RegisterTimeOfFlightEvent(TOFCRangeCallback, 0, true, tofFrontCenterValidations, "FrontCenter", null);

			//Back Time of Flight
			List<TimeOfFlightValidation> tofBackValidations = new List<TimeOfFlightValidation>();
			tofBackValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.SensorName, Comparison = ComparisonOperator.Equal, ComparisonValue = TimeOfFlightPosition.Back });
			_misty.RegisterTimeOfFlightEvent(TOFBRangeCallback, 0, true, tofBackValidations, "Back", null);

			//Setting debounce a little higher to avoid too much traffic
			//Firmware will do the actual stop for edge detection
			List<TimeOfFlightValidation> tofFrontRightEdgeValidations = new List<TimeOfFlightValidation>();
			tofFrontRightEdgeValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.SensorName, Comparison = ComparisonOperator.Equal, ComparisonValue = TimeOfFlightPosition.DownwardFrontRight });
			_misty.RegisterTimeOfFlightEvent(FrontEdgeCallback, 1000, true, tofFrontRightEdgeValidations, "FREdge", null);

			List<TimeOfFlightValidation> tofFrontLeftEdgeValidations = new List<TimeOfFlightValidation>();
			tofFrontLeftEdgeValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.SensorName, Comparison = ComparisonOperator.Equal, ComparisonValue = TimeOfFlightPosition.DownwardFrontLeft });
			_misty.RegisterTimeOfFlightEvent(FrontEdgeCallback, 1000, true, tofFrontLeftEdgeValidations, "FLEdge", null);

			IList<DriveEncoderValidation> driveValidations = new List<DriveEncoderValidation>();
			LogEventDetails(_misty.RegisterDriveEncoderEvent(EncoderCallback, 250, true, driveValidations, "DriveEncoder", null));

			LogEventDetails(_misty.RegisterIMUEvent(IMUCallback, 50, true, null, "IMU", null));

		}
		
		public void UnregisterEvent(string trigger)
		{
			if (string.IsNullOrWhiteSpace(trigger))
			{
				return;
			}

			trigger = trigger.Trim();
			//Register events and start services as needed if it is the first time we see this trigger
			if (_bumpSensorRegistered && (string.Equals(trigger, Triggers.BumperPressed, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(trigger, Triggers.BumperReleased, StringComparison.OrdinalIgnoreCase)))
			{
				_misty.UnregisterEvent("BumpSensor", null);
				_bumpSensorRegistered = false;
			}
			else if (_capTouchRegistered && (string.Equals(trigger, Triggers.CapTouched, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(trigger, Triggers.CapReleased, StringComparison.OrdinalIgnoreCase)))
			{
				_misty.UnregisterEvent("CapTouch", null);
				_capTouchRegistered = false;
			}
			else if (_arTagRegistered && string.Equals(trigger, Triggers.ArTagSeen, StringComparison.OrdinalIgnoreCase))
			{
				_misty.StartArTagDetector(7, 140, null);
				_misty.UnregisterEvent("ArTag", null);
				_arTagRegistered = false;
			}
			else if (_qrTagRegistered && string.Equals(trigger, Triggers.QrTagSeen, StringComparison.OrdinalIgnoreCase))
			{
				_misty.StartQrTagDetector(null);
				_misty.UnregisterEvent("QrTag", null);
				_qrTagRegistered = false;
			}
			else if (_serialMessageRegistered && string.Equals(trigger, Triggers.SerialMessage, StringComparison.OrdinalIgnoreCase))
			{
				_misty.UnregisterEvent("SerialMessage", null);
				_serialMessageRegistered = false;
			}
			else if (_faceRecognitionRegistered && string.Equals(trigger, Triggers.FaceRecognized, StringComparison.OrdinalIgnoreCase))
			{
				_misty.StopFaceRecognition(null);
				_misty.UnregisterEvent("FaceRecognition", null);
				_faceRecognitionRegistered = false;
			}
			else if (_objectDetectionRegistered && string.Equals(trigger, Triggers.ObjectSeen, StringComparison.OrdinalIgnoreCase))
			{
				//TODO!!
				_misty.StopObjectDetector(null);
				_misty.UnregisterEvent("ObjectDetection", null);
				_objectDetectionRegistered = false;
			}
			else if (_tofRegistered && string.Equals(trigger, Triggers.TimeOfFlightRange, StringComparison.OrdinalIgnoreCase))
			{
				_misty.UnregisterEvent("TimeOfFlight", null);
				_tofRegistered = false;
			}
		}

		public void RegisterEvent(string trigger)
		{
			if (string.IsNullOrWhiteSpace(trigger))
			{
				return;
			}

			trigger = trigger.Trim();
			//Register events and start services as needed if it is the first time we see this trigger
			if (!_bumpSensorRegistered && (string.Equals(trigger, Triggers.BumperPressed, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(trigger, Triggers.BumperReleased, StringComparison.OrdinalIgnoreCase)))
			{
				LogEventDetails(_misty.RegisterBumpSensorEvent(BumpCallback, 50, true, null, "BumpSensor", null));
				_bumpSensorRegistered = true;
			}
			else if (!_capTouchRegistered && (string.Equals(trigger, Triggers.CapTouched, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(trigger, Triggers.CapReleased, StringComparison.OrdinalIgnoreCase)))
			{
				LogEventDetails(_misty.RegisterCapTouchEvent(CapTouchCallback, 50, true, null, "CapTouch", null));
				_capTouchRegistered = true;
			}
			else if (!_arTagRegistered && string.Equals(trigger, Triggers.ArTagSeen, StringComparison.OrdinalIgnoreCase))
			{
				_misty.StartArTagDetector(7, 140, null);
				LogEventDetails(_misty.RegisterArTagDetectionEvent(ArTagCallback, 250, true, "ArTag", null));
				_arTagRegistered = true;
			}
			else if (!_qrTagRegistered && string.Equals(trigger, Triggers.QrTagSeen, StringComparison.OrdinalIgnoreCase))
			{
				_misty.StartQrTagDetector(null);
				LogEventDetails(_misty.RegisterQrTagDetectionEvent(QrTagCallback, 250, true, "QrTag", null));
				_qrTagRegistered = true;
			}
			else if (!_serialMessageRegistered && string.Equals(trigger, Triggers.SerialMessage, StringComparison.OrdinalIgnoreCase))
			{
				LogEventDetails(_misty.RegisterSerialMessageEvent(SerialMessageCallback, 0, true, "SerialMessage", null));
				_serialMessageRegistered = true;
			}
			else if (!_faceRecognitionRegistered && string.Equals(trigger, Triggers.FaceRecognized, StringComparison.OrdinalIgnoreCase))
			{
				//Misty.StopObjectDetector(null);
				_misty.StartFaceRecognition(null);
				//Misty.StartFaceDetection(null);
				LogEventDetails(_misty.RegisterFaceRecognitionEvent(FaceRecognitionCallback, 250, true, null, "FaceRecognition", null));
				_faceRecognitionRegistered = true;
			}
			else if (!_objectDetectionRegistered && string.Equals(trigger, Triggers.ObjectSeen, StringComparison.OrdinalIgnoreCase))
			{
				//TODO!!
				_misty.StartObjectDetector(_characterParameters.PersonConfidence, 0, 2, null);
				LogEventDetails(_misty.RegisterObjectDetectionEvent(ObjectDetectionCallback, 250, true, null, "ObjectDetection", null));
				_objectDetectionRegistered = true;
			}
			else if (!_tofRegistered && string.Equals(trigger, Triggers.TimeOfFlightRange, StringComparison.OrdinalIgnoreCase))
			{
				LogEventDetails(_misty.RegisterTimeOfFlightEvent(TimeOfFlightCallback, 100, true, null, "TimeOfFlight", null));
				_tofRegistered = true;
			}
		}

		#endregion

		#region Speech Event Handlers

		private void HandlePreSpeechCompleted(object sender, IAudioPlayCompleteEvent e)
		{
			//TODO
		}

		private void HandleKeyPhraseRecognized(object sender, IKeyPhraseRecognizedEvent keyPhraseEvent)
		{
			_currentCharacterState.KeyPhraseRecognized = (KeyPhraseRecognizedEvent)keyPhraseEvent;						
			//KeyPhraseRecognized?.Invoke(this, keyPhraseEvent);
		}
		
		private void HandleStartedProcessingVoice(object sender, IVoiceRecordEvent e)
		{
			//StartedProcessingVoice?.Invoke(this, e);   
        }

		private void HandleCompletedProcessingVoice(object sender, IVoiceRecordEvent e)
		{
			//CompletedProcessingVoice?.Invoke(this, e);   
        }

		private void HandleKeyPhraseRecognitionOn(object sender, bool e)
		{
			if (_currentCharacterState == null)
			{
				return;
			}
			_currentCharacterState.KeyPhraseRecognitionOn = e;
			//KeyPhraseRecognitionOn?.Invoke(this, e);
		}

		private void HandleStoppedListening(object sender, IVoiceRecordEvent e)
		{
			if (_currentCharacterState == null || e == null)
			{
				return;
			}
			_currentCharacterState.Listening = false;
			//StoppedListening?.Invoke(this, e);
		}

		private void HandleStartedListening(object sender, DateTime e)
		{
			if (_currentCharacterState == null || e == null)
			{
				return;
			}
			_currentCharacterState.Listening = true;
			//StartedListening?.Invoke(this, e);
		}

		private void HandleStoppedSpeaking(object sender, IAudioPlayCompleteEvent e)
		{
			//e could be null if exception thrown when trying to speak
			if (_currentCharacterState == null || e == null)
			{
				return;
			}
			_currentCharacterState.Speaking = false;
			_currentCharacterState.Saying = "";
			_currentCharacterState.Spoke = true;

		//	StoppedSpeaking?.Invoke(this, e);
		}

		private void HandleStartedSpeaking(object sender, string e)
		{
			if (_currentCharacterState == null || e == null)
			{
				return;
			}
			_currentCharacterState.Speaking = true;
			//StartedSpeaking?.Invoke(this, e);
		}

		private void HandleSpeechIntent(object sender, TriggerData speechIntent)
		{
			if (_currentCharacterState == null)
			{
				return;
			}
			_currentCharacterState.SpeechResponseEvent = speechIntent;
			//SpeechIntentEvent?.Invoke(this, speechIntent);
		}

		#endregion Speech Event Handlers

		#region Robot Event Callbacks
		
		private void LeftArmCallback(IActuatorEvent leftArmEvent)
		{
			LeftArmActuatorEvent?.Invoke(this, leftArmEvent);
		}

		private void RightArmCallback(IActuatorEvent rightArmEvent)
		{
			RightArmActuatorEvent?.Invoke(this, rightArmEvent);
		}

		private void ObjectDetectionCallback(IObjectDetectionEvent objEvent)
		{
			try
			{
				if (_currentCharacterState == null ||
				(_currentCharacterState.ObjectEvent != null && objEvent.Created == _currentCharacterState.ObjectEvent.Created))
				{
					return;
				}

				if (objEvent.Description == "person")
				{
					_followedObjectLastSeen = DateTime.Now;
					_lastPersonEvent = objEvent;
					_lastObjectEvent = null;
				}
				else if (objEvent.Description.ToLower() == _currentHeadRequest?.FollowObject?.ToLower())
				{
					_currentCharacterState.NonPersonObjectEvent = (ObjectDetectionEvent)objEvent;
					_followedObjectLastSeen = DateTime.Now;
					_lastObjectEvent = objEvent;
					_lastPersonEvent = null;
				}
				else
				{
					_lastObjectEvent = null;
					_lastPersonEvent = null;
				}
				
				_currentCharacterState.ObjectEvent = (ObjectDetectionEvent)objEvent;				
				ObjectEvent?.Invoke(this, objEvent);
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.Log("Failed processing face event.", ex);
			}
		}

		private void ActuatorCallback(IActuatorEvent actuatorEvent)
		{
			switch (actuatorEvent.SensorPosition)
			{
				case ActuatorPosition.HeadPitch:
					_lastActuatorPitch = actuatorEvent.ActuatorValue;
					HeadPitchActuatorEvent?.Invoke(this, actuatorEvent);
					_currentCharacterState.HeadPitchActuatorEvent = (ActuatorEvent)actuatorEvent;
					break;
				case ActuatorPosition.HeadYaw:
					_lastActuatorYaw = actuatorEvent.ActuatorValue;
					HeadYawActuatorEvent?.Invoke(this, actuatorEvent);
					_currentCharacterState.HeadYawActuatorEvent = (ActuatorEvent)actuatorEvent;
					break;
				case ActuatorPosition.HeadRoll:
					HeadRollActuatorEvent?.Invoke(this, actuatorEvent);
					_currentCharacterState.HeadRollActuatorEvent = (ActuatorEvent)actuatorEvent;
					break;
			}
		}
		
		private void TimeOfFlightCallback(ITimeOfFlightEvent timeOfFlightEvent)
		{
			if (_currentCharacterState == null)
			{
				return;
			}
			_currentCharacterState.TimeOfFlightEvent = (TimeOfFlightEvent)timeOfFlightEvent;
			TimeOfFlightEvent?.Invoke(this, timeOfFlightEvent);
		}

		private void ArTagCallback(IArTagDetectionEvent arTagEvent)
		{
			if (_currentCharacterState == null ||
				(_currentCharacterState.ArTagEvent != null && arTagEvent.Created == _currentCharacterState.ArTagEvent.Created))
			{
				return;
			}
			_currentCharacterState.ArTagEvent = (ArTagDetectionEvent)arTagEvent;
			ArTagEvent?.Invoke(this, arTagEvent);
		}

		private void QrTagCallback(IQrTagDetectionEvent qrTagEvent)
		{
			if (_currentCharacterState == null ||
				(_currentCharacterState.QrTagEvent != null && qrTagEvent.Created == _currentCharacterState.QrTagEvent.Created))
			{
				return;
			}
			_currentCharacterState.QrTagEvent = (QrTagDetectionEvent)qrTagEvent;			
			QrTagEvent?.Invoke(this, qrTagEvent);
		}

		private void SerialMessageCallback(ISerialMessageEvent serialMessageEvent)
		{
			if (_currentCharacterState == null ||
				(_currentCharacterState.SerialMessageEvent != null && serialMessageEvent.Created == _currentCharacterState.SerialMessageEvent.Created))
			{
				return;
			}
			_currentCharacterState.SerialMessageEvent = (SerialMessageEvent)serialMessageEvent;
			SerialMessageEvent?.Invoke(this, serialMessageEvent);
		}

		private void FaceRecognitionCallback(IFaceRecognitionEvent faceRecognitionEvent)
		{
			string _lastKnownFace = _currentCharacterState.LastKnownFaceSeen;
			if (_currentCharacterState == null ||
				(_currentCharacterState.FaceRecognitionEvent != null && faceRecognitionEvent.Created == _currentCharacterState.FaceRecognitionEvent.Created))
			{
				return;
			}

			_currentCharacterState.FaceRecognitionEvent = (FaceRecognitionEvent)faceRecognitionEvent;

			if(faceRecognitionEvent.Label != ConversationConstants.UnknownPersonFaceLabel)
			{
				_currentCharacterState.LastKnownFaceSeen = faceRecognitionEvent.Label;
			}
			FaceRecognitionEvent?.Invoke(this, faceRecognitionEvent);
		}
		
		private void CapTouchCallback(ICapTouchEvent capTouchEvent)
		{
			if (_currentCharacterState == null ||
				(_currentCharacterState.CapTouchEvent != null && capTouchEvent.Created == _currentCharacterState.CapTouchEvent.Created))
			{
				return;
			}

			_currentCharacterState.CapTouchEvent = (CapTouchEvent)capTouchEvent;
			CapTouchEvent?.Invoke(this, capTouchEvent);
		}
		
		private void BatteryChargeCallback(IBatteryChargeEvent batteryEvent)
		{
			if (_currentCharacterState == null)
			{
				return;
			}
			_currentCharacterState.BatteryChargeEvent = (BatteryChargeEvent)batteryEvent;
			BatteryChargeEvent?.Invoke(this, batteryEvent);
		}
		
		private void EncoderCallback(IDriveEncoderEvent encoderEvent)
		{

			if (_currentCharacterState == null)
			{
				return;
			}
			_currentCharacterState.LocomotionState.LeftDistanceSinceLastStop = encoderEvent.LeftDistance;
			_currentCharacterState.LocomotionState.RightDistanceSinceLastStop = encoderEvent.RightDistance;
			_currentCharacterState.LocomotionState.LeftVelocity = encoderEvent.LeftVelocity;
			_currentCharacterState.LocomotionState.RightVelocity = encoderEvent.RightVelocity;
			_currentCharacterState.DriveEncoder = (DriveEncoderEvent)encoderEvent;
			DriveEncoder?.Invoke(this, encoderEvent);
		}

		private void IMUCallback(IIMUEvent imuEvent)
		{
			_currentCharacterState.LocomotionState.RobotPitch = imuEvent.Pitch;
			_currentCharacterState.LocomotionState.RobotYaw = imuEvent.Yaw;
			_currentCharacterState.LocomotionState.RobotRoll = imuEvent.Roll;
			_currentCharacterState.LocomotionState.XAcceleration = imuEvent.XAcceleration;
			_currentCharacterState.LocomotionState.YAcceleration = imuEvent.YAcceleration;
			_currentCharacterState.LocomotionState.ZAcceleration = imuEvent.ZAcceleration;
			_currentCharacterState.LocomotionState.PitchVelocity = imuEvent.PitchVelocity;
			_currentCharacterState.LocomotionState.RollVelocity = imuEvent.RollVelocity;
			_currentCharacterState.LocomotionState.YawVelocity = imuEvent.YawVelocity;
		}

		private void TOFFLRangeCallback(ITimeOfFlightEvent tofEvent)
		{
			if (TryGetAdjustedDistance(tofEvent, out double distance))
			{
				_currentCharacterState.LocomotionState.FrontLeftTOF = distance;
			}
		}

		private void TOFFRRangeCallback(ITimeOfFlightEvent tofEvent)
		{
			if (TryGetAdjustedDistance(tofEvent, out double distance))
			{
				_currentCharacterState.LocomotionState.FrontRightTOF = distance;
			}
		}

		private void TOFCRangeCallback(ITimeOfFlightEvent tofEvent)
		{
			if (TryGetAdjustedDistance(tofEvent, out double distance))
			{
				_currentCharacterState.LocomotionState.FrontCenterTOF = distance;
			}
		}

		private void TOFBRangeCallback(ITimeOfFlightEvent tofEvent)
		{
			if (TryGetAdjustedDistance(tofEvent, out double distance))
			{
				_currentCharacterState.LocomotionState.BackTOF = distance;
			}
		}
		
		private void BumpCallback(IBumpSensorEvent bumpEvent)
		{
			if (_currentCharacterState == null ||
				(_currentCharacterState.BumpEvent != null && bumpEvent.Created == _currentCharacterState.BumpEvent.Created))
			{
				return;
			}

			switch (bumpEvent.SensorPosition)
			{
				case BumpSensorPosition.FrontRight:
					_currentCharacterState.LocomotionState.FrontRightBumpContacted = bumpEvent.IsContacted;
					break;
				case BumpSensorPosition.FrontLeft:
					_currentCharacterState.LocomotionState.FrontLeftBumpContacted = bumpEvent.IsContacted;
					break;
				case BumpSensorPosition.BackRight:
					_currentCharacterState.LocomotionState.BackRightBumpContacted = bumpEvent.IsContacted;
					break;
				case BumpSensorPosition.BackLeft:
					_currentCharacterState.LocomotionState.BackLeftBumpContacted = bumpEvent.IsContacted;
					break;
			}

			_currentCharacterState.BumpEvent = (BumpSensorEvent)bumpEvent;
			BumperEvent?.Invoke(this, bumpEvent);

		}

		private void FrontEdgeCallback(ITimeOfFlightEvent edgeEvent)
		{
			switch (edgeEvent.SensorPosition)
			{
				case TimeOfFlightPosition.DownwardFrontRight:
					_currentCharacterState.LocomotionState.FrontRightEdgeTOF = edgeEvent.DistanceInMeters;
					break;
				case TimeOfFlightPosition.DownwardFrontLeft:
					_currentCharacterState.LocomotionState.FrontLeftEdgeTOF = edgeEvent.DistanceInMeters;
					break;
			}
		}

		#endregion Robot Event Callbacks

		#region Cross robot and other event callbacks

		private void SyncEventCallback(IUserEvent userEvent)
		{
			//this is an external call to the animation manager of another bot (and self if set)			
			//AnimationManager.HandleSyncEvent(userEvent);

			//TODO Trigger in basecharacter
		}

		private void RobotCommandCallback(IUserEvent userEvent)
		{
			//this is an external call to the animation manager of another bot (and self if set)		
			//_ = AnimationManager.HandleExternalCommand(userEvent);

			//TODO Trigger in basecharacter

		}

		private void ExternalEventCallback(IUserEvent userEvent)
		{
			//TODO Deny cross robot communication per robot

			if (_currentCharacterState == null)
			{
				return;
			}
			_currentCharacterState.ExternalEvent = (UserEvent)userEvent;

			//Pull out the intent and text, required
			if (userEvent.TryGetPayload(out IDictionary<string, object> payload))
			{
				//Can override other triggers				
				ExternalEvent?.Invoke(this, userEvent);
			}
		}

		#endregion Cross robot and other event callbacks
		
		#region Helpers

		private void LogEventDetails(IEventDetails eventDetails)
		{
			_misty.SkillLogger.Log($"Registered event '{eventDetails.EventName}' at {DateTime.Now}.  Id = {eventDetails.EventId}, Type = {eventDetails.EventType}, KeepAlive = {eventDetails.KeepAlive}");
		}

		private bool TryGetAdjustedDistance(ITimeOfFlightEvent tofEvent, out double distance)
		{
			distance = 0;
			// From Testing, using this pattern for return data
			//   0 = valid range data
			// 101 = sigma fail - lower confidence but most likely good
			// 104 = Out of bounds - Distance returned is greater than distance we are confident about, but most likely good - error codes can be returned in distance field at this time :(  so ignore error code range
			if (tofEvent.Status == 0 ||
				(tofEvent.Status == 101 && tofEvent.DistanceInMeters >= 1) ||
				tofEvent.Status == 104)
			{
				distance = tofEvent.DistanceInMeters;
			}
			else if (tofEvent.Status == 102)
			{
				//102 generally indicates nothing substantial is in front of the robot so the TOF is returning the floor as a close distance
				//So ignore the distance returned and just set to 2 meters
				distance = 2;
			}
			else
			{
				//TOF returning uncertain data or really low confidence in distance, ignore value 
				return false;
			}
			return true;
		}

		#endregion Helpers

		#region IDisposable Support

		private bool _isDisposed = false;

		private void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					_misty.UnregisterAllEvents(null);
				}

				_isDisposed = true;
			}
		}

		public virtual void Dispose()
		{
			Dispose(true);
		}

		#endregion
	}
}
 
 