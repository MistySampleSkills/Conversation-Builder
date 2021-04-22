using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ConversationBuilder.DataModels
{
	public class DefaultEmotions
	{
		public ConcurrentDictionary<string, string> AllItems { get; private set; }= new ConcurrentDictionary<string, string>();
		public DefaultEmotions()
		{
			AllItems.TryAdd(Admiration, Admiration);
			AllItems.TryAdd(Adoration, Adoration);
			AllItems.TryAdd(AestheticAppreciation, AestheticAppreciation);
			AllItems.TryAdd(Amusement, Amusement);
			AllItems.TryAdd(Anxiety, Anxiety);
			AllItems.TryAdd(Awe, Awe);
			AllItems.TryAdd(Awkwardness, Awkwardness);
			AllItems.TryAdd(Boredom, Boredom);
			AllItems.TryAdd(Calmness, Calmness);
			AllItems.TryAdd(Confusion, Confusion);
			AllItems.TryAdd(Craving, Craving);
			AllItems.TryAdd(Disgust, Disgust);
			AllItems.TryAdd(EmpatheticPain, EmpatheticPain);
			AllItems.TryAdd(Entrancement, Entrancement);
			AllItems.TryAdd(Envy, Envy);
			AllItems.TryAdd(Excitement, Excitement);
			AllItems.TryAdd(Fear, Fear);

			AllItems.TryAdd(Horror, Horror);
			AllItems.TryAdd(Interest, Interest);
			AllItems.TryAdd(Joy, Joy);
			AllItems.TryAdd(Nostalgia, Nostalgia);
			AllItems.TryAdd(Romance, Romance);
			AllItems.TryAdd(Sadness, Sadness);
			AllItems.TryAdd(Satisfaction, Satisfaction);
			AllItems.TryAdd(Desire, Desire);
			AllItems.TryAdd(Sympathy, Sympathy);
			AllItems.TryAdd(Triumph, Triumph);
			AllItems.TryAdd(Avoidance, Avoidance);
		}

		public const string Admiration = "Admiration";
		public const string Adoration = "Adoration";
		public const string AestheticAppreciation = "AestheticAppreciation";
		public const string Amusement = "Amusement";
		public const string Anxiety = "Anxiety";
		public const string Awe = "Awe";
		public const string Awkwardness = "Awkwardness";
		public const string Boredom = "Boredom";
		public const string Calmness = "Calmness";
		public const string Confusion = "Confusion";
		public const string Craving = "Craving";
		public const string Disgust = "Disgust";
		public const string EmpatheticPain = "EmpatheticPain";
		public const string Entrancement = "Entrancement";
		public const string Envy = "Envy";
		public const string Excitement = "Excitement";
		public const string Fear = "Fear";
		public const string Horror = "Horror";
		public const string Interest = "Interest";
		public const string Joy = "Joy";
		public const string Nostalgia = "Nostalgia";
		public const string Romance = "Romance";
		public const string Sadness = "Sadness";
		public const string Satisfaction = "Satisfaction";
		public const string Desire = "Desire";
		public const string Sympathy = "Sympathy";
		public const string Triumph = "Triumph";
		public const string Avoidance = "Avoidance";
	}

	public class LEDPatterns
	{
		public ConcurrentDictionary<string, string> AllItems { get; private set; }= new ConcurrentDictionary<string, string>();
		public LEDPatterns()
		{
			AllItems.TryAdd(None, None);
			AllItems.TryAdd(TransitOnce, TransitOnce);
			AllItems.TryAdd(Breathe, Breathe);
			AllItems.TryAdd(Blink, Blink);
		}

		public const string None = "None";
		public const string TransitOnce = "TransitOnce";
		public const string Breathe = "Breathe";
		public const string Blink = "Blink";
	}

	public class LogLevels
	{
		public ConcurrentDictionary<string, string> AllItems { get; private set; }= new ConcurrentDictionary<string, string>();
		public LogLevels()
		{
			AllItems.TryAdd(Verbose, Verbose);
			AllItems.TryAdd(Info, Info);
			AllItems.TryAdd(Warning, Warning);
			AllItems.TryAdd(Error, Error);
		}

		public const string Verbose = "Verbose";
		public const string Info = "Info";
		public const string Warning = "Warning";
		public const string Error = "Error";
	}

	public class SpeechProfanitySettings
	{
		public ConcurrentDictionary<string, string> AllItems { get; private set; }= new ConcurrentDictionary<string, string>();
		public SpeechProfanitySettings()
		{
			AllItems.TryAdd(Raw, Raw);
			AllItems.TryAdd(Removed, Removed);
			AllItems.TryAdd(Masked, Masked);
		}

		/// <summary>
		/// Default
		/// Allows profanity
		/// </summary>
		public const string Raw = "Raw";

		/// <summary>
		/// Profanity removed
		/// </summary>
		public const string Removed = "Removed";

		/// <summary>
		/// Profane word characters replaced with asterisks
		/// </summary>
		public const string Masked = "Masked";
	}

	public class SpeechServices
	{
		public ConcurrentDictionary<string, string> AllItems { get; private set; }= new ConcurrentDictionary<string, string>();
		public SpeechServices()
		{
			AllItems.TryAdd(Misty, Misty);
			AllItems.TryAdd(Azure, Azure);
			AllItems.TryAdd(Google, Google);
		}

		public const string Misty = "Misty";
		public const string Azure = "Azure";
		public const string Google = "Google";
	}

	public class Triggers
	{
		public ConcurrentDictionary<string, string> AllItems { get; private set; }= new ConcurrentDictionary<string, string>();
		public Triggers()
		{
			AllItems.TryAdd(SpeechHeard, SpeechHeard);
			AllItems.TryAdd(None, None);
			AllItems.TryAdd(Timeout, Timeout);
			AllItems.TryAdd(Timer, Timer);
			AllItems.TryAdd(FaceRecognized, FaceRecognized);
			AllItems.TryAdd(BumperPressed, BumperPressed);
			AllItems.TryAdd(BumperReleased, BumperReleased);
			AllItems.TryAdd(CapTouched, CapTouched);
			AllItems.TryAdd(CapReleased, CapReleased);
			AllItems.TryAdd(QrTagSeen, QrTagSeen);
			AllItems.TryAdd(ArTagSeen, ArTagSeen);
			AllItems.TryAdd(SerialMessage, SerialMessage);
			AllItems.TryAdd(ObjectSeen, ObjectSeen);
			AllItems.TryAdd(ExternalEvent, ExternalEvent);
			AllItems.TryAdd(AudioCompleted, AudioCompleted);
		}

		public const string None = "None"; //only used for end trigger

		//Timed out with no successful (unhandled Unknowns) intents
		//Can be handled, not the same as Interaction timeout which ends conversation
		public const string Timeout = "Timeout";

		//User defined timer trigger event
		public const string Timer = "Timer";

		//Common trigger types caused by interaction with robot
		public const string SpeechHeard = "SpeechHeard";
		public const string FaceRecognized = "FaceRecognized";
		public const string BumperPressed = "BumperPressed";
		public const string CapTouched = "CapTouched";
		public const string BumperReleased = "BumperReleased";
		public const string CapReleased = "CapReleased";
		public const string QrTagSeen = "QrTagSeen";
		public const string ArTagSeen = "ArTagSeen";
		public const string SerialMessage = "SerialMessage";
		public const string ObjectSeen = "ObjectSeen";
		public const string KeyPhraseRecognized = "KeyPhraseRecognized";

		//trigger due to external event call into robot skill
		public const string ExternalEvent = "ExternalEvent";

		//To immediately trigger a start or stop trigger or go to next animation after Misty speaks plays audio
		public const string AudioCompleted = "AudioCompleted";
		
		//TODO
		//MoodChanged
	}

	public class TriggerFilters
	{
		public ConcurrentDictionary<string, string> AllItems { get; private set; }= new ConcurrentDictionary<string, string>();
		public TriggerFilters()
		{
			AllItems.TryAdd(Chin, Chin);
			AllItems.TryAdd(Scruff, Scruff);
			AllItems.TryAdd(Right, "Right Cap");
			AllItems.TryAdd(Left, "Left Cap");
			AllItems.TryAdd(Front, "Front Cap");
			AllItems.TryAdd(Back, "Back Cap");

			AllItems.TryAdd(FrontRight, "Front Right Bumper");
			AllItems.TryAdd(FrontLeft, "Front Left Bumper");
			AllItems.TryAdd(BackRight, "Back Right Bumper");
			AllItems.TryAdd(BackLeft, "Back Left Bumper");
			
			AllItems.TryAdd(HeardNothing, "Heard Nothing");
			AllItems.TryAdd(HeardUnknownSpeech, "Heard Unknown Speech");
			
			AllItems.TryAdd(SeenUnknownFace, "See Unknown Face");
			AllItems.TryAdd(SeenKnownFace, "See Known Face");
			AllItems.TryAdd(SeenNewFace, "See New Known Face");

			IDictionary<string, string> objects = new Objects().GetObjectList();
			foreach(KeyValuePair<string, string> objectData in objects)
			{
				AllItems.TryAdd(objectData.Key, objectData.Value);
			}
		}

		//TODO add release
		public const string Chin = "Chin";
		public const string Scruff = "Scruff";
		public const string Right = "Right";
		public const string Left = "Left";
		public const string Front = "Front";
		public const string Back = "Back";
		
		public const string FrontRight = "FrontRight";
		public const string FrontLeft = "FrontLeft";
		public const string BackRight = "BackRight";
		public const string BackLeft = "BackLeft";
	
		public const string HeardNothing = "HeardNothing";
		public const string HeardUnknownSpeech = "HeardUnknownSpeech";
		
		public const string SeenKnownFace = "SeenKnownFace";
		public const string SeenUnknownFace = "SeenUnknownFace";

		public const string SeenNewFace = "SeenNewFace";		
	}

	public class Objects
	{
		public Dictionary<string, string> GetObjectList()
		{
			Dictionary<string, string> list = new Dictionary<string, string>();
			foreach(ObjectFilter objectName in Enum.GetValues(typeof(ObjectFilter)))
			{
				string pertyName = objectName.ToString().Replace("_", " ");
				list.Add(pertyName, $"object: {pertyName}");
			}
			return list ?? new Dictionary<string, string>();
		}

	}
	
	public enum ObjectFilter
	{
		person,
		bicycle,
		car,
		motorcycle,
		airplane,
		bus,
		train,
		truck,
		boat,
		traffic_light,
		fire_hydrant,
		stop_sign,
		parking_meter,
		bench,
		bird,
		cat,
		dog,
		horse,
		sheep,
		cow,
		elephant,
		bear,
		zebra,
		giraffe,
		backpack,
		umbrella,
		handbag,
		tie,
		suitcase,
		frisbee,
		skis,
		snowboard,
		sports_ball,
		kite,
		baseball_bat,
		baseball_glove,
		skateboard,
		surfboard,
		tennis_racket,
		bottle,
		wine_glass,
		cup,
		fork,
		knife,
		spoon,
		bowl,
		banana,
		apple,
		sandwich,
		orange,
		broccoli,
		carrot,
		hot_dog,
		pizza,
		donut,
		cake,
		chair,
		couch,
		potted_plant,
		bed,
		dining_table,
		toilet,
		tv,
		laptop,
		mouse,
		remote,
		keyboard,
		cell_phone,
		microwave,
		oven,
		toaster,
		sink,
		refrigerator,
		book,
		clock,
		vase,
		scissors,
		teddy_bear,
		hair_drier,
		toothbrush
	}
}