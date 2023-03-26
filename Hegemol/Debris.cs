using HutongGames.PlayMaker.Actions;
using System.Collections;
using UnityEngine;

namespace FiveKnights.Hegemol
{
	public class Debris : MonoBehaviour
	{
		public enum DebrisType
		{
			NORMAL,
			CC,
			DUNG
		}

		private float rotSpeed;
		private MusicPlayer _ap;
		private ParticleSystem _pt;
		private bool hitGround;

		public Rigidbody2D rb;
		public float gravityScale;
		public Vector2 vel;
		public float GroundY;
		public DebrisType type;

		private void Awake()
		{
			rb = GetComponent<Rigidbody2D>();
			PlayMakerFSM spellControl = HeroController.instance.gameObject.LocateMyFSM("Spell Control");
			GameObject fireballParent = spellControl.GetAction<SpawnObjectFromGlobalPool>("Fireball 2", 3).gameObject.Value;
			PlayMakerFSM fireballCast = fireballParent.LocateMyFSM("Fireball Cast");
			GameObject actor = fireballCast.GetAction<AudioPlayerOneShotSingle>("Cast Right", 3).audioPlayer.Value;
			_ap = new MusicPlayer
			{
				Volume = 1f,
				Player = actor,
				MaxPitch = 1f,
				MinPitch = 1f,
				Spawn = gameObject
			};
			_pt = transform.Find("Particle System").gameObject.GetComponent<ParticleSystem>();
		}

		private void OnEnable()
		{
			rb.gravityScale = gravityScale;
			transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
			rotSpeed = (Random.Range(0, 2) == 0 ? -1 : 1) * Random.Range(200f, 400f);
			rb.velocity = vel;
			switch(type)
			{
				case DebrisType.NORMAL:
					transform.Find("Debris" + Random.Range(0, 3)).gameObject.SetActive(true);
					_ap.Clip = FiveKnights.Clips["HegDebris"];
					break;
				case DebrisType.CC:
					transform.Find("Debris" + Random.Range(3, 6)).gameObject.SetActive(true);
					_ap.Clip = FiveKnights.Clips["HegDebris"];
					ParticleSystem.MainModule ptMain = _pt.main;
					ptMain.startColor = Color.white;
					break;
				case DebrisType.DUNG:
					transform.Find("DebrisDung").gameObject.SetActive(true);
					_ap.Clip = FiveKnights.Clips["HegDungDebris"];
					GameObject particles = Instantiate(FiveKnights.preloadedGO["DungBreakChunks"], transform);
					_pt = particles.GetComponent<ParticleSystem>();
					break;
			}
		}

		private void FixedUpdate()
		{
			if(transform.position.y > GroundY - 0.4f && !hitGround)
			{
				Vector3 rot = transform.rotation.eulerAngles;
				rot.z += rotSpeed * Time.fixedDeltaTime;
				transform.rotation = Quaternion.Euler(rot.x, rot.y, rot.z);
			}
			else if(!hitGround)
			{
				hitGround = true;
				rb.gravityScale = 0f;
				rb.velocity = Vector2.zero;
				transform.rotation = Quaternion.Euler(0f, 0f, 0f);
				Destroy(GetComponent<CircleCollider2D>());
				_ap.DoPlayRandomClip();
				_pt.Play();
				foreach(SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
				{
					sr.enabled = false;
				}
				if(type == DebrisType.DUNG)
				{
					Destroy(_pt.gameObject, 3f);
				}
				Destroy(gameObject, 3f);
			}
		}
	}
}