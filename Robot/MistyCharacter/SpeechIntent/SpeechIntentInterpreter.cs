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
using System.Linq;
using Conversation.Common;

namespace MistyCharacter.SpeechIntent
{	
	internal class SpeechIntentInterpreter
	{
		private IDictionary<string, KeyValuePair<bool, IList<string>>> _utteranceLists { get; set; }
		private IList<GenericDataStore> _userData;
		public SpeechIntentInterpreter(IDictionary<string, KeyValuePair<bool, IList<string>>> utteranceLists, IList<GenericDataStore> userData = null)
		{
			_utteranceLists = utteranceLists ?? new Dictionary<string, KeyValuePair<bool, IList<string>>>();
			_userData = userData ?? new List<GenericDataStore>();
		}
        
		public GenericData FindUserDataFromText(string name, string text)
		{
			try
			{
				GenericData genericData = new GenericData();
				if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(text) || _userData == null || !_userData.Any())
				{
					genericData.Key = text;
					genericData.Value = "";
					return genericData;
				}

				IList<KeyValuePair<string, KeyValuePair<bool, IList<string>>>> filteredUtteranceLists = new List<KeyValuePair<string, KeyValuePair<bool, IList<string>>>>();
				GenericDataStore genericDataStore = _userData.FirstOrDefault(x => x.Name == name);
				if (genericDataStore?.Name != null)
				{
					foreach(KeyValuePair<string, GenericData> data in genericDataStore.Data)
					{
						IList<string> utterances = data.Value.Key.Split(',').ToList();
						filteredUtteranceLists.Add(new KeyValuePair<string, KeyValuePair<bool, IList<string>>>(data.Value.Key, new KeyValuePair<bool, IList<string>>(genericDataStore.ExactMatchesOnly, utterances)));
					}					
				}

				string userDataKey = GetIntentExperimentThree(text, filteredUtteranceLists);
				KeyValuePair<string, GenericData> returnData = genericDataStore.Data.FirstOrDefault(x => x.Value.Key == userDataKey);
				if(returnData.Value != null)
				{
					genericData = returnData.Value;
				}
				return genericData;
			}
			catch
			{
				GenericData genericData = new GenericData();
				genericData.Key = text;
				genericData.Value = "";
				return genericData;
			}
		}
		
