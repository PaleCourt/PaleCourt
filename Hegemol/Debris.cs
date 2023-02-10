using HutongGames.PlayMaker.Actions;
using System.Collections;
using UnityEngine;

namespace FiveKnights.Hegemol
{
	public class Debris : MonoBehaviour
	{
		private float rotSpeed;
		private MusicPlayer _ap;
		private ParticleSystem _pt;
		private bool hitGround;

		public Rigidbody2D rb;
		public float gravityScale;
		public Vector2 vel;
		public float GroundY;

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
				rb.velocity = Vector2.zero;
				transform.rotation = Quaternion.Euler(0f, 0f, 0f);
				Destroy(GetComponent<CircleCollider2D>());
				_ap.Clip = FiveKnights.Clips["HegDebris"];
				_ap.DoPlayRandomClip();
				_pt.Play();
				foreach(SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
				{
					sr.enabled = false;
				}
				StartCoroutine(WaitToDestroy());
			}
		}

		private IEnumerator WaitToDestroy()
		{
			yield return new WaitForSeconds(3f);
			Destroy(gameObject);
		}
	}
}