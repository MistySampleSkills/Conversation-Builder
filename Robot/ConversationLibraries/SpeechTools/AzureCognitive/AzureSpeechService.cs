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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Conversation.Common;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Messengers;
using MistyRobotics.SDK.Responses;
using NAudio.Wave;
using Windows.Foundation;
using Windows.Storage;

namespace SpeechTools.AzureCognitive
{
	/// <summary>
	/// Wrapper class for Azure Cognitive Speech Service
	/// </summary>
	public sealed class AzureSpeechService : ISpeechService
	{
		private IRobotMessenger _robot;
		private SpeechConfig _ttsSpeechConfig;
		private SpeechTranslationConfig _speechTranslationConfig;
		private readonly SemaphoreSlim _speechSemaphore = new SemaphoreSlim(1, 1);
		private AzureServiceAuthorization _ttsAuthorization;
		private AzureServiceAuthorization _recognitionAuthorization;
		
		private string _azureSpeechProfanitySetting = "Raw";
		private string _spokenLanguage = "en-US";
		private string _translatedLanguage = "en";
		private string _speakingvoice = "en-US-AriaNeural";
		
		/// <summary>
		/// Is the service authorized
		/// </summary>
		public bool Authorized { get; private set; }

		/// <summary>
		/// The voice misty is using
		/// </summary>
		public string SpeakingVoice
		{
			get
			{
				return _speakingvoice;
			}
			set
			{
				_speakingvoice = value;
				_ttsSpeechConfig.SpeechSynthesisVoiceName = _speakingvoice;
				_speechTranslationConfig.SpeechSynthesisVoiceName = _speakingvoice;
			}
		}

		/// <summary>
		/// The language Misty is speaking in
		/// </summary>
		public string TranslatedLanguage
		{
			get
			{
				return _translatedLanguage;
			}
			set
			{
				_translatedLanguage = value;
				_ttsSpeechConfig.SpeechSynthesisLanguage = _translatedLanguage;
				_speechTranslationConfig.SpeechSynthesisLanguage = _translatedLanguage;
			}
		}
		
		/// <summary>
		/// The language the user is speaking
		/// </summary>
		public string SpokenLanguage
		{
			get
			{
				return _spokenLanguage;
			}
			set
			{
				_spokenLanguage = value;
				_speechTranslationConfig.SpeechRecognitionLanguage = _spokenLanguage;
				_ttsSpeechConfig.SpeechRecognitionLanguage = _spokenLanguage;
			}
		}

		/// <summary>
		/// Profanity setting
		/// </summary>
		public string ProfanitySetting
		{
			get
			{
				return _azureSpeechProfanitySetting;
			}
			set
			{
				_azureSpeechProfanitySetting = value;
				switch (_azureSpeechProfanitySetting)
				{
					case "Removed":
						_speechTranslationConfig.SetProfanity(ProfanityOption.Removed);
						_ttsSpeechConfig.SetProfanity(ProfanityOption.Removed);
						break;
					case "Masked":
						_speechTranslationConfig.SetProfanity(ProfanityOption.Masked);
						_ttsSpeechConfig.SetProfanity(ProfanityOption.Masked);
						break;
					case "Raw":
					default:
						_speechTranslationConfig.SetProfanity(ProfanityOption.Raw);
						_ttsSpeechConfig.SetProfanity(ProfanityOption.Raw);
						break;
				}
			}
		}

		public AzureSpeechService(AzureServiceAuthorization ttsAuthorization, AzureServiceAuthorization recognitionAuthorization, IRobotMessenger robot)
		{
			_robot = robot;
			_ttsAuthorization = ttsAuthorization;
			_recognitionAuthorization = recognitionAuthorization;

			_ttsSpeechConfig = SpeechConfig.FromSubscription(_ttsAuthorization.SubscriptionKey, _ttsAuthorization.Region);
			_speechTranslationConfig = SpeechTranslationConfig.FromSubscription(_recognitionAuthorization.SubscriptionKey, _recognitionAuthorization.Region);
			
			if (_ttsSpeechConfig != null || _speechTranslationConfig != null)
			{
				Authorized = true;
			}
		}

