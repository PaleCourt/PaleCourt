namespace FiveKnights.Isma
{
    using UnityEngine;

    public class ShadeOnlyPass : MonoBehaviour
    {
        public Collider2D disableCollider;
        private bool unlocked;
        private EventRegister eventRegister;

        private void Awake() => this.eventRegister = this.GetComponent<EventRegister>();

        private void Start()
        {
            if ((bool) (Object) this.eventRegister)
                this.eventRegister.OnReceivedEvent += new EventRegister.RegisteredEvent(this.Setup);
            this.Setup();
        }

        private void OnDestroy()
        {
            if (!(bool) (Object) this.eventRegister)
                return;
            this.eventRegister.OnReceivedEvent -= new EventRegister.RegisteredEvent(this.Setup);
        }

        private void Setup()
        {
            if (!GameManager.instance.playerData.GetBool("hasShadowDash"))
                return;
            this.unlocked = true;
        }

        private void FixedUpdate()
        {
            if (!this.unlocked)
                return;
            if (HeroController.instance.cState.shadowDashing && this.disableCollider.enabled)
                this.disableCollider.enabled = false;
            if (HeroController.instance.cState.shadowDashing || this.disableCollider.enabled)
                return;
            this.disableCollider.enabled = true;
        }
    }
}