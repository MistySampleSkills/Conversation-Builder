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

namespace SpeechTools
{
	public class SpeechMatchData
	{
		public string Id { get; set; }
		public string Name { get; set; }
	}

	public class SpeechIntentInterpreter
	{
		private IDictionary<string, UtteranceData> _utteranceLists { get; set; }
		private IList<GenericDataStore> _userData;

		private const int WordMatchPoints = 2;
		private const int WordMatchAndPlacementPoints = 2;
		private const int WordPluralPoints = 1;

		public SpeechIntentInterpreter(IDictionary<string, UtteranceData> utteranceLists, IList<GenericDataStore> userData = null)
		{
			_utteranceLists = utteranceLists ?? new Dictionary<string, UtteranceData>();
			_userData = userData ?? new List<GenericDataStore>();
		}


		public SpeechMatchData GetMatch(string text, IDictionary<string, string> matchstrings, string wordMatchRule = "exact", bool exactPhraseMatchOnly = false)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(text))
				{
					SpeechMatchData speechMatchData = new SpeechMatchData();
					speechMatchData.Name = ConversationConstants.HeardNothingTrigger;
					speechMatchData.Id = ConversationConstants.HeardNothingTrigger;
					return speechMatchData;
				}

				//Create an utterance list
				IList<KeyValuePair<string, UtteranceData>> dynamiceUtteranceList = new List<KeyValuePair<string, UtteranceData>>();
				foreach (KeyValuePair<string, string> pair in matchstrings)
				{
					List<string> utteranceList = pair.Value.Split(",").ToList();
					dynamiceUtteranceList.Add(new KeyValuePair<string, UtteranceData>(pair.Key,
						new UtteranceData
						{
							Id = pair.Key,
							Name = pair.Key,
							Priority = 1,
							ExactPhraseMatchesOnly = exactPhraseMatchOnly,
							WordMatchRule = wordMatchRule,
							Utterances = utteranceList
						}));
				}
				
