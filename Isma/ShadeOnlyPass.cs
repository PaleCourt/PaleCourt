namespace FiveKnights.Isma
{
    using UnityEngine;

    public class ShadeOnlyPass : MonoBehaviour
    {
        public Collider2D disableCollider;
        public bool colliderEnabled;
        private bool unlocked;
        private EventRegister eventRegister;

        private void Awake() => eventRegister = GetComponent<EventRegister>();

        private void Start()
        {
            if ((bool) (Object) eventRegister)
                eventRegister.OnReceivedEvent += new EventRegister.RegisteredEvent(Setup);
            Setup();
        }

        private void OnDestroy()
        {
            if (!(bool)eventRegister)
                return;
            eventRegister.OnReceivedEvent -= new EventRegister.RegisteredEvent(Setup);
        }

        private void Setup()
        {
            if (!GameManager.instance.playerData.GetBool("hasShadowDash"))
                return;
            unlocked = true;
        }

        private void FixedUpdate()
        {
            if (!unlocked || disableCollider == null)
                return;
            if (HeroController.instance.cState.shadowDashing && disableCollider.enabled)
                disableCollider.enabled = false;
            if (HeroController.instance.cState.shadowDashing || disableCollider.enabled)
                return;
            disableCollider.enabled = colliderEnabled;
        }
    }
}