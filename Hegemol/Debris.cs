using System.Collections;
using UnityEngine;

namespace FiveKnights.Hegemol
{
	public class Debris : MonoBehaviour
	{
		private Rigidbody2D _rb;
		private float rotSpeed;

		public float delay;
		public Vector2 vel;
		public float GroundY;

		private void Awake()
		{
			_rb = GetComponent<Rigidbody2D>();
		}

		private void OnEnable()
		{
			_rb.gravityScale = 0.4f;
			transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
			rotSpeed = (Random.Range(0, 2) == 0 ? -1 : 1) * Random.Range(200f, 400f);
			StartCoroutine(WaitToMove());
		}

		private IEnumerator WaitToMove()
		{
			yield return new WaitForSeconds(delay);
			_rb.velocity = vel;
		}

		private void FixedUpdate()
		{
			Vector3 rot = transform.rotation.eulerAngles;
			rot.z += rotSpeed * Time.fixedDeltaTime;
			transform.rotation = Quaternion.Euler(rot.x, rot.y, rot.z);

			if(transform.position.y < GroundY - 5f) Destroy(gameObject);
		}
	}
}