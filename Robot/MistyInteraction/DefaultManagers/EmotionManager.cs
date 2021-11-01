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

namespace MistyInteraction
{
	public enum IntensityLevel
	{
		Ignored = 0,
		Boredom = 1,
		Calm = 2,
		AboveNormal = 3,
		Intense = 4,
		ExtremelyIntense = 5
	}

	public enum PleasureLevel
	{
		Hate = 0,
		Dislike = 1,
		Normal = 2,
		Like = 3,
		Love = 4
	}

	public enum DominanceLevel
	{
		Submissive = 0,
		Normal = 1,
		Dominant = 2
	}

	public class PID
	{
		public PID(PleasureLevel pleasure, IntensityLevel intensity, DominanceLevel dominance)
		{
			Pleasure = pleasure;
			Intensity = intensity;
			Dominance = dominance;
		}

		public PleasureLevel Pleasure { get; set; }
		public IntensityLevel Intensity { get; set; }
		public DominanceLevel Dominance { get; set; }
	}

	public class EmotionManager : IEmotionManager
	{
		private Random _random = new Random();
		private string _currentEmotion = Emotions.Joy;
		public PID CurrentPID { get; private set; }
		private IDictionary<string, PID> EmotionalMapping = new Dictionary<string, PID>();

		public string GetCurrentEmotion()
		{
			return _currentEmotion;
		}

		public EmotionManager(string startingEmotion)
		{
			_currentEmotion = startingEmotion;
			CurrentPID = GetPIDForEmotion(_currentEmotion);

			EmotionalMapping.TryAdd(Emotions.Entrancement, new PID(PleasureLevel.Love, IntensityLevel.AboveNormal, DominanceLevel.Submissive));
			EmotionalMapping.TryAdd(Emotions.Joy, new PID(PleasureLevel.Love, IntensityLevel.Intense, DominanceLevel.Dominant));
			EmotionalMapping.TryAdd(Emotions.Adoration, new PID(PleasureLevel.Love, IntensityLevel.Intense, DominanceLevel.Dominant));
			EmotionalMapping.TryAdd(Emotions.Romance, new PID(PleasureLevel.Love, IntensityLevel.Intense, DominanceLevel.Dominant));
			EmotionalMapping.TryAdd(Emotions.Craving, new PID(PleasureLevel.Love, IntensityLevel.Intense, DominanceLevel.Dominant));
			EmotionalMapping.TryAdd(Emotions.Awe, new PID(PleasureLevel.Love, IntensityLevel.ExtremelyIntense, DominanceLevel.Submissive));
			EmotionalMapping.TryAdd(Emotions.Desire, new PID(PleasureLevel.Love, IntensityLevel.ExtremelyIntense, DominanceLevel.Dominant));
			EmotionalMapping.TryAdd(Emotions.Triumph, new PID(PleasureLevel.Love, IntensityLevel.ExtremelyIntense, DominanceLevel.Dominant));

			EmotionalMapping.TryAdd(Emotions.AestheticAppreciation, new PID(PleasureLevel.Like, IntensityLevel.Calm, DominanceLevel.Normal));
			EmotionalMapping.TryAdd(Emotions.Interest, new PID(PleasureLevel.Like, IntensityLevel.AboveNormal, DominanceLevel.Normal));
			EmotionalMapping.TryAdd(Emotions.Admiration, new PID(PleasureLevel.Like, IntensityLevel.AboveNormal, DominanceLevel.Normal));
			EmotionalMapping.TryAdd(Emotions.Amusement, new PID(PleasureLevel.Like, IntensityLevel.AboveNormal, DominanceLevel.Normal));
			EmotionalMapping.TryAdd(Emotions.Nostalgia, new PID(PleasureLevel.Like, IntensityLevel.AboveNormal, DominanceLevel.Normal));
			EmotionalMapping.TryAdd(Emotions.Satisfaction, new PID(PleasureLevel.Like, IntensityLevel.Intense, DominanceLevel.Dominant));
			EmotionalMapping.TryAdd(Emotions.Excitement, new PID(PleasureLevel.Like, IntensityLevel.ExtremelyIntense, DominanceLevel.Dominant));

			EmotionalMapping.TryAdd(Emotions.Calmness, new PID(PleasureLevel.Normal, IntensityLevel.Calm, DominanceLevel.Normal));
			EmotionalMapping.TryAdd(Emotions.Avoidance, new PID(PleasureLevel.Dislike, IntensityLevel.Ignored, DominanceLevel.Normal));

			EmotionalMapping.TryAdd(Emotions.Boredom, new PID(PleasureLevel.Dislike, IntensityLevel.Boredom, DominanceLevel.Submissive));
			EmotionalMapping.TryAdd(Emotions.EmpatheticPain, new PID(PleasureLevel.Dislike, IntensityLevel.AboveNormal, DominanceLevel.Normal));
			EmotionalMapping.TryAdd(Emotions.Awkwardness, new PID(PleasureLevel.Dislike, IntensityLevel.Intense, DominanceLevel.Submissive));
			EmotionalMapping.TryAdd(Emotions.Confusion, new PID(PleasureLevel.Dislike, IntensityLevel.Intense, DominanceLevel.Submissive));
			EmotionalMapping.TryAdd(Emotions.Envy, new PID(PleasureLevel.Dislike, IntensityLevel.Intense, DominanceLevel.Dominant));
			EmotionalMapping.TryAdd(Emotions.Sadness, new PID(PleasureLevel.Dislike, IntensityLevel.Intense, DominanceLevel.Dominant));
			EmotionalMapping.TryAdd(Emotions.Sympathy, new PID(PleasureLevel.Dislike, IntensityLevel.Intense, DominanceLevel.Submissive));
			EmotionalMapping.TryAdd(Emotions.Anxiety, new PID(PleasureLevel.Dislike, IntensityLevel.Intense, DominanceLevel.Submissive));
			
			EmotionalMapping.TryAdd(Emotions.Fear, new PID(PleasureLevel.Hate, IntensityLevel.Intense, DominanceLevel.Submissive));
			EmotionalMapping.TryAdd(Emotions.Disgust, new PID(PleasureLevel.Hate, IntensityLevel.Intense, DominanceLevel.Dominant));
			EmotionalMapping.TryAdd(Emotions.Horror, new PID(PleasureLevel.Hate, IntensityLevel.ExtremelyIntense, DominanceLevel.Submissive));
		}