		/// <summary>
		/// Using an audio stream, get the translation of that audio file
		/// </summary>
		/// <param name="audioData"></param>
		/// <returns></returns>
		public IAsyncOperation<SpeechToTextData> TranslateAudioStream(IEnumerable<byte> audioData)
		{
			return TranslateAudioStreamInternal(audioData.ToArray()).AsAsyncOperation();
		}
		
		private async Task<SpeechToTextData> TranslateAudioStreamInternal(byte[] audioData)
		{
			//_speechSemaphore.Wait();
			StorageFile storageFile = null;
			try
			{
				if(audioData == null || audioData.Count() == 0)
				{
					return null;
				}

				TranslationRecognitionResult result;
				StorageFolder localFolder = ApplicationData.Current.LocalFolder;

				//TODO Update to use PullAudioInputStream
				string newAudio = Guid.NewGuid().ToString();
				storageFile = await localFolder.CreateFileAsync($"{newAudio}.wav", CreationCollisionOption.ReplaceExisting);
				using (var stream = await storageFile.OpenStreamForWriteAsync())
				{
					await stream.WriteAsync(audioData, 0, audioData.Count());
					stream.Close();
				}

				var audioConfig = AudioConfig.FromWavFileInput(storageFile.Path);
				_speechTranslationConfig.AddTargetLanguage(_translatedLanguage);
			
				using (var translationRecognizer = new TranslationRecognizer(_speechTranslationConfig, audioConfig))
				{
					result = await translationRecognizer.RecognizeOnceAsync();
				}

				if (result.Reason == ResultReason.Canceled)
				{
					var cancellation = CancellationDetails.FromResult(result);
					_robot.SkillLogger.LogWarning($"Call cancelled.  {cancellation.Reason}");

					if (cancellation.Reason == CancellationReason.Error)
					{
						_robot.SkillLogger.Log($"Cancel error code = {cancellation.ErrorCode}");
						_robot.SkillLogger.Log($"Cancel details = {cancellation.ErrorDetails}");

						if (cancellation.ErrorCode == CancellationErrorCode.NoError || cancellation.ErrorCode == CancellationErrorCode.AuthenticationFailure)
						{
							_robot.SkillLogger.Log("You may be having an authorization issue, are your keys correct and up to date?");
						}
					}
				}
				else
				{
					_robot.SkillLogger.LogInfo($"Azure Translation. '{result.Reason}': {result.Text}");
				}

				IDictionary<string, string> translations = result.Translations.ToDictionary(x => x.Key, y => y.Value);				
				return new SpeechToTextData { Translations = translations, Text = result.Text, Duration = result.Duration };
			}
			catch (Exception ex)
			{
				string message = "Failed processing Azure audio stream - overlap.";
				_robot.SkillLogger.Log(message, ex);
				return new SpeechToTextData { Translations = new Dictionary<string, string>(), Text = "", Duration = new TimeSpan(0,0,0)};
			}
			finally
			{
				//_speechSemaphore.Release();
				if (storageFile != null)
				{
					try
					{
						_ = storageFile.DeleteAsync();
					}
					catch
					{

					}
				}
			}
		}

		/// <summary>
		/// Using an audio file in either the LocalFolder (or the Assets folder for testing)
		/// get the translation of that audio file
		/// </summary>
		/// <param name="filename">The file name translate</param>
		/// <param name="useAssetFolder">If null will look for the asset in Assets folder, otherwise, will look in local folder </param>
		/// <returns></returns>
		public IAsyncOperation<SpeechToTextData> TranslateAudioFile(string filename, bool useAssetFolder)
		{
			return TranslateAudioFileInternal(filename, useAssetFolder).AsAsyncOperation();
		}

