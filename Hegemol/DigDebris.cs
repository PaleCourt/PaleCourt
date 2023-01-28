using UnityEngine;

namespace FiveKnights.Hegemol
{
    public class DigDebris : MonoBehaviour
    {
        private Rigidbody2D _rb;
		private bool hitWall;
		private float rotSpeed;

        public Vector2 vel;
        public float WallX;
        public float GroundY;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
		{
            _rb.velocity = vel;
			transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
			rotSpeed = Random.Range(400f, 600f);
		}

        private void FixedUpdate()
        {
            Vector3 rot = transform.rotation.eulerAngles;
            rot.z += rotSpeed * Time.fixedDeltaTime;
            transform.rotation = Quaternion.Euler(rot.x, rot.y, rot.z);

			if(vel.x < 0f)
			{
				if(!hitWall && transform.position.x < WallX)
				{
					_rb.velocity = new Vector2(-vel.x, vel.y);
					hitWall = true;
				}
			}
			else
			{
				if(!hitWall && transform.position.x > WallX)
				{
					_rb.velocity = new Vector2(-vel.x, vel.y);
					hitWall = true;
				}
			}

			if(transform.position.y < GroundY - 5f && hitWall) Destroy(gameObject);
		}
    }
}