				return GetIntentExperimentFive(text, dynamiceUtteranceList);

			}
			catch
			{
				SpeechMatchData speechMatchData = new SpeechMatchData();
				speechMatchData.Name = ConversationConstants.HeardUnknownTrigger;
				speechMatchData.Id = ConversationConstants.HeardUnknownTrigger;
				return speechMatchData;
			}
		}

		public GenericData FindUserDataFromText(string name, string text)
		{
			try
			{
				GenericData genericData = new GenericData();
				if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(text) || _userData == null || !_userData.Any())
				{
					//	genericData.Key = text;
					//	genericData.Value = "";
					//	return genericData;
					return null;
				}

				IList<KeyValuePair<string, UtteranceData>> filteredUtteranceLists = new List<KeyValuePair<string, UtteranceData>>();				
				GenericDataStore genericDataStore = _userData.FirstOrDefault(x => string.Compare(x.Name, name, true) == 0);
				if (genericDataStore?.Name != null)
				{
					foreach (KeyValuePair<string, GenericData> data in genericDataStore.Data)
					{
						IList<string> utterances = data.Value.Key.Split(',').ToList();

						UtteranceData utteranceData = new UtteranceData();
						utteranceData.Name = data.Value.Key;
						utteranceData.Id = data.Value.Key;
						utteranceData.ExactPhraseMatchesOnly = genericDataStore.ExactPhraseMatchesOnly;
						utteranceData.WordMatchRule = genericDataStore.WordMatchRule;
						utteranceData.Utterances = utterances;
						utteranceData.Priority = genericData.Priority;

						filteredUtteranceLists.Add(new KeyValuePair<string, UtteranceData>(data.Value.Key, utteranceData));
					}
				}
				
				SpeechMatchData userDataKey = GetIntentExperimentFive(text, filteredUtteranceLists.ToList());				
				KeyValuePair<string, GenericData> returnData = genericDataStore.Data.FirstOrDefault(x => string.Compare(x.Value.Key, userDataKey.Name, true) == 0);
				if (returnData.Value != null)
				{
					return returnData.Value;
				}
				return null;
			}
			catch
			{
				//GenericData genericData = new GenericData();
				//genericData.Key = text;
				//genericData.Value = "";
				//return genericData;
				return null;
			}
		}

		/// <summary>
		/// If null or empty intent options passed in, compares against all of them
		/// </summary>
		/// <param name="text"></param>
		/// <param name="intentOptions"></param>
		/// <returns></returns>
		public SpeechMatchData GetIntent(string text, IList<string> intentOptions = null)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(text))
				{
					SpeechMatchData speechMatchData = new SpeechMatchData();
					speechMatchData.Name = ConversationConstants.HeardNothingTrigger;
					speechMatchData.Id = ConversationConstants.HeardNothingTrigger;
					return speechMatchData;
				}

				IEnumerable<KeyValuePair<string, UtteranceData>> filteredUtteranceLists = null;
				intentOptions = intentOptions ?? new List<string>();
				if (intentOptions == null || intentOptions.Count() == 0)
				{
					filteredUtteranceLists = _utteranceLists;
				}
				else
				{
					filteredUtteranceLists = _utteranceLists.Where(x => intentOptions.Contains(x.Key) || intentOptions.Contains(x.Value.Name, StringComparer.OrdinalIgnoreCase) /*legacy*/);
				}
				
				return GetIntentExperimentFive(text, filteredUtteranceLists.ToList());

			}
			catch
			{
				SpeechMatchData speechMatchData = new SpeechMatchData();
				speechMatchData.Name = ConversationConstants.HeardUnknownTrigger;
				speechMatchData.Id = ConversationConstants.HeardUnknownTrigger;
				return speechMatchData;
			}
		}

		public bool IsIgnoredWord(string word)
		{
			if (string.IsNullOrWhiteSpace(word))
			{
				return true;
			}

			//These seem like filler words
			switch (word.ToLower().Trim())
			{
				case "the":
				case "of":
				case "an":
				case "a":
				case "um":
				case "uh":
					return true;
				default:
					return false;


			}
		}
		private SpeechMatchData GetIntentExperimentFive(string text, IList<KeyValuePair<string, UtteranceData>> filteredUtteranceLists)
		{
			try
			{
				SpeechMatchData speechMatchData = new SpeechMatchData();
				IList<TextComparisonObject> textComparisonObjects = new List<TextComparisonObject>();

				if (string.IsNullOrWhiteSpace(text))
				{
					speechMatchData.Name = ConversationConstants.HeardNothingTrigger;
					speechMatchData.Id = ConversationConstants.HeardNothingTrigger;
					return speechMatchData;
				}

				foreach (KeyValuePair<string, UtteranceData> utteranceData in filteredUtteranceLists)
				{
					bool exactPhraseMatchOnly = utteranceData.Value.ExactPhraseMatchesOnly;
					string wordMatchRule = utteranceData.Value.WordMatchRule;
					/*TextComparisonObject textComparisonObject =
						new TextComparisonObject
						{
							Id = utteranceData.Key,
							Intent = utteranceData.Value.Name,
							HitCountAverage = 0,
							MaxHitCount = 0,
							Priority = utteranceData.Value.Priority
						};*/

					foreach (string utterance in utteranceData.Value.Utterances)
					{
						TextComparisonObject textComparisonObject =
						new TextComparisonObject
						{
							Id = utteranceData.Key,
							Intent = utteranceData.Value.Name,
							HitCountAverage = 0,
							MaxHitCount = 0,
							Priority = utteranceData.Value.Priority
						};

						//For each utterance string...
						int currentHitCount = 0;

						string adjustedText = text.
							Replace("?", " ", StringComparison.OrdinalIgnoreCase).
							Replace(".", " ", StringComparison.OrdinalIgnoreCase).
							Replace(",", " ", StringComparison.OrdinalIgnoreCase).
							Replace("!", " ", StringComparison.OrdinalIgnoreCase).
							Trim().ToLower();

						string adjustedUtterance = utterance.Trim().ToLower().
							Replace("?", " ", StringComparison.OrdinalIgnoreCase).
							Replace(".", " ", StringComparison.OrdinalIgnoreCase).
							Replace(",", " ", StringComparison.OrdinalIgnoreCase).
							Replace("!", " ", StringComparison.OrdinalIgnoreCase).
							Trim().ToLower();

						if (adjustedUtterance == adjustedText)
						{
							speechMatchData.Name = utteranceData.Value.Name;
							speechMatchData.Id = utteranceData.Key;
							//TODO What if more than one exact?
							return speechMatchData;
						}

						if (adjustedUtterance.Replace(" ", "") == adjustedText.Replace(" ", ""))
						{
							speechMatchData.Name = utteranceData.Value.Name;
							speechMatchData.Id = utteranceData.Key;
							//TODO What if more than one exact?
							return speechMatchData;
						}

						if (exactPhraseMatchOnly)
						{
							speechMatchData.Name = ConversationConstants.HeardUnknownTrigger;
							speechMatchData.Id = ConversationConstants.HeardUnknownTrigger;
							return speechMatchData;
						}

						IList<string> spokenWords = adjustedText.Split(" ").Where(x => !IsIgnoredWord(x)).Select(x => x.ToLower().Trim()).ToList();
						IList<string> matchWords = utterance.Trim().ToLower().Split(" ").Where(x => !IsIgnoredWord(x)).Select(x => x.ToLower().Trim()).ToList();

						foreach (string matchWord in matchWords)
						{
							if (spokenWords.Contains(matchWord, StringComparer.OrdinalIgnoreCase))
							{
								//if (spokenWords.IndexOf(matchWord) == matchWords.IndexOf(matchWord))
								//{
								//	currentHitCount += WordMatchAndPlacementPoints;
								//}
								//else
								//{
								currentHitCount += WordMatchPoints;
								//}
							}
							else if (wordMatchRule != "exact")
							{
								switch (wordMatchRule)
								{
									case "startswith":
										foreach (string spokenWord in spokenWords)
										{
											if (matchWord.StartsWith(spokenWord))
											{
												currentHitCount += WordPluralPoints;
												//break;
											}
										}
										break;
									case "endswith":
										foreach (string spokenWord in spokenWords)
										{
											if (matchWord.EndsWith(spokenWord))
											{
												currentHitCount += WordPluralPoints;
												//break;
											}
										}
										break;
									case "contains":
										foreach (string spokenWord in spokenWords)
										{
											if (matchWords.Contains(spokenWord, StringComparer.OrdinalIgnoreCase))
											{
												currentHitCount = currentHitCount + 2;
											}
											else
											{
												foreach (string utteranceWord in matchWords)
												{
													if (utteranceWord.Contains(spokenWord))
													{
														currentHitCount++;
														//break;
													}
												}
											}
										}

										foreach (string utteranceWord in matchWords)
										{
											if (spokenWords.Contains(matchWord, StringComparer.OrdinalIgnoreCase))
											{
												currentHitCount += WordPluralPoints;
											}
											else
											{
												foreach (string spokenWord in spokenWords)
												{
													if (spokenWord.Contains(utteranceWord))
													{
														currentHitCount += WordPluralPoints;
														//break;
													}
												}
											}
										}
										break;
									case "plurals.v1":
										//Hack attempt for catching some English plurals, prolly too greedy
										foreach (string spokenWord in spokenWords)
										{
											int wordLength = spokenWord.Length;
											if (wordLength > 6)
											{
												if (matchWord.Contains(spokenWord.Substring(0, wordLength - 3)))
												{
													currentHitCount += WordPluralPoints;
													//break;
												}
											}
											else if (wordLength > 5)
											{
												if (matchWord.Contains(spokenWord.Substring(0, wordLength - 2)))
												{
													currentHitCount += WordPluralPoints;
													//break;
												}
											}
											else if (wordLength > 4)
											{
												if (matchWord.Contains(spokenWord.Substring(0, wordLength - 1)))
												{
													currentHitCount += WordPluralPoints;
													//break;
												}
											}
										}
										break;
									case "exact":
									default:
										//do nothing  else...
										break;

								}
							}
						}

						if (currentHitCount > 0)
						{
							if (currentHitCount > textComparisonObject.MaxHitCount)
							{
								textComparisonObject.MaxHitCount = currentHitCount;
								textComparisonObject.HitCountAverage = currentHitCount / matchWords.Count;
							}
							if (currentHitCount == textComparisonObject.MaxHitCount)
							{
								if (currentHitCount / matchWords.Count > textComparisonObject.MaxHitCount / textComparisonObject.HitCountAverage)
								{
									//same hit count, less words
									textComparisonObject.HitCountAverage = currentHitCount / matchWords.Count;
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
						//Add priority checking for matching compare counts
						TextComparisonObject closest = textComparisonObjects.Where(x => x.MaxHitCount == maxHitCount).OrderByDescending(x => x.HitCountAverage).OrderBy(x => x.Priority).First();
						speechMatchData.Name = closest.Intent;
						speechMatchData.Id = closest.Id;
						return speechMatchData;
					}
				}

				speechMatchData.Name = ConversationConstants.HeardUnknownTrigger;
				speechMatchData.Id = ConversationConstants.HeardUnknownTrigger;
				return speechMatchData;
			}
			catch
			{
				SpeechMatchData speechMatchData = new SpeechMatchData();
				speechMatchData.Name = ConversationConstants.HeardUnknownTrigger;
				speechMatchData.Id = ConversationConstants.HeardUnknownTrigger;
				return speechMatchData;
			}
		}

		private SpeechMatchData GetIntentExperimentFour(string text, IList<KeyValuePair<string, UtteranceData>> filteredUtteranceLists)
		{
			try
			{
				SpeechMatchData speechMatchData = new SpeechMatchData();
				IList<TextComparisonObject> textComparisonObjects = new List<TextComparisonObject>();

				if (string.IsNullOrWhiteSpace(text))
				{
					speechMatchData.Name = ConversationConstants.HeardNothingTrigger;
					speechMatchData.Id = ConversationConstants.HeardNothingTrigger;
					return speechMatchData;
				}

				foreach (KeyValuePair<string, UtteranceData> utteranceData in filteredUtteranceLists)
				{
					bool exactPhraseMatchOnly = utteranceData.Value.ExactPhraseMatchesOnly;
					string wordMatchRule = utteranceData.Value.WordMatchRule;
					TextComparisonObject textComparisonObject =
						new TextComparisonObject
						{
							Id = utteranceData.Key,
							Intent = utteranceData.Value.Name,
							HitCountAverage = 0,
							MaxHitCount = 0,
							Priority = utteranceData.Value.Priority
						};

					foreach (string utterance in utteranceData.Value.Utterances)
					{
						//For each utterance string...
						int currentHitCount = 0;

						string adjustedText = text.
							Replace("?", " ", StringComparison.OrdinalIgnoreCase).
							Replace(".", " ", StringComparison.OrdinalIgnoreCase).
							Replace(",", " ", StringComparison.OrdinalIgnoreCase).
							Replace("!", " ", StringComparison.OrdinalIgnoreCase).
							Trim().ToLower();

						string adjustedUtterance = utterance.Trim().ToLower().
							Replace("?", " ", StringComparison.OrdinalIgnoreCase).
							Replace(".", " ", StringComparison.OrdinalIgnoreCase).
							Replace(",", " ", StringComparison.OrdinalIgnoreCase).
							Replace("!", " ", StringComparison.OrdinalIgnoreCase).
							Trim().ToLower();

						if (adjustedUtterance == adjustedText)
						{
							speechMatchData.Name = utteranceData.Value.Name;
							speechMatchData.Id = utteranceData.Key;
							//TODO What if more than one exact?
							return speechMatchData;
						}

						if (adjustedUtterance.Replace(" ", "") == adjustedText.Replace(" ", ""))
						{
							speechMatchData.Name = utteranceData.Value.Name;
							speechMatchData.Id = utteranceData.Key;
							//TODO What if more than one exact?
							return speechMatchData;
						}

						if (exactPhraseMatchOnly)
						{
							speechMatchData.Name = ConversationConstants.HeardUnknownTrigger;
							speechMatchData.Id = ConversationConstants.HeardUnknownTrigger;
							return speechMatchData;
						}

						IList<string> spokenWords = adjustedText.Split(" ").Where(x => !IsIgnoredWord(x)).Select(x => x.ToLower().Trim()).ToList();
						IList<string> matchWords = utterance.Trim().ToLower().Split(" ").Where(x => !IsIgnoredWord(x)).Select(x => x.ToLower().Trim()).ToList();

						foreach (string matchWord in matchWords)
						{
							if (spokenWords.Contains(matchWord, StringComparer.OrdinalIgnoreCase))
							{
								if (spokenWords.IndexOf(matchWord) == matchWords.IndexOf(matchWord))
								{
									currentHitCount += WordMatchAndPlacementPoints;
								}
								else
								{
									currentHitCount += WordMatchPoints;
								}
							}
							else if (wordMatchRule != "exact")
							{
								switch (wordMatchRule)
								{
									case "startswith":
										foreach (string spokenWord in spokenWords)
										{
											if (matchWord.StartsWith(spokenWord))
											{
												currentHitCount += WordPluralPoints;
												break;
											}
										}
										break;
									case "endswith":
										foreach (string spokenWord in spokenWords)
										{
											if (matchWord.EndsWith(spokenWord))
											{
												currentHitCount += WordPluralPoints;
												break;
											}
										}
										break;
									case "contains":
										foreach (string spokenWord in spokenWords)
										{
											if (matchWords.Contains(spokenWord))
											{
												currentHitCount = currentHitCount + 2;
											}
											else
											{
												foreach (string utteranceWord in matchWords)
												{
													if (utteranceWord.Contains(spokenWord))
													{
														currentHitCount++;
														break;
													}
												}
											}
										}

										foreach (string utteranceWord in matchWords)
										{
											if (spokenWords.Contains(matchWord))
											{
												currentHitCount += WordPluralPoints;
											}
											else
											{
												foreach (string spokenWord in spokenWords)
												{
													if (spokenWord.Contains(utteranceWord))
													{
														currentHitCount += WordPluralPoints;
														break;
													}
												}
											}
										}
										break;
									case "plurals.v1":
										//Hack attempt for catching some English plurals, prolly too greedy
										foreach (string spokenWord in spokenWords)
										{
											int wordLength = spokenWord.Length;
											if (wordLength > 4)
											{
												if (matchWord.Contains(spokenWord.Substring(0, wordLength - 1)))
												{
													currentHitCount += WordPluralPoints;
													break;
												}
											}

											if (wordLength > 5)
											{
												if (matchWord.Contains(spokenWord.Substring(0, wordLength - 2)))
												{
													currentHitCount += WordPluralPoints;
													break;
												}
											}

											if (wordLength > 6)
											{
												if (matchWord.Contains(spokenWord.Substring(0, wordLength - 3)))
												{
													currentHitCount += WordPluralPoints;
													break;
												}
											}
										}
										break;
									case "exact":
									default:
										//do nothing  else...
										break;

								}
							}
						}

						if (currentHitCount > 0)
						{
							if (currentHitCount > textComparisonObject.MaxHitCount)
							{
								textComparisonObject.MaxHitCount = currentHitCount;
								textComparisonObject.HitCountAverage = currentHitCount / spokenWords.Count;
							}
							if (currentHitCount == textComparisonObject.MaxHitCount)
							{
								if (currentHitCount / spokenWords.Count > textComparisonObject.MaxHitCount / textComparisonObject.HitCountAverage)
								{
									//same hit count, less words
									textComparisonObject.HitCountAverage = currentHitCount / spokenWords.Count;
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
						//Add priority checking for matching compare counts
						TextComparisonObject closest = textComparisonObjects.Where(x => x.MaxHitCount == maxHitCount).OrderByDescending(x => x.HitCountAverage).OrderBy(x => x.Priority).First();
						speechMatchData.Name = closest.Intent;
						speechMatchData.Id = closest.Id;
						return speechMatchData;
					}
				}

				speechMatchData.Name = ConversationConstants.HeardUnknownTrigger;
				speechMatchData.Id = ConversationConstants.HeardUnknownTrigger;
				return speechMatchData;
			}
			catch
			{
				SpeechMatchData speechMatchData = new SpeechMatchData();
				speechMatchData.Name = ConversationConstants.HeardUnknownTrigger;
				speechMatchData.Id = ConversationConstants.HeardUnknownTrigger;
				return speechMatchData;
			}
		}
	}
}