		private  async Task<SpeechToTextData> TranslateAudioFileInternal(string filename, bool useAssetFolder = false)
		{
			//_speechSemaphore.Wait();
			try
			{
				TranslationRecognitionResult result;

				StorageFolder localFolder;
				if (!useAssetFolder)
				{
					localFolder = ApplicationData.Current.LocalFolder;
				}
				else
				{
					localFolder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("Assets");
				}

				StorageFile file = await localFolder.GetFileAsync(filename);

				var audioConfig = AudioConfig.FromWavFileInput(file.Path);
				_speechTranslationConfig.AddTargetLanguage(_translatedLanguage);
				
				using (var translationRecognizer = new TranslationRecognizer(_speechTranslationConfig, audioConfig))
				{
					result = await translationRecognizer.RecognizeOnceAsync();
				}
				
				if (result.Reason == ResultReason.Canceled)
				{
					var cancellation = CancellationDetails.FromResult(result);
					_robot.SkillLogger.LogWarning($"Call cancelled.  {cancellation.Reason}");

					if (cancellation.Reason == CancellationReason.Error)
					{
						_robot.SkillLogger.Log($"Cancel error code = {cancellation.ErrorCode}");
						_robot.SkillLogger.Log($"Cancel details = {cancellation.ErrorDetails}");

						if (cancellation.ErrorCode == CancellationErrorCode.NoError)
						{
							_robot.SkillLogger.Log("You may be having an authorization issue, are your keys correct and up to date?");

						}
					}
				}
				else if (result.Reason == ResultReason.TranslatedSpeech)
				{
					_robot.SkillLogger.LogInfo($"Azure Translation. '{result.Reason}': {result.Text}");
				}

				IDictionary<string, string> translations = result.Translations.ToDictionary(x => x.Key, y => y.Value);
				return new SpeechToTextData { Translations = translations, Text = result.Text, Duration = result.Duration };
			}
			catch (Exception ex)
			{
				string message = "Failed processing Azure audio file.";
				_robot.SkillLogger.Log(message, ex);
				return new SpeechToTextData { Translations = new Dictionary<string, string>(), Text = "", Duration = new TimeSpan(0, 0, 0) };
			}
			finally
			{
		//		_speechSemaphore.Release();
			}
		}

		/// <summary>
		/// Return an audio file for the passed in text
		/// </summary>
		/// <param name="text"></param>
		/// <param name="useSSML"></param>
		/// <returns></returns>
		public IAsyncOperation<TextToSpeechData> TextToSpeechFile(string text, bool useSSML)
		{
			return TextToSpeechFileInternal(text, useSSML).AsAsyncOperation();
		}

		/// <summary>
		/// Return an audio file for the passed in text
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public IAsyncOperation<TextToSpeechData> TextToSpeechFile(string text)
		{
			return TextToSpeechFileInternal(text, false).AsAsyncOperation();
		}