		public PID GetPIDForEmotion(string emotion)
		{
			if(EmotionalMapping.TryGetValue(emotion, out PID pid))
			{
				return pid;
			}
			return new PID(PleasureLevel.Normal, IntensityLevel.Ignored, DominanceLevel.Normal);
		}
        
		public string GetNextEmotion(string newEmotion)
		{
			PID newPid = GetPIDForEmotion(newEmotion);

			if (newPid.Intensity == IntensityLevel.Ignored)
			{
				return _currentEmotion;
			}

			PID changingPid = new PID(CurrentPID.Pleasure, CurrentPID.Intensity, CurrentPID.Dominance);

			if (newPid.Pleasure < CurrentPID.Pleasure)
			{
				changingPid.Pleasure = CurrentPID.Pleasure - 1;
			}
			else if (newPid.Pleasure > CurrentPID.Pleasure)
			{
				changingPid.Pleasure = CurrentPID.Pleasure + 1;
			}

			if (newPid.Intensity < CurrentPID.Intensity)
			{
				changingPid.Intensity = CurrentPID.Intensity - 1;
			}
			else if (newPid.Intensity > CurrentPID.Intensity)
			{
				changingPid.Intensity = CurrentPID.Intensity + 1;
			}

			if (newPid.Dominance < CurrentPID.Dominance)
			{
				changingPid.Dominance = CurrentPID.Dominance - 1;
			}
			else if (newPid.Dominance > CurrentPID.Dominance)
			{
				changingPid.Dominance = CurrentPID.Dominance + 1;
			}

			KeyValuePair<string, PID> nextEmotion = EmotionalMapping.FirstOrDefault(x => x.Value.Pleasure == changingPid.Pleasure && x.Value.Intensity == changingPid.Intensity && x.Value.Dominance == changingPid.Dominance);

			//One exists, return it
			if (nextEmotion.Value != null)
			{
				CurrentPID = nextEmotion.Value;
				_currentEmotion = nextEmotion.Key;
				return _currentEmotion;
			}

			//You have no emotion to deal, so do yer best
			//For now, find an emotion at the new pleasure level
			IList<KeyValuePair<string, PID>> pleasureEmotionList = EmotionalMapping.Where(x => x.Value.Pleasure == changingPid.Pleasure).ToList();			
			IList<KeyValuePair<string, PID>> dominanceEmotionList = pleasureEmotionList.Where(x => x.Value.Dominance == changingPid.Dominance).ToList();
			if(dominanceEmotionList.Count() > 0)
			{
				IntensityLevel intensity = changingPid.Intensity;
				//Get the closest in intensity going down since we know we don't have a match for this...
				while (intensity >= IntensityLevel.Ignored)
				{
					intensity--;
					KeyValuePair<string, PID> selectedEmotion = dominanceEmotionList.FirstOrDefault(x => x.Value.Intensity == intensity);
					if(selectedEmotion.Value != null)
					{
						CurrentPID = selectedEmotion.Value;
						_currentEmotion = selectedEmotion.Key;
						return _currentEmotion;
					}
				}
				
				//if none, pick a random one :D
				int selection = _random.Next(0, dominanceEmotionList.Count());

				CurrentPID = dominanceEmotionList[selection].Value;
				_currentEmotion = dominanceEmotionList[selection].Key;
				return _currentEmotion;
			}
			else
			{
				//TODO Better transition logic
				int selection = _random.Next(0, pleasureEmotionList.Count());

				CurrentPID = pleasureEmotionList[selection].Value;
				_currentEmotion = pleasureEmotionList[selection].Key;
				return _currentEmotion;
			}
		}
	}
}