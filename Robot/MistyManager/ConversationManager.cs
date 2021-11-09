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
		private IRobotMessenger _misty;		
		private IBaseCharacter _character;
		private IAssetWrapper _assetWrapper;
		private IDictionary<string, object> _parameters = new Dictionary<string, object>();
		private ParameterManager _parameterManager;
		private ManagerConfiguration _managerConfiguration = new ManagerConfiguration();
		private CharacterParameters _characterParameters = new CharacterParameters();
		
		public ConversationManager(IRobotMessenger misty, IDictionary<string, object> parameters = null, ManagerConfiguration managerConfiguration = null)
		{
			_misty = misty;
			_managerConfiguration = managerConfiguration;
			_parameters = parameters;
		}
		
		public async Task<bool> Initialize(IBaseCharacter character = null)
		{
			try
			{
				_misty.UnregisterAllEvents(null);

				//Revert display to defaults before starting init process...
				await _misty.SetDisplaySettingsAsync(true);
				_misty.SetBlinkSettings(true, null, null, null, null, null, null);
				
				_assetWrapper = new AssetWrapper(_misty);
				await _assetWrapper.LoadAssets(true);

				_assetWrapper.ShowSystemImage(SystemImage.DefaultContent);
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

				_misty.ChangeLED(0, 255, 255, null);
				_misty.MoveArms(65.0, 65.0, 20, 20, null, AngularUnit.Degrees, null);
				_misty.MoveHead(0, 0, 0, 75, AngularUnit.Degrees, null);
				
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
				
				
				_parameterManager = new ParameterManager(_misty, _parameters);
				if (_parameterManager == null)
				{
					_misty.DisplayText($"Failed initialization.", "Text", null);
					_misty.SkillLogger.Log($"Failed misty conversation skill initialization.  Cancelling skill.");
					_misty.SkillCompleted();
					return false;
				}
				
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
						await _misty.TransitionLEDAsync(0, 255, 0, 30, 144, 255, LEDTransition.Breathe, 1000);
					}


					string startupConversation = _characterParameters.ConversationGroup.StartupConversation;
					ConversationData conversationData = _characterParameters.ConversationGroup.Conversations.FirstOrDefault(x => x.Id == startupConversation);
					if (conversationData == null)
					{
						_misty.SkillLogger.Log($"Could not locate the starting conversation.");
						_misty.DisplayText($"Failed to start conversation.", "SpokeText", null);
						return false;
					}

					_character = character == null ? new BasicMisty(_misty, _parameters) : character;

					bool initialized = await _character.Initialize(_characterParameters);
					if (initialized)
					{
						_misty.DisplayText("Hello!", "Text", null);
						_assetWrapper.ShowSystemImage(SystemImage.Joy);
					}
					else
					{
						_misty.DisplayText("Failed to wake up robot!", "Text", null);
						_assetWrapper.ShowSystemImage(SystemImage.Disoriented);
						await Task.Delay(5000);

					}
					await Task.Delay(1000);
					return initialized;
				}
				else
				{

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
					Deleted = true
				}, null);
			}
			return false;
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
				{
					
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
