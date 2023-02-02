using System.Collections;
using UnityEngine;

namespace FiveKnights.Hegemol
{
	public class Debris : MonoBehaviour
	{
		private float rotSpeed;

		public Rigidbody2D rb;
		public float gravityScale;
		public Vector2 vel;
		public float GroundY;

		private void Awake()
		{
			rb = GetComponent<Rigidbody2D>();
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
			Vector3 rot = transform.rotation.eulerAngles;
			rot.z += rotSpeed * Time.fixedDeltaTime;
			transform.rotation = Quaternion.Euler(rot.x, rot.y, rot.z);

			if(transform.position.y < GroundY - 5f) Destroy(gameObject);
		}
	}
}