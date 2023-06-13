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
		private readonly float ScrollSpeed = 290f * (Screen.height / 2400f);
		private readonly float ScrollY = 16500f * (Screen.height / 2400f);

		private Transform _creditsParent;
		private Transform _scrollParent;
		private Transform _thanksParent;
		private Transform _finalParent;
		private AudioSource _creditsAudio;

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
			FixFonts();
			StopAudio();
			StartCoroutine(RollCredits());
		}

		private void Update()
		{
			if(_scrolling)
			{
				if(_scrollParent.position.y < ScrollY)
				{
					_scrollParent.Translate(Vector3.up * Time.deltaTime * ScrollSpeed);
				}
				else
				{
					_scrolling = false;
				}
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

			if(FiveKnights.Instance.SaveSettings.HasSeenCredits) _returnCoro = StartCoroutine(ReturnFromCredits());

			yield return new WaitForSeconds(1f);
			_creditsAudio.clip = ABManager.AssetBundles[ABManager.Bundle.Sound].LoadAsset<AudioClip>("CreditsMusic");
			_creditsAudio.outputAudioMixerGroup = HeroController.instance.GetComponent<AudioSource>().outputAudioMixerGroup;
			_creditsAudio.Play();

			Log("Fade main credits");
			for(int i = 0; i < _creditsParent.childCount; i++)
			{
				yield return FadeInOut(_creditsParent.GetChild(i).gameObject, 1f, 5.5f, 1f);
				yield return new WaitForSeconds(0.5f);
			}

			Log("Scroll credits");
			_scrolling = true;
			yield return new WaitWhile(() => _scrolling);

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
			StartCoroutine(Fade(_finalParent.GetChild(1).gameObject, 1f, true));

			if(_returnCoro == null) StartCoroutine(ReturnFromCredits());
		}

		private IEnumerator ReturnFromCredits()
		{
			yield return new WaitUntil(() => InputHandler.Instance.inputActions.jump.IsPressed);

			Log("Ending credits sequence, going to reward room");

			FiveKnights.Instance.SaveSettings.HasSeenCredits = true;

			StartCoroutine(Fade(_finalParent.parent.gameObject, 1f, false));
			//StartCoroutine(Fade(_finalParent.parent.gameObject, 1f, false));
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
