using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Conversation.Common;
using MistyCharacter;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MistyRobotics.SDK.Responses;
using Newtonsoft.Json;
using SkillTools.AssetTools;

namespace MistyManager
{
    public class ConversationManager : IDisposable
    {
		private IRobotMessenger _misty;		
		private IBaseCharacter _character;
		private IAssetWrapper _assetWrapper;
		private IDictionary<string, object> _parameters = new Dictionary<string, object>();
		private ParameterManager _parameterManager;
		private ManagerConfiguration _managerConfiguration = new ManagerConfiguration();
		private CharacterParameters _characterParameters = new CharacterParameters();
		private Timer _publishConversationTimer;
		private string _runningConversation;
		
		public ConversationManager(IRobotMessenger misty, IDictionary<string, object> parameters = null, ManagerConfiguration managerConfiguration = null)
		{
			_misty = misty;
			_managerConfiguration = managerConfiguration;
			_parameters = parameters;
		}

		public async Task<bool> CheckAudio()
		{
			try
			{
				bool audioEnabled = false;
				bool cameraEnabled = false;
				IRobotCommandResponse audioResults = null;
				IRobotCommandResponse cameraResults = null;
				cameraEnabled = (await _misty.CameraServiceEnabledAsync()).Data;
				audioEnabled = (await _misty.AudioServiceEnabledAsync()).Data;
				if (!cameraEnabled)
				{
					cameraResults = await _misty.EnableCameraServiceAsync();
					cameraEnabled = cameraResults.Status == ResponseStatus.Success;
				}
				if (!audioEnabled)
				{
					audioResults = await _misty.EnableAudioServiceAsync();
					audioEnabled = audioResults.Status == ResponseStatus.Success;
				}

				if ((!cameraEnabled || !audioEnabled))
				{
					return false;
				}
				return true;
			}
			catch (Exception ex)
			{
				return false;
			}
		}

		public async Task<bool> ResetRobot()
		{
			try
			{
				//Revert display to defaults before starting init process...
				await _misty.SetDisplaySettingsAsync(true);
				_misty.SetBlinkSettings(true, null, null, null, null, null, null);

				_assetWrapper.ShowSystemImage(SystemImage.DefaultContent);
			
				_misty.ChangeLED(0, 255, 255, null);
				_misty.MoveArms(65.0, 65.0, 50, 50, null, AngularUnit.Degrees, null);
				_misty.MoveHead(0, 0, 0, 75, AngularUnit.Degrees, null);

			}
			catch (Exception ex)
			{
				_assetWrapper.ShowSystemImage(SystemImage.DefaultContent);
				_misty.Speak("Sorry, I am having trouble initializing the conversation and cannot continue.", true, null, null);
				_misty.DisplayText($"Failed manager initialization", "Text", null);
				_misty.SkillLogger.Log("Exception thrown in conversation manager.", ex);
				await Task.Delay(4000);
			}
			finally
			{
				_misty.SetTextDisplaySettings("Text", new TextSettings
				{
					Deleted = true
				}, null);
			}
			return false;
		}


		public async void StartConversationCallback(IUserEvent userEvent)
		{
			try
			{
				if (userEvent.TryGetPayload(out IDictionary<string, object> eventPayload))
				{
					if (eventPayload.TryGetValue("ConversationGroupId", out object conversationId))
					{
						eventPayload.TryGetValue("ConversationData", out object data);

						IDictionary<string, object> newData = data == null ? new Dictionary<string, object>() : (IDictionary<string, object>)data;
						newData.Remove("ConversationGroupId");
						newData.Add("ConversationGroupId", conversationId);
						if (!string.IsNullOrWhiteSpace(_runningConversation))
						{
							if (_character != null)
							{
								await _character?.StopConversation();
								_character?.Dispose();
							}
							await Task.Delay(10000);//TODO
							_runningConversation = null;
						}

						_misty.RegisterUserEvent("LoadConversation", LoadConversationCallback, 0, true, null);
						_misty.RegisterUserEvent("StartConversation", StartConversationCallback, 0, true, null);
						_misty.RegisterUserEvent("RemoveConversation", RemoveConversationCallback, 0, true, null);
						_misty.RegisterUserEvent("StopConversation", StopConversationCallback, 0, true, null);



						_ = Initialize(new BasicMisty(_misty, newData, new ManagerConfiguration()));

						//await _parameterManager.StartConversation(Convert.ToString(conversationId), data == null ? new Dictionary<string, object>() : (IDictionary<string, object>)data);
						return;
					}
					_misty.SkillLogger.LogError("Missing parameters or improper json format.");
				}
				else
				{
					_misty.SkillLogger.LogError("Failed to get Load Conversation payload.");
				}
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogError("Exception getting Load Conversation payload.", ex);
			}
		}
		