		private async Task<TextToSpeechData> TextToSpeechFileInternal(string text, bool useSSML)
		{
			//_speechSemaphore.Wait();
			StorageFile storageFile = null;
			try
			{
				if(string.IsNullOrWhiteSpace(text))
				{
					return null;
				}

				StorageFolder localFolder = ApplicationData.Current.LocalFolder;

				//TODO Update to use PullAudioInputStream

				string newAudio = Guid.NewGuid().ToString();
				storageFile = await localFolder.CreateFileAsync($"{newAudio}.wav", CreationCollisionOption.ReplaceExisting);
				
				using (var fileOutput = AudioConfig.FromWavFileOutput(storageFile.Path))
				{
					using (var synthesizer = new SpeechSynthesizer(_ttsSpeechConfig, fileOutput))
					{
						SpeechSynthesisResult result = null;
						if(useSSML)
						{
							result = await synthesizer.SpeakSsmlAsync(text);
						}
						else
						{
							result = await synthesizer.SpeakTextAsync(text);
						}

						if (result.Reason == ResultReason.Canceled)
						{
							var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
							_robot.SkillLogger.LogWarning($"Call cancelled.  {cancellation.Reason}");

							if (cancellation.Reason == CancellationReason.Error)
							{
								_robot.SkillLogger.Log($"Cancel error code = {cancellation.ErrorCode}");
								_robot.SkillLogger.Log($"Cancel details = {cancellation.ErrorDetails}");

								if (cancellation.ErrorCode == CancellationErrorCode.NoError)
								{
									_robot.SkillLogger.Log("You may be having an authorization issue, are your keys correct and up to date?");
								}
							}
							return null;
						}

						_robot.SkillLogger.LogInfo($"Audio Received. '{result.Reason}'");	
						
						//TODO Text to audio translate in cognitive?
						/*if(_speechConfig.SpeechSynthesisLanguage != "en" || _speechConfig.SpeechSynthesisLanguage != "en-US")
						{
							SpeechToTextData newData = await TranslateAudioStream(result.AudioData);

							//Get the translations and play those now in tts
						}*/
						return new TextToSpeechData { AudioData = result.AudioData };
					}
				}
			}
			catch (Exception ex)
			{
				string message = "Failed processing Azure text to speech.";
				_robot.SkillLogger.Log(message, ex);
				return new TextToSpeechData { AudioData = new byte[0]};
			}
			finally
			{
				if (storageFile != null)
				{
					_ = storageFile.DeleteAsync();
				}
			}
		}


		public IAsyncOperation<bool> Speak(string text, string fileName, bool useSSML, int trimAudioSilenceMs)
		{
			return SpeakInternal(text, fileName, useSSML, trimAudioSilenceMs).AsAsyncOperation();
		}
		
		/// <summary>
		/// Tell the robot to speak using azure cognitive tts at the current volume
		/// </summary>
		/// <param name="text"></param>
		/// <param name="fileName"></param>
		private Task<bool> SpeakInternal(string text, string fileName, bool useSSML, int trimAudioSilenceMs)
		{
			return SpeakInternal(text, fileName, -1, useSSML, trimAudioSilenceMs);
		}

		public IAsyncOperation<bool> Speak(string text, string fileName, int overrideVolume, bool useSSML, int trimAudioSilenceMs)
		{
			return SpeakInternal(text, fileName, overrideVolume, useSSML, trimAudioSilenceMs).AsAsyncOperation();
		}


