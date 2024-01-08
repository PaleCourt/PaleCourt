using UnityEngine;

namespace FiveKnights.Misc
{
    public class SuperDashCancel : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            HeroController.instance.CancelSuperDash();
            Destroy(gameObject);
        }
    }
}