		public async void  StopConversationCallback(IUserEvent userEvent)
		{
			try
			{
				//Stop conversation
				if(_character != null)
				{
					await _character?.StopConversation();
					_character?.Dispose();  //make awaitable?
					await Task.Delay(5000);//let it clean all dee shite before resetting

					_misty.RegisterUserEvent("LoadConversation", LoadConversationCallback, 0, true, null);
					_misty.RegisterUserEvent("StartConversation", StartConversationCallback, 0, true, null);
					_misty.RegisterUserEvent("RemoveConversation", RemoveConversationCallback, 0, true, null);
					_misty.RegisterUserEvent("StopConversation", StopConversationCallback, 0, true, null);
				}
				_runningConversation = null;
				_characterParameters.InitializationErrorStatus = InitializationStatus.Waiting;

			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogError("Exception getting Load Conversation payload.", ex);
			}
		}

		public async void LoadConversationCallback(IUserEvent userEvent)
		{
			try
			{
				if (userEvent.TryGetPayload(out IDictionary<string, object> eventPayload))
				{
					if (eventPayload.TryGetValue("ConversationGroup", out object conversation))
					{

						if (conversation == null)
						{
							return;
						}

						CharacterParameters cp = JsonConvert.DeserializeObject<CharacterParameters>(Convert.ToString(conversation));

						await _parameterManager.LoadConversation(cp);
						return;
					}
					_misty.SkillLogger.LogError("Missing parameters or improper json format.");
				}
				else
				{
					_misty.SkillLogger.LogError("Failed to get Load Conversation payload.");
				}
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogError("Exception getting Load Conversation payload.", ex);
			}
		}
		
		public async void RemoveConversationCallback(IUserEvent userEvent)
		{
			try
			{
				if (userEvent.TryGetPayload(out IDictionary<string, object> eventPayload))
				{
					if (eventPayload.TryGetValue("ConversationGroupId", out object conversationId))
					{
						await _parameterManager.RemoveConversation(Convert.ToString(conversationId));
					}
					_misty.SkillLogger.LogError("Missing conversation Id or improper json format.");
				}
				else
				{
					_misty.SkillLogger.LogError("Failed to get Load Conversation payload.");
				}
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogError("Exception getting Load Conversation payload.", ex);
			}		
		}