		/// <summary>
		/// Tell the robot to speak using azure cognitive tts
		/// </summary>
		/// <param name="text"></param>
		/// <param name="fileName"></param>
		/// <param name="overrideVolume"></param>
		private async Task<bool> SpeakInternal(string text, string fileName, int overrideVolume, bool useSSML, int trimAudioSilenceMs)
		{
			if(!string.IsNullOrWhiteSpace(fileName))
			{
				//TODO Update this, overly complicated				
				if (_speechTranslationConfig.SpeechSynthesisLanguage.ToLower() == "en" || _speechTranslationConfig.SpeechSynthesisLanguage.ToLower() == "en-us")
				{
					TextToSpeechData response = await TextToSpeechFileInternal(text, useSSML);

					if (response?.AudioData != null)
					{
						string fullName = AssetHelper.AddMissingWavExtension(fileName);

						if (trimAudioSilenceMs > 0)
						{
							byte[] trimmedFile = GetTrimmedWavFile(response.AudioData, new TimeSpan(0, 0, 0, 0, trimAudioSilenceMs));

							if (trimmedFile != null)
							{
								response.AudioData = trimmedFile;
							}
						}

						IRobotCommandResponse result = null;
						if (overrideVolume < 0)
						{
							result = await _robot.SaveAudioAsync(fullName, response.AudioData, true, true);
						}
						else
						{
							result = await _robot.SaveAudioAsync(fullName, response.AudioData, false, true);
							if (result != null && result.Status == ResponseStatus.Success)
							{
								_robot.PlayAudio(fullName, overrideVolume, null);
							}
						}

						if(result == null)
						{
							_robot.SkillLogger.LogWarning($"Received null response from Save Audio call.");
						}
						else if (result.Status != ResponseStatus.Success)
						{
							_robot.SkillLogger.LogWarning($"Save audio call failed.  Attempting to re-enable audio service.");

							IRobotCommandResponse enableResponse = await _robot.EnableAudioServiceAsync();
							if (enableResponse.Status != ResponseStatus.Success)
							{
								_robot.SkillLogger.LogWarning($"Failed to re-enable audio service.");
							}
						}
					}
				}
				else
				{
					//TODO Fix this, too many calls - Update to: Text to text, then audio?  Need to add Cognitive.Translator

					TextToSpeechData response = await TextToSpeechFileInternal(text, useSSML);					
					//take audio and retranslate to foreign text... 
					SpeechToTextData data = await TranslateAudioStream(response.AudioData);                    
					TextToSpeechData response2 = await TextToSpeechFileInternal(data.Translations.First().Value, useSSML);

					if (response2?.AudioData != null)
					{
						string fullName = AssetHelper.AddMissingWavExtension(fileName);
						
						if (trimAudioSilenceMs > 0)
						{
							byte[] trimmedFile = GetTrimmedWavFile(response2.AudioData, new TimeSpan(0, 0, 0, 0, trimAudioSilenceMs));
							
							if (trimmedFile != null)
							{
								response.AudioData = trimmedFile;
							}
						}

						IRobotCommandResponse result = null;
						if (overrideVolume < 0)
						{
							result = await _robot.SaveAudioAsync(fullName, response2.AudioData, true, true);
						}
						else
						{
							result = await _robot.SaveAudioAsync(fullName, response2.AudioData, false, true);
							if(result != null && result.Status == ResponseStatus.Success)
							{
								_robot.PlayAudio(fullName, overrideVolume, null);
							}
						}

						if (result == null)
						{
							_robot.SkillLogger.LogWarning($"Received null response from Save Audio call.");
						}
						else if (result.Status != ResponseStatus.Success)
						{
							_robot.SkillLogger.LogWarning($"Save audio call failed.  Attempting to re-enable audio service.");

							IRobotCommandResponse enableResponse = await _robot.EnableAudioServiceAsync();
							if (enableResponse.Status != ResponseStatus.Success)
							{
								_robot.SkillLogger.LogWarning($"Failed to re-enable audio service.");
							}
						}
					}
				}
				return true;
			}
			return false;
		}

		private byte[] GetTrimmedWavFile(byte[] data, TimeSpan endCutTimeSpan)
		{
			try
			{
				MemoryStream audioFileOutStream = new MemoryStream();

				MemoryStream audioFileInStream = new MemoryStream(data);

				using (WaveFileReader reader = new WaveFileReader(audioFileInStream))
				{
					using (WaveFileWriter writer = new WaveFileWriter(audioFileOutStream, reader.WaveFormat))
					{
						int bytesPerMillisecond = reader.WaveFormat.AverageBytesPerSecond / 1000;
						
						int endBytes = (int)endCutTimeSpan.TotalMilliseconds * bytesPerMillisecond;
						endBytes = endBytes - endBytes % reader.WaveFormat.BlockAlign;
						int endPos = (int)reader.Length - endBytes;
						
						TrimWavFile(reader, writer, 0, endPos);
					}
				}

				return audioFileOutStream?.ToArray();
			}
			catch
			{
				return data;
			}
		}
		
		private static void TrimWavFile(WaveFileReader reader, WaveFileWriter writer, int startPos, int endPos)
		{
			reader.Position = startPos;
			byte[] buffer = new byte[endPos- startPos+1];
			while (reader.Position < endPos)
			{
				int bytesRequired = (int)(endPos - reader.Position);
				if (bytesRequired > 0)
				{
					int bytesToRead = Math.Min(bytesRequired, buffer.Length);
					int bytesRead = reader.Read(buffer, 0, bytesToRead);
					if (bytesRead > 0)
					{
						writer.Write(buffer, 0, bytesRead);
					}
				}
			}
		}
	}
}