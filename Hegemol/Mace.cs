using UnityEngine;

namespace FiveKnights.Hegemol
{
    public class Mace : MonoBehaviour
    {
        public float LaunchSpeed = 45f;
        public float SpinSpeed = -300f;

        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }
        
        private void OnEnable()
        {
            _rb.velocity = Vector3.up * LaunchSpeed;
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }

        private void FixedUpdate()
        {
            Vector3 rot = transform.rotation.eulerAngles;
            rot.z += SpinSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Euler(rot.x, rot.y, rot.z);
        }
    }
}