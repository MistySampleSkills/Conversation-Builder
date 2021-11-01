using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conversation.Common;
using MistyCharacter;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK;
using MistyRobotics.SDK.Messengers;
using MistyRobotics.SDK.Responses;
using SkillTools.AssetTools;

namespace MistyManager
{
    public class ConversationManager : IDisposable
    {
		private IDictionary<string, object> _parameters;

		private IRobotMessenger _misty;
		private ParameterManager _parameterManager;
		private ManagerConfiguration _managerConfiguration = new ManagerConfiguration();

		private IBaseCharacter _character;
		private CharacterParameters _characterParameters = new CharacterParameters();
		private IAssetWrapper _assetWrapper;

		public async Task<bool> Initialize(IBaseCharacter character = null)
		{
			try
			{
				_misty.DisplayText("Initializing robot systems...", "Text", null);
				await Task.Delay(1500);

				_assetWrapper = new AssetWrapper(_misty);
				await _assetWrapper.LoadAssets(true);
				_assetWrapper.ShowSystemImage(SystemImage.SystemGearPrompt);
				
				//Unregister events in case didn't shut down cleanly last time
				_misty.UnregisterAllEvents(null);

				//Revert display to defaults before starting...
				await _misty.SetDisplaySettingsAsync(true);
				_misty.SetBlinkSettings(true, null, null, null, null, null, null);

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
					Height = 60
				});

				_misty.ChangeLED(0, 255, 255, null);
				_misty.MoveArms(65.0, 65.0, 20, 20, null, AngularUnit.Degrees, null);

				_misty.DisplayText("Checking audio and camera systems...", "Text", null);
				_assetWrapper.ShowSystemImage(SystemImage.SystemCamera);
				await Task.Delay(2000);

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
						_misty.Speak("Can you hear me? I may be having audio or camera issues.", true, null, null);
						_misty.DisplayText($"Unknown camera or audio status.", "Text", null);
						_misty.SkillLogger.Log($"Failed to get enabled response from audio or camera.");
						await Task.Delay(4000);
						//Warned them, but try anyway in case they are just slow to come up
					}
				}
				catch (Exception ex)
				{
					_misty.Speak("Can you hear me? I may be having audio or camera issues?.", true, null, null);
					_misty.DisplayText($"Unknown camera or audio status.", "Text", null);
					_misty.SkillLogger.Log("Enable services threw an exception. 820 services not loaded properly.", ex);
					await Task.Delay(4000);
					//Warned them, but try anyway in case they are just slow to come up
				}

				_misty.DisplayText($"Checking robot directives...", "Text", null);
				_assetWrapper.ShowSystemImage(SystemImage.SystemLogoPrompt);
				await Task.Delay(2000);

				string startupConversation = _characterParameters.ConversationGroup.StartupConversation;
				ConversationData conversationData = _characterParameters.ConversationGroup.Conversations.FirstOrDefault(x => x.Id == startupConversation);
				if (conversationData == null)
				{
					_misty.SkillLogger.Log($"Could not locate the starting conversation.");
					_misty.DisplayText($"Failed to start conversation.", "SpokeText", null);
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

				_misty.DisplayText($"Turning robot on...", "Text", null);
				_assetWrapper.ShowSystemImage(SystemImage.SleepingZZZ);
				await Task.Delay(2000);

				_characterParameters = await _parameterManager.Initialize();
				if (_characterParameters != null && _characterParameters.InitializationErrorStatus != "Error")
				{
					if (_characterParameters.InitializationErrorStatus == "Warning")
					{
						_misty.DisplayText(_characterParameters.InitializationStatusMessage ?? "Initialization Warning.", "Text", null);
						await _misty.TransitionLEDAsync(255, 39, 18, 240, 255, 48, LEDTransition.Breathe, 1000);
					}
					else
					{
						_misty.DisplayText($"Loading conversation.", "Text", null);
						await _misty.TransitionLEDAsync(0, 255, 0, 30, 144, 255, LEDTransition.Breathe, 1000);
					}

					_character = character == null ? new BasicMisty(_misty, _parameters) : character;

					bool initialized = await _character.Initialize(_characterParameters);
					//bool initialized = await _character.Initialize();
					if (initialized)
					{
						_misty.DisplayText("Hello!", "Text", null);
						_assetWrapper.ShowSystemImage(SystemImage.DefaultContent);
					}
					else
					{
						_misty.DisplayText("Failed to wake up robot!", "Text", null);
					}
					
					await Task.Delay(2000);
					return initialized;
				}
			}
			catch (Exception ex)
			{
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

		public ConversationManager(IRobotMessenger misty, IDictionary<string, object> parameters = null, ManagerConfiguration managerConfiguration = null)
		{
			_misty = misty;
			_managerConfiguration = managerConfiguration;
			_parameters = parameters;
		}

		public async Task<bool> StartConversation()
		{
			return await _character.StartConversation();
		}


		#region IDisposable Support

		private bool _isDisposed = false;

		private void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{ }

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
