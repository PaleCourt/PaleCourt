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
		private Transform creditsParent;
		private AudioSource creditsAudio;

		private void Start()
		{
			GameManager.instance.SetState(GameState.CUTSCENE);
			UIManager.instance.SetState(UIState.CUTSCENE);
			InputHandler.Instance.PreventPause();

			creditsParent = GameObject.Find("Credits Parent").transform;
			creditsAudio = GameObject.Find("Credits Audio").GetComponent<AudioSource>();
			FixFonts();
			StopAudio();
			StartCoroutine(RollCredits());
		}

		private void FixFonts()
		{
			foreach(Text text in creditsParent.GetComponentsInChildren<Text>(true))
			{
				if(text.gameObject.name.Contains("Title")) text.font = CanvasUtil.TrajanNormal;
				else text.font = CanvasUtil.GetFont("Perpetua");
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

			yield return new WaitForSeconds(1f);
			creditsAudio.clip = ABManager.AssetBundles[ABManager.Bundle.Sound].LoadAsset<AudioClip>("CreditsMusic");
			creditsAudio.Play();

			for(int i = 0; i < creditsParent.childCount; i++)
			{
				yield return FadeInOut(creditsParent.GetChild(i).gameObject, 1f, 5f, 1f);
			}

			Log("Ending credits sequence, going to reward room");

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
				creditsAudio.volume -= Time.deltaTime / time;
				yield return null;
			}
		}

		private void SetAlpha(Graphic t, float a) => t.color = new Color(t.color.r, t.color.g, t.color.b, a);

		private void Log(object o) => Modding.Logger.Log("[Credits Controller] " + o);
	}
}
