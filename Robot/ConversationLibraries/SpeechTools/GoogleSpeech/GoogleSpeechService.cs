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
using MistyInteraction.Common;
using Google.Cloud.Speech.V1;
using Google.Cloud.TextToSpeech.V1;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Messengers;
using MistyRobotics.SDK.Responses;
using NAudio.Wave;
using SkillTools.Web;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace SpeechTools.GoogleSpeech
{
	internal class Alternatives
	{
		public string Transcript { get; set; }
		public double Confidence { get; set; }
	}

	internal class GoogleResults
	{
		public IList<Alternatives> Alternatives { get; set; } = new List<Alternatives>();
		public string LanguageCode { get; set; }
	}

	internal class GoogleSpeechRecognitionResults
	{
		public IList<GoogleResults> Results = new List<GoogleResults>();
	}
    
	internal class GoogleTextToSpeechResults
	{
		public string AudioContent { get; set; }
	}
	
	/// <summary>
	/// Wrapper class for Google Speech Service
	/// </summary>
	public sealed class GoogleSpeechService : ISpeechService
	{
		private IRobotMessenger _robot;
		private WebMessenger _googleRecognitionEndpoint = new WebMessenger();
		private WebMessenger _googleTTSEndpoint = new WebMessenger();

		private SemaphoreSlim _speechSemaphore = new SemaphoreSlim(1, 1);
		private GoogleServiceAuthorization _recognitionServicesAuthorization;
		private GoogleServiceAuthorization _ttsServicesAuthorization;

		/// <summary>
		/// Is the service authorized
		/// </summary>
		public bool Authorized { get; set; }

		/// <summary>
		/// The voice misty is using
		/// </summary>
		public string SpeakingVoice { get; set; }
		
		/// <summary>
		/// The voice misty is using
		/// </summary>
		public string SpeakingGender { get; set; }

		/// <summary>
		/// The language Misty is speaking in
		/// </summary>
		public string TranslatedLanguage { get; set; } = "en-US";

		/// <summary>
		/// The language the user is speaking
		/// </summary>
		public string SpokenLanguage { get; set; } = "en-US";

		/// <summary>
		/// Profanity setting
		/// </summary>
		public string ProfanitySetting { get; set; } = "Raw";
 
		public GoogleSpeechService(GoogleServiceAuthorization ttsServicesAuthorization, GoogleServiceAuthorization recognitionServicesAuthorization, IRobotMessenger robot)
		{
			_robot = robot;
			_recognitionServicesAuthorization = recognitionServicesAuthorization;
			_ttsServicesAuthorization = ttsServicesAuthorization;

			if ((_recognitionServicesAuthorization != null && _recognitionServicesAuthorization.SubscriptionKey != null) ||
				((_ttsServicesAuthorization != null && _ttsServicesAuthorization.SubscriptionKey != null)))
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
			_speechSemaphore.Wait();
			try
			{
				
				var arguments = @"{
				'config': {
					'encoding': 'LINEAR16',
					'sampleRateHertz': 16000,
					'languageCode': '" + SpokenLanguage + @"',
					'model': 'phone_call',
					'useEnhanced': true,
					'speechContexts': [{
						'phrases': ['yes', 'no']
					}]
				},
				'audio': {
					'content': '" + Convert.ToBase64String(audioData) + @"'
				}}";

				WebMessengerData googleResponse = await _googleRecognitionEndpoint.PostRequest(_recognitionServicesAuthorization.Endpoint + _recognitionServicesAuthorization.SubscriptionKey, arguments, "application/json");

				//if fails, possibly due to settings of skill, so try through robot
				if (googleResponse.HttpCode == 200)
				{

					GoogleSpeechRecognitionResults results = Newtonsoft.Json.JsonConvert.DeserializeObject<GoogleSpeechRecognitionResults>(googleResponse.Response);
					try
					{
						if (results.Results != null && results.Results.Count > 0 && results.Results.First().Alternatives?.First().Transcript != null)
						{
							return new SpeechToTextData { Translations = null, Text = results.Results?.First().Alternatives?.First().Transcript, Duration = new TimeSpan() };
						}
					}
					catch (Exception ex)
					{
						string message = "Failed processing audio file using Google services.";
						_robot.SkillLogger.Log(message, ex);
					}

					return new SpeechToTextData { Translations = null, Text = "", Duration = new TimeSpan() };

				}
				else
				{
					ISendExternalRequestResponse sdata = await _robot.SendExternalRequestAsync("POST", _recognitionServicesAuthorization.Endpoint + _recognitionServicesAuthorization.SubscriptionKey, "BEARER", _recognitionServicesAuthorization.SubscriptionKey, arguments, false, false, null, "application/json");
					GoogleSpeechRecognitionResults results = Newtonsoft.Json.JsonConvert.DeserializeObject<GoogleSpeechRecognitionResults>(sdata.Data.Data.ToString());
					try
					{
						if (results.Results != null && results.Results.Count > 0 && results.Results.First().Alternatives?.First().Transcript != null)
						{
							return new SpeechToTextData { Translations = null, Text = results.Results?.First().Alternatives?.First().Transcript, Duration = new TimeSpan() };
						}
					}
					catch (Exception ex)
					{
						string message = "Failed processing audio file using Google services.";
						_robot.SkillLogger.Log(message, ex);
					}

					return new SpeechToTextData { Translations = null, Text = "", Duration = new TimeSpan() };
				}
				
			}
			catch (Exception ex)
			{
				string message = "Failed processing audio file using Google REST services.";
				_robot.SkillLogger.Log(message, ex);
				return null;
			}
			finally
			{
				_speechSemaphore.Release();
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
			_speechSemaphore.Wait();
			try
			{
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

				if (file == null)
				{
					string message = $"Could not find file {filename}.";
					_robot.SkillLogger.Log(message);
					return null;
				}
                
				_speechSemaphore.Wait();
				
				string base64String = "";
				IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read);
				var reader = new DataReader(fileStream.GetInputStreamAt(0));
				await reader.LoadAsync((uint)fileStream.Size);
				byte[] byteArray = new byte[fileStream.Size];
				reader.ReadBytes(byteArray);
				base64String = Convert.ToBase64String(byteArray);
					
				var arguments = @"{
				'config': {
					'encoding': 'LINEAR16',
					'sampleRateHertz': 16000,
					'languageCode': '" + SpokenLanguage + @"',
					'model': 'phone_call',
					'useEnhanced': true,
					'speechContexts': [{
						'phrases': ['yes', 'no']
					}]
				},
				'audio': {
					'content': '" + base64String + @"'
				}}";

				WebMessengerData googleResponse = await _googleRecognitionEndpoint.PostRequest(_recognitionServicesAuthorization.Endpoint + _recognitionServicesAuthorization.SubscriptionKey, arguments, "application /json");

				if (googleResponse.HttpCode == 200)
				{
					GoogleSpeechRecognitionResults results = Newtonsoft.Json.JsonConvert.DeserializeObject<GoogleSpeechRecognitionResults>(googleResponse.Response);

					try
					{
						if (results.Results != null && results.Results.Count > 0 && results.Results.First().Alternatives?.First().Transcript != null)
						{
							return new SpeechToTextData { Translations = null, Text = results.Results?.First().Alternatives?.First().Transcript, Duration = new TimeSpan() };
						}
					}
					catch (Exception ex)
					{
						string message = "Failed processing audio file using Google services.";
						_robot.SkillLogger.Log(message, ex);
					}

					return new SpeechToTextData { Translations = null, Text = "", Duration = new TimeSpan() };
				}
				else
				{
					ISendExternalRequestResponse sdata = await _robot.SendExternalRequestAsync("POST", _recognitionServicesAuthorization.Endpoint + _recognitionServicesAuthorization.SubscriptionKey, "BEARER", _recognitionServicesAuthorization.SubscriptionKey, arguments, false, false, null, "application/json");
					GoogleSpeechRecognitionResults results = Newtonsoft.Json.JsonConvert.DeserializeObject<GoogleSpeechRecognitionResults>(sdata.Data.Data.ToString());

					try
					{
						if (results.Results != null && results.Results.Count > 0 && results.Results.First().Alternatives?.First().Transcript != null)
						{
							return new SpeechToTextData { Translations = null, Text = results.Results?.First().Alternatives?.First().Transcript, Duration = new TimeSpan() };
						}
					}
					catch (Exception ex)
					{
						string message = "Failed processing audio file using Google services.";
						_robot.SkillLogger.Log(message, ex);
					}

					return new SpeechToTextData { Translations = null, Text = "", Duration = new TimeSpan() };
				}
			}
			catch (Exception ex)
			{
				string message = "Failed processing audio file using Google REST services.";
				_robot.SkillLogger.Log(message, ex);
				return null;
			}
			finally
			{
				_speechSemaphore.Release();
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
			_speechSemaphore.Wait();
			try
			{
				if(string.IsNullOrWhiteSpace(text))
				{
					return null;
				}

				var arguments = @"{
					'input':{
					'text': '" + text +
					@"'},
					'voice':{
					'languageCode':'" + SpokenLanguage + @"',
					'name':'" + SpeakingVoice + @"',
					'ssmlGender':'" + SpeakingGender + @"',
					},
					'audioConfig':{
					'audioEncoding':'LINEAR16'
					}
				}";

				WebMessengerData googleResponse = await _googleTTSEndpoint.PostRequest(_ttsServicesAuthorization.Endpoint + _ttsServicesAuthorization.SubscriptionKey, arguments, "application/json");

				//if fails, possibly due to settings of skill, so try through robot
				if (googleResponse.HttpCode == 200)
				{

					GoogleTextToSpeechResults results = Newtonsoft.Json.JsonConvert.DeserializeObject<GoogleTextToSpeechResults>(googleResponse.Response);
					try
					{
						if (results != null && !string.IsNullOrWhiteSpace(results.AudioContent))
						{
							return new TextToSpeechData { AudioData = Convert.FromBase64String(results.AudioContent) };
						}
					}
					catch (Exception ex)
					{
						string message = "Failed processing Google text to speech..";
						_robot.SkillLogger.Log(message, ex);
					}

					return new TextToSpeechData { AudioData = null };

				}
				else
				{
					ISendExternalRequestResponse sdata = await _robot.SendExternalRequestAsync("POST", _ttsServicesAuthorization.Endpoint + _ttsServicesAuthorization.SubscriptionKey, "BEARER", _ttsServicesAuthorization.SubscriptionKey, arguments, false, false, null, "application/json");
					GoogleTextToSpeechResults results = Newtonsoft.Json.JsonConvert.DeserializeObject<GoogleTextToSpeechResults>(sdata.Data.Data.ToString());
					try
					{
						if (results != null && !string.IsNullOrWhiteSpace(results.AudioContent))
						{
							return new TextToSpeechData { AudioData = Convert.FromBase64String(results.AudioContent) };
						}
					}
					catch (Exception ex)
					{
						string message = "Failed processing Google REST text to speech..";
						_robot.SkillLogger.Log(message, ex);
					}

					return new TextToSpeechData { AudioData = null };
				}
			}
			catch (Exception ex)
			{
				string message = "Failed processing Google text to speech.";
				_robot.SkillLogger.Log(message, ex);
				return null;
			}
			finally
			{
				_speechSemaphore.Release();
			}
		}
		
		public IAsyncOperation<bool> Speak(string text, string fileName, bool useSSML, int trimAudioSilenceMs)
		{
			return SpeakInternal(text, fileName, useSSML, trimAudioSilenceMs).AsAsyncOperation();
		}
		
		/// <summary>
		/// Tell the robot to speak using google tts at the current volume
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
		/// Tell the robot to speak using google tts
		/// </summary>
		/// <param name="text"></param>
		/// <param name="fileName"></param>
		/// <param name="overrideVolume"></param>
		private async Task<bool> SpeakInternal(string text, string fileName, int overrideVolume, bool useSSML, int trimAudioSilenceMs)
		{
			if(!string.IsNullOrWhiteSpace(fileName))
			{
				TextToSpeechData response = null;
				
				var arguments = @"{
					'input':{
					'text': '" + text +
					@"'},
					'voice':{
					'languageCode':'" + SpokenLanguage + @"',
					'name':'" + SpeakingVoice + @"',
					'ssmlGender':'" + SpeakingGender + @"',
					},
					'audioConfig':{
					'audioEncoding':'LINEAR16'
					}
				}";
				
				WebMessengerData googleResponse = await _googleTTSEndpoint.PostRequest(_ttsServicesAuthorization.Endpoint + _ttsServicesAuthorization.SubscriptionKey, arguments, "application/json");

				if (googleResponse.HttpCode == 200)
				{
					GoogleTextToSpeechResults results = Newtonsoft.Json.JsonConvert.DeserializeObject<GoogleTextToSpeechResults>(googleResponse.Response);
					try
					{
						if (results != null && !string.IsNullOrWhiteSpace(results.AudioContent))
						{
							response = new TextToSpeechData { AudioData = Convert.FromBase64String(results.AudioContent) };
						}
					}
					catch (Exception ex)
					{
						string message = "Failed processing Google text to speech..";
						_robot.SkillLogger.Log(message, ex);
						return false;
					}
				}
				else
				{
					ISendExternalRequestResponse sdata = await _robot.SendExternalRequestAsync("POST", _ttsServicesAuthorization.Endpoint + _ttsServicesAuthorization.SubscriptionKey, "BEARER", _ttsServicesAuthorization.SubscriptionKey, arguments, false, false, null, "application/json");
					GoogleTextToSpeechResults results = Newtonsoft.Json.JsonConvert.DeserializeObject<GoogleTextToSpeechResults>(sdata.Data.Data.ToString());
					try
					{
						if (results != null && !string.IsNullOrWhiteSpace(results.AudioContent))
						{
							response = new TextToSpeechData { AudioData = Convert.FromBase64String(results.AudioContent) };
						}
					}
					catch (Exception ex)
					{
						string message = "Failed processing Google REST text to speech..";
						_robot.SkillLogger.Log(message, ex);
						return false;
					}
				}

				try
				{
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
					return true;
				}
				catch (Exception ex)
				{
					string message = "Failed processing or trimming Google audio file.";
					_robot.SkillLogger.Log(message, ex);
				}

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

				return audioFileOutStream == null ? null : audioFileOutStream.ToArray();
			}
			catch
			{
				return null;
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
 
 