		/// <summary>
		/// If null or empty intent options passed in, compares against all of them
		/// </summary>
		/// <param name="text"></param>
		/// <param name="intentOptions"></param>
		/// <returns></returns>
		public string GetIntent(string text, IList<string> intentOptions = null)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(text))
				{
					return ConversationConstants.HeardNothingTrigger;
				}

				IEnumerable<KeyValuePair<string, KeyValuePair<bool, IList<string>>>> filteredUtteranceLists = null;
				intentOptions = intentOptions ?? new List<string>();
				if(intentOptions == null || intentOptions.Count() == 0)
				{
					filteredUtteranceLists = _utteranceLists;
				}
				else
				{
					filteredUtteranceLists = _utteranceLists.Where(x => intentOptions.Contains(x.Key));
				}

				return GetIntentExperimentThree(text, filteredUtteranceLists.ToList());
			}
			catch
			{
				return ConversationConstants.HeardUnknownTrigger;
			}
		}
		
		private string GetIntentExperimentOne(string text, IList<KeyValuePair<string, KeyValuePair<bool, IList<string>>>> filteredUtteranceLists)
		{
			try
			{
				IList<TextComparisonObject> textComparisonObjects = new List<TextComparisonObject>();

				//In each Voice Intent Group
				foreach (KeyValuePair<string, KeyValuePair<bool, IList<string>>> utteranceList in filteredUtteranceLists)
				{
					string currentIntent = utteranceList.Key;
					bool exactMatchOnly = utteranceList.Value.Key;
					TextComparisonObject textComparisonObject = new TextComparisonObject { Intent = currentIntent, HitCountAverage = 0, MaxHitCount = 0 };

					foreach (string utterance in utteranceList.Value.Value)
					{
						//For each utterance string...
						int currentHitCount = 0;

						//if an exact match, we are done
						if (utterance.Trim().ToLower() == text.Trim().ToLower())
						{
							return currentIntent;
						}

						string adjustedText = text.
							Replace("?", " ", StringComparison.OrdinalIgnoreCase).
							Replace(".", " ", StringComparison.OrdinalIgnoreCase).
							Replace(",", " ", StringComparison.OrdinalIgnoreCase).
							Replace("!", " ", StringComparison.OrdinalIgnoreCase).
							Trim().ToLower();

						//if an exact match without punctuation, call it good
						if (utterance.Trim().ToLower().
							Replace("?", " ", StringComparison.OrdinalIgnoreCase).
							Replace(".", " ", StringComparison.OrdinalIgnoreCase).
							Replace(",", " ", StringComparison.OrdinalIgnoreCase).
							Replace("!", " ", StringComparison.OrdinalIgnoreCase) == adjustedText)
						{
							return currentIntent;
						}

						if (exactMatchOnly)
						{
							return ConversationConstants.HeardUnknownTrigger;
						}

						//else check by word count for this utterance
						string[] words = adjustedText.Split(" ");
						IList<string> utteranceWords = utterance.Trim().ToLower().Split(" ").ToList();

						foreach (string word in words)
						{
							if (string.IsNullOrWhiteSpace(word))
							{
								continue;
							}
							string updatedWord = word.Trim().ToLower();
							if (updatedWord == "the" || updatedWord == "an" || updatedWord == "a" || updatedWord == "um" || updatedWord == "uh")
							{
								continue;
							}

							if (utteranceWords.Contains(updatedWord))
							{
								currentHitCount++;
							}
						}

						if (currentHitCount > 0)
						{
							if (currentHitCount > textComparisonObject.MaxHitCount)
							{
								textComparisonObject.MaxHitCount = currentHitCount;
								textComparisonObject.HitCountAverage = currentHitCount / words.Length;
							}
							if (currentHitCount == textComparisonObject.MaxHitCount)
							{
								if (currentHitCount / words.Length > textComparisonObject.MaxHitCount / textComparisonObject.HitCountAverage)
								{
									//same matches, less words
									textComparisonObject.HitCountAverage = currentHitCount / words.Length;
								}
							}
						}

						textComparisonObjects.Add(textComparisonObject);
					}
				}

				if (textComparisonObjects.Count > 0)
				{
					//Checked them all, now get the intent
					int maxHitCount = textComparisonObjects.Max(x => x.MaxHitCount);
					if (maxHitCount > 0)
					{
						return textComparisonObjects.Where(x => x.MaxHitCount == maxHitCount).OrderByDescending(x => x.HitCountAverage).First().Intent;
					}
				}
				return ConversationConstants.HeardUnknownTrigger;
			}
			catch
			{
				return ConversationConstants.HeardUnknownTrigger;
			}
		}

		private string GetIntentExperimentTwo(string text, IList<KeyValuePair<string, KeyValuePair<bool, IList<string>>>> filteredUtteranceLists)
		{
			try
			{
				IList<TextComparisonObject> textComparisonObjects = new List<TextComparisonObject>();

				///In each Voice Intent Group
				foreach (KeyValuePair<string, KeyValuePair<bool, IList<string>>> utteranceList in filteredUtteranceLists)
				{
					string currentIntent = utteranceList.Key;
					bool exactMatchOnly = utteranceList.Value.Key;
					TextComparisonObject textComparisonObject = new TextComparisonObject { Intent = currentIntent, HitCountAverage = 0, MaxHitCount = 0 };

					foreach (string utterance in utteranceList.Value.Value)
					{
						//For each utterance string...
						int currentHitCount = 0;

						//if an exact match, we are done
						if (utterance.Trim().ToLower() == text.Trim().ToLower())
						{
							return currentIntent;
						}

						string adjustedText = text.
							Replace("?", " ", StringComparison.OrdinalIgnoreCase).
							Replace(".", " ", StringComparison.OrdinalIgnoreCase).
							Replace(",", " ", StringComparison.OrdinalIgnoreCase).
							Replace("!", " ", StringComparison.OrdinalIgnoreCase).
							Trim().ToLower();

						//if an exact match without punctuation, call it good
						if (utterance.Trim().ToLower().
							Replace("?", " ", StringComparison.OrdinalIgnoreCase).
							Replace(".", " ", StringComparison.OrdinalIgnoreCase).
							Replace(",", " ", StringComparison.OrdinalIgnoreCase).
							Replace("!", " ", StringComparison.OrdinalIgnoreCase) == adjustedText)
						{
							return currentIntent;
						}

						if (exactMatchOnly)
						{
							return ConversationConstants.HeardUnknownTrigger;
						}

						//else check by word count for this utterance
						string[] spokenWords = adjustedText.Split(" ");
						IList<string> utteranceWords = utterance.Trim().ToLower().Split(" ").ToList();

						foreach (string spokenWord in spokenWords)
						{
							if (string.IsNullOrWhiteSpace(spokenWord))
							{
								continue;
							}
							string updatedWord = spokenWord.Trim().ToLower();
							if (updatedWord == "the" || updatedWord == "an" || updatedWord == "a" || updatedWord == "um" || updatedWord == "uh")
							{
								continue;
							}

							if (utteranceWords.Contains(updatedWord))
							{
								currentHitCount++;
							}
							else
							{
								foreach(string utteranceWord in utteranceWords)
								{
									if (spokenWord.Contains(utteranceWord))
									{
										currentHitCount++;
										break;
									}
								}
							}
						}

						if (currentHitCount > 0)
						{
							if (currentHitCount > textComparisonObject.MaxHitCount)
							{
								textComparisonObject.MaxHitCount = currentHitCount;
								textComparisonObject.HitCountAverage = currentHitCount / spokenWords.Length;
							}
							if (currentHitCount == textComparisonObject.MaxHitCount)
							{
								if (currentHitCount / spokenWords.Length > textComparisonObject.MaxHitCount / textComparisonObject.HitCountAverage)
								{
									//same matches, less words
									textComparisonObject.HitCountAverage = currentHitCount / spokenWords.Length;
								}
							}
						}

						textComparisonObjects.Add(textComparisonObject);
					}
				}

				if (textComparisonObjects.Count > 0)
				{
					//Checked them all, now get the intent
					int maxHitCount = textComparisonObjects.Max(x => x.MaxHitCount);
					if (maxHitCount > 0)
					{
						return textComparisonObjects.Where(x => x.MaxHitCount == maxHitCount).OrderByDescending(x => x.HitCountAverage).First().Intent;
					}
				}
				return ConversationConstants.HeardUnknownTrigger;
			}
			catch
			{
				return ConversationConstants.HeardUnknownTrigger;
			}
		}

		private string GetIntentExperimentThree(string text, IList<KeyValuePair<string, KeyValuePair<bool, IList<string>>>> filteredUtteranceLists)
		{
			try
			{
				IList<TextComparisonObject> textComparisonObjects = new List<TextComparisonObject>();

				///In each Voice Intent Group
				foreach (KeyValuePair<string, KeyValuePair<bool, IList<string>>> utteranceList in filteredUtteranceLists)
				{
					string currentIntent = utteranceList.Key;
					bool exactMatchOnly = utteranceList.Value.Key;
					TextComparisonObject textComparisonObject = new TextComparisonObject { Intent = currentIntent, HitCountAverage = 0, MaxHitCount = 0 };

					foreach (string utterance in utteranceList.Value.Value)
					{
						//For each utterance string...
						int currentHitCount = 0;

						//if an exact match, we are done
						if (utterance.Trim().ToLower() == text.Trim().ToLower())
						{
							return currentIntent;
						}

						string adjustedText = text.
							Replace("?", " ", StringComparison.OrdinalIgnoreCase).
							Replace(".", " ", StringComparison.OrdinalIgnoreCase).
							Replace(",", " ", StringComparison.OrdinalIgnoreCase).
							Replace("!", " ", StringComparison.OrdinalIgnoreCase).
							Trim().ToLower();

						//if an exact match without punctuation, call it good
						if (utterance.Trim().ToLower().
							Replace("?", " ", StringComparison.OrdinalIgnoreCase).
							Replace(".", " ", StringComparison.OrdinalIgnoreCase).
							Replace(",", " ", StringComparison.OrdinalIgnoreCase).
							Replace("!", " ", StringComparison.OrdinalIgnoreCase) == adjustedText)
						{
							return currentIntent;
						}

						if (exactMatchOnly)
						{
							return ConversationConstants.HeardUnknownTrigger;
						}

						//else check by word count for this utterance
						string[] spokenWords = adjustedText.Split(" ");
						IList<string> utteranceWords = utterance.Trim().ToLower().Split(" ").ToList();

						foreach (string spokenWord in spokenWords)
						{
							if (string.IsNullOrWhiteSpace(spokenWord))
							{
								continue;
							}
							string updatedWord = spokenWord.Trim().ToLower();
							if (updatedWord == "the" || updatedWord == "an" || updatedWord == "a" || updatedWord == "um" || updatedWord == "uh")
							{
								continue;
							}

							if (utteranceWords.Contains(updatedWord))
							{
								currentHitCount = currentHitCount + 2;
							}
							else
							{
								foreach (string utteranceWord in utteranceWords)
								{
									if (utteranceWord.Contains(spokenWord))
									{
										currentHitCount++;
										break;
									}
								}
							}
						}

						foreach (string utteranceWord in utteranceWords)
						{
							if (string.IsNullOrWhiteSpace(utteranceWord))
							{
								continue;
							}
							string updatedWord = utteranceWord.Trim().ToLower();
							if (updatedWord == "the" || updatedWord == "an" || updatedWord == "a" || updatedWord == "um" || updatedWord == "uh")
							{
								continue;
							}

							if (spokenWords.Contains(updatedWord))
							{
								currentHitCount = currentHitCount + 2;
							}
							else
							{
								foreach (string spokenWord in spokenWords)
								{
									if (spokenWord.Contains(utteranceWord))
									{
										currentHitCount++;
										break;
									}
								}
							}
						}
						
						if (currentHitCount > 0)
						{
							if (currentHitCount > textComparisonObject.MaxHitCount)
							{
								textComparisonObject.MaxHitCount = currentHitCount;
								textComparisonObject.HitCountAverage = currentHitCount / spokenWords.Length;
							}
							if (currentHitCount == textComparisonObject.MaxHitCount)
							{
								if (currentHitCount / spokenWords.Length > textComparisonObject.MaxHitCount / textComparisonObject.HitCountAverage)
								{
									//same matches, less words
									textComparisonObject.HitCountAverage = currentHitCount / spokenWords.Length;
								}
							}
						}

						textComparisonObjects.Add(textComparisonObject);
					}
				}

				if (textComparisonObjects.Count > 0)
				{
					//Checked them all, now get the intent
					int maxHitCount = textComparisonObjects.Max(x => x.MaxHitCount);
					if (maxHitCount > 0)
					{
						return textComparisonObjects.Where(x => x.MaxHitCount == maxHitCount).OrderByDescending(x => x.HitCountAverage).First().Intent;
					}
				}
				return ConversationConstants.HeardUnknownTrigger;
			}
			catch
			{
				return ConversationConstants.HeardUnknownTrigger;
			}
		}
	}
}