		public async Task<bool> Initialize(IBaseCharacter character = null)
		{
			try
			{
				_assetWrapper = new AssetWrapper(_misty);
				await _assetWrapper.LoadAssets(true);

				await ResetRobot();
				//await Task.Delay(2500)//waiti for things to unregister and reset

				_parameters = character.OriginalParameters;

				_misty.RegisterUserEvent("LoadConversation", LoadConversationCallback, 0, true, null);
				_misty.RegisterUserEvent("StartConversation", StartConversationCallback, 0, true, null);
				_misty.RegisterUserEvent("RemoveConversation", RemoveConversationCallback, 0, true, null);
				_misty.RegisterUserEvent("StopConversation", StopConversationCallback, 0, true, null);

				await _misty.SetTextDisplaySettingsAsync("Text", new TextSettings
				{
					Wrap = true,
					Visible = true,
					Weight = 15,
					Size = 20,
					HorizontalAlignment = ImageHorizontalAlignment.Center,
					VerticalAlignment = ImageVerticalAlignment.Bottom,
					Red = 255,
					Green = 255,
					Blue = 255,
					PlaceOnTop = true,
					FontFamily = "Courier New",
					Height = 40
				});

				_misty.DisplayText("Waking robot...", "Text", null);
				await Task.Delay(2000);

				_publishConversationTimer = new Timer(PublishTimerCallback, null, 0, 15000);

				if (!await CheckAudio())
				{
					_misty.Speak("I am having audio or camera issues. Please restart the skill or robot.", true, null, null);
					_misty.DisplayText($"Unknown camera or audio status.", "Text", null);
					_misty.SkillLogger.Log($"Failed to get enabled response from audio or camera.");
					return false;
				}

				_parameterManager = new ParameterManager(_misty, _parameters);				
				if (_parameterManager == null)
				{
					_misty.DisplayText($"Failed initialization.", "Text", null);
					_misty.SkillLogger.Log($"Failed misty conversation skill initialization.  Cancelling skill.");
					_misty.SkillCompleted();
					return false;
				}
				
				_characterParameters = await _parameterManager.Initialize();
				if (_characterParameters != null && _characterParameters.InitializationErrorStatus != InitializationStatus.Error)
				{
					if (_characterParameters.InitializationErrorStatus == InitializationStatus.Waiting || _characterParameters.InitializationErrorStatus == InitializationStatus.Unknown)
					{
						await _misty.DisplayTextAsync("Waiting for conversation!", "Text");
						_assetWrapper.ShowSystemImage(SystemImage.Joy);
						await Task.Delay(2000);

						if (_characterParameters.IgnoreArmCommands || _characterParameters.IgnoreHeadCommands)
						{
							await _misty.HaltAsync(new List<MotorMask> { MotorMask.AllMotors });
						}
						return true;
					}

					if (_characterParameters.InitializationErrorStatus == InitializationStatus.Warning)
					{
						_misty.DisplayText(_characterParameters.InitializationStatusMessage ?? "Initialization Warning.", "Text", null);
						await _misty.TransitionLEDAsync(255, 39, 18, 240, 255, 48, LEDTransition.Breathe, 1000);
					}
					else
					{
						_characterParameters.InitializationErrorStatus = InitializationStatus.Success;
						await _misty.TransitionLEDAsync(0, 255, 0, 30, 144, 255, LEDTransition.Breathe, 1000);
					}
					
					string startupConversation = _characterParameters.ConversationGroup.StartupConversation;
					ConversationData conversationData = _characterParameters.ConversationGroup.Conversations.FirstOrDefault(x => x.Id == startupConversation);
					if (conversationData == null)
					{
						_misty.SkillLogger.Log($"Could not locate the starting conversation.");
						_misty.DisplayText($"Failed to start conversation.", "Text", null);
						return false;
					}

					_character = character == null ? new BasicMisty(_misty, _parameters) : character;

					bool initialized = await _character.Initialize(_characterParameters);
					if (initialized)
					{
						await _misty.DisplayTextAsync("Hello!", "Text");
						_assetWrapper.ShowSystemImage(SystemImage.Joy);
						await Task.Delay(2000);

						if (_characterParameters.IgnoreArmCommands || _characterParameters.IgnoreHeadCommands)
						{
							await _misty.HaltAsync(new List<MotorMask> { MotorMask.AllMotors });
						}

						await _character.StartConversation();
					}
					else
					{
						await _misty.DisplayTextAsync("Failed to wake up robot!", "Text");
						_assetWrapper.ShowSystemImage(SystemImage.Disoriented);
						await Task.Delay(5000);
					}
					
					return initialized;
				}
				else if(_characterParameters == null)
				{
					_misty.SkillLogger.Log($"Failed retrieving parameters and conversation.");
					_misty.DisplayText($"Failed to start conversation.", "Text", null);
					await Task.Delay(5000);
					return false;
				}
				else
				{
					_misty.SkillLogger.Log($"Failed retrieving parameters and conversation. {_characterParameters.InitializationStatusMessage}");
					_misty.DisplayText(_characterParameters.InitializationStatusMessage, "Text", null);
					await Task.Delay(5000);
					return false;
				}
			}
			catch (Exception ex)
			{
				_assetWrapper.ShowSystemImage(SystemImage.DefaultContent);
				_misty.Speak("Sorry, I am having trouble initializing the conversation and cannot continue.", true, null, null);
				_misty.DisplayText($"Failed manager initialization", "Text", null);
				_misty.SkillLogger.Log("Exception thrown in conversation manager.", ex);
				await Task.Delay(4000);
			}
			finally
			{
				_misty.SetTextDisplaySettings("Text", new TextSettings
				{
					Visible = false,
					Deleted = true
				}, null);
			}
			return false;
		}

		private void PublishTimerCallback(object timerData)
		{
			if(_parameterManager != null)
			{
				_parameterManager.PublishConversationList();
			}
		}

		//public async Task<bool> StartConversation()
		//{
		//	return await _character.StartConversation();
		//}

		#region IDisposable Support

		private bool _isDisposed = false;

		private void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					_character?.Dispose();
					_publishConversationTimer?.Dispose();
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
