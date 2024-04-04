using UnityEngine;

namespace FiveKnights
{
    public class Dagger : MonoBehaviour
    {
        private const int DaggerDamage = 13;
        private const int DaggerDamageUpgraded = 25;
        public bool upgraded;

        private void OnTriggerEnter2D(Collider2D collider)
        {
            int layer = collider.gameObject.layer;
            if(layer == 20 || layer == 9 || layer == 26 || layer == 31 || collider.CompareTag("Geo"))
            {
                return;
            }
            HitInstance smallShotHit = new HitInstance();
            smallShotHit.DamageDealt = upgraded ? DaggerDamageUpgraded : DaggerDamage;
            smallShotHit.AttackType = AttackTypes.Spell;
            smallShotHit.IgnoreInvulnerable = true;
            smallShotHit.Source = gameObject;
            smallShotHit.Multiplier = 1f;
            //HealthManager hm = collider.gameObject.GetComponent<HealthManager>();
            //if(hm != null) hm.Hit(smallShotHit);

            HitTaker.Hit(collider.gameObject, smallShotHit, 3);
        }

        private void Log(object message) => Modding.Logger.Log("[Dagger] " + message);
    }
}