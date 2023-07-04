using UnityEngine;

namespace FiveKnights
{
    public class Dagger : MonoBehaviour
    {
        private int damage => upgraded ? 25 : 13;
        public bool upgraded;

        private void OnTriggerEnter2D(Collider2D collider)
        {
            if(collider.gameObject.layer == 11)
            {
                HitInstance smallShotHit = new HitInstance();
                smallShotHit.DamageDealt = damage;
                smallShotHit.AttackType = AttackTypes.Spell;
                smallShotHit.IgnoreInvulnerable = true;
                smallShotHit.Source = gameObject;
                smallShotHit.Multiplier = 1f;
                HealthManager hm = collider.gameObject.GetComponent<HealthManager>();
                hm.Hit(smallShotHit);
            }
        }

        private void Log(object message) => Modding.Logger.Log("[Dagger] " + message);
    }
}