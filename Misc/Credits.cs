using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modding;
using UnityEngine.SceneManagement;
using GlobalEnums;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Audio;

namespace FiveKnights
{
	// Much of the below code referenced HKV's custom credits code
	public static class Credits
	{
        public static void Hook()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += ActiveSceneChanged;
			On.GameManager.IsNonGameplayScene += GameManagerIsNonGameplayScene;
		}

		private static void ActiveSceneChanged(Scene arg0, Scene arg1)
		{
			if(arg1.name == "Pale_Court_Credits")
			{
				GameObject.Find("Credits Controller").AddComponent<CreditsController>();
			}
		}

		private static bool GameManagerIsNonGameplayScene(On.GameManager.orig_IsNonGameplayScene orig, GameManager self)
		{
			return orig(self) || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Pale_Court_Credits";
		}
	}

	public class CreditsController : MonoBehaviour
	{
		// Values measured on a 3840 x 2400 screen
		private readonly float ScrollSpeed = 330f * (Screen.height / 2400f);
		private readonly float ScrollY = 17500f * (Screen.height / 2400f);

		private Transform _creditsParent;
		private Transform _scrollParent;
		private Transform _thanksParent;
		private Transform _finalParent;
		private AudioSource _creditsAudio;

		private Coroutine _creditsCoro;
		private Coroutine _finalFadeCoro;
		private Coroutine _returnCoro;
		private bool _scrolling;

		private void Start()
		{
			GameManager.instance.SetState(GameState.CUTSCENE);
			UIManager.instance.SetState(UIState.CUTSCENE);
			InputHandler.Instance.PreventPause();

			_creditsParent = GameObject.Find("Credits Parent").transform;
			_scrollParent = GameObject.Find("Scroll Parent").transform;
			_thanksParent = GameObject.Find("Thanks Parent").transform;
			_finalParent = GameObject.Find("Final Parent").transform;
			_creditsAudio = GameObject.Find("Credits Audio").GetComponent<AudioSource>();
			_scrolling = false;
			FixFonts();
			StopAudio();
			_creditsCoro = StartCoroutine(RollCredits());
		}

		private void Update()
		{
			if(_scrolling)
			{
				_scrollParent.Translate(Vector3.up * Time.deltaTime * ScrollSpeed);
			}
		}

		private void FixFonts()
		{
			foreach(Text text in _creditsParent.parent.GetComponentsInChildren<Text>(true))
			{
				text.font = CanvasUtil.GetFont("Perpetua");
			}
		}

		private void StopAudio()
		{
			MusicCue musicCue = ScriptableObject.CreateInstance<MusicCue>();
			MusicCue.MusicChannelInfo[] channelInfos = new MusicCue.MusicChannelInfo[]
			{
				new MusicCue.MusicChannelInfo(), null, null, null, null, null
			};
			Vasi.Mirror.SetField(musicCue, "channelInfos", channelInfos);
			var yoursnapshot = Resources.FindObjectsOfTypeAll<AudioMixer>().First(x => x.name == "Music").FindSnapshot("Main Only");
			yoursnapshot.TransitionTo(0);
			GameManager.instance.AudioManager.ApplyMusicCue(musicCue, 0f, 0f, false);
		}

		private IEnumerator RollCredits()
		{
			Log("Starting credits sequence");
			Log("Screen height is " + Screen.height);
			Log("Screen resolution height is " + Screen.currentResolution.height);

			yield return new WaitForSeconds(1f);

			if(FiveKnights.Instance.SaveSettings.HasSeenCredits) _returnCoro = StartCoroutine(ReturnFromCredits());

			_creditsAudio.clip = ABManager.AssetBundles[ABManager.Bundle.Sound].LoadAsset<AudioClip>("CreditsMusic");
			_creditsAudio.outputAudioMixerGroup = Vasi.Mirror.GetField<AudioManager, AudioSource[]>(GameManager.instance.AudioManager, 
				"musicSources")[0].outputAudioMixerGroup;
			_creditsAudio.Play();

			Log("Fade main credits");
			for(int i = 0; i < _creditsParent.childCount; i++)
			{
				yield return FadeInOut(_creditsParent.GetChild(i).gameObject, 1f, 5.5f, 1f);
				yield return new WaitForSeconds(0.5f);
			}

			Log("Scroll credits");
			_scrolling = true;
			yield return null;
			yield return null;
			yield return WaitForScrollEnd();
			_scrolling = false;

			Log("Fade thank yous");
			for(int i = 0; i < _thanksParent.childCount; i++)
			{
				yield return FadeInOut(_thanksParent.GetChild(i).gameObject, 1f, 6.5f, 1f);
				yield return new WaitForSeconds(0.5f);
			}

			Log("Fade final");
			SetAlpha(_finalParent.GetChild(0).gameObject.GetComponent<Text>(), 0f);
			_finalParent.GetChild(0).gameObject.SetActive(true);
			yield return Fade(_finalParent.GetChild(0).gameObject, 1f, true);
			yield return new WaitForSeconds(3f);
			SetAlpha(_finalParent.GetChild(1).gameObject.GetComponent<Text>(), 0f);
			_finalParent.GetChild(1).gameObject.SetActive(true);
			_finalFadeCoro = StartCoroutine(Fade(_finalParent.GetChild(1).gameObject, 1f, true));

			if(_returnCoro == null) StartCoroutine(ReturnFromCredits());
		}
		
		public IEnumerator WaitForScrollEnd()
		{
			// get last scroll parent size
			var lastSection = _scrollParent.GetChild(_scrollParent.childCount - 1);
			// there are 2 children left and right side, get the right one
			var lastSectionMainText = lastSection.GetChild(lastSection.childCount - 1).GetComponent<RectTransform>();

			float headerSize = 100;
			float lastSectionHeight = lastSectionMainText.sizeDelta.y + headerSize; // we can use sizeDelta.y or rect.height
			var endingPos =
				lastSectionHeight *
				lastSectionMainText.lossyScale.y; // if lossyScale.y gives wrong value we can manually scale height 

			const float buffer = 10;
			Modding.Logger.Log(lastSectionMainText.transform.position.y + " " +  endingPos);
			while (lastSectionMainText.transform.position.y < endingPos + buffer)
			{
				yield return null;
			}
		}

		private IEnumerator ReturnFromCredits()
		{
			yield return new WaitUntil(() => InputHandler.Instance.inputActions.jump.IsPressed);

			Log("Ending credits sequence, going to reward room");

			if(_creditsCoro != null) StopCoroutine(_creditsCoro);
			if(_finalFadeCoro != null) StopCoroutine(_finalFadeCoro);

			FiveKnights.Instance.SaveSettings.HasSeenCredits = true;

			StartCoroutine(Fade(_finalParent.parent.gameObject, 1f, false));
			StartCoroutine(FadeAudio(2f));
			yield return new WaitForSeconds(2f);

			HeroController.instance.EnterWithoutInput(true);
			GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
			{
				SceneName = "hidden_reward_room",
				EntryGateName = "door1",
				Visualization = GameManager.SceneLoadVisualizations.Default,
				WaitForSceneTransitionCameraFade = false,
				PreventCameraFadeOut = true,
				EntryDelay = 0,
				HeroLeaveDirection = GatePosition.door
			});
			InputHandler.Instance.AllowPause();
		}

		private IEnumerator FadeInOut(GameObject go, float timeUp, float timeStay, float timeDown)
		{
			foreach(Text text in go.GetComponentsInChildren<Text>(true))
			{
				SetAlpha(text, 0f);
			}
			foreach(Image image in go.GetComponentsInChildren<Image>(true))
			{
				SetAlpha(image, 0f);
			}
			go.SetActive(true);
			yield return Fade(go, timeUp, true);
			yield return new WaitForSeconds(timeStay);
			yield return Fade(go, timeDown, false);
			go.SetActive(false);
		}

		private IEnumerator Fade(GameObject go, float time, bool fadeIn)
		{
			float alphaChangeCounter = 0;

			Graphic[] texts = go.GetComponentsInChildren<Text>(true).Cast<Graphic>().ToArray();
			Graphic[] sprites = go.GetComponentsInChildren<Image>(true).Cast<Graphic>().ToArray();

			Graphic[] graphics = texts.Concat(sprites).ToArray();

			while(alphaChangeCounter < 1f)
			{
				foreach(Graphic graphic in graphics)
				{
					if(fadeIn)
					{
						SetAlpha(graphic, graphic.color.a + Time.deltaTime / time);
					}
					else
					{
						SetAlpha(graphic, graphic.color.a - Time.deltaTime / time);
					}
				}

				alphaChangeCounter += Time.deltaTime / time;
				yield return null;
			}
		}

		private IEnumerator FadeAudio(float time)
		{
			float changeCounter = 0f;
			while(changeCounter < 1f)
			{
				_creditsAudio.volume -= Time.deltaTime / time;
				yield return null;
			}
		}

		private void SetAlpha(Graphic t, float a) => t.color = new Color(t.color.r, t.color.g, t.color.b, a);

		private void Log(object o) => Modding.Logger.Log("[Credits Controller] " + o);
	}
}
