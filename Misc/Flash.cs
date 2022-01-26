using UnityEngine;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace FiveKnights
{
    public class Flash : MonoBehaviour
    {
        private Task _lastTask;
        private CancellationTokenSource ts;
        private CancellationToken ct;
        private SpriteRenderer _sr;
        private bool _flashing;

        private void Start()
        {
            _sr = gameObject.GetComponent<SpriteRenderer>();
            _sr.material = FiveKnights.Materials["flash"];
            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            On.SpellFluke.DoDamage += SpellFlukeOnDoDamage;
            ts = new CancellationTokenSource();
            ct = ts.Token;
        }

        private void HealthManager_TakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.gameObject == gameObject)
            {
                if (_lastTask == null || _lastTask.IsCompleted)
                {
                    _lastTask = FlashWhite2();
                }
                else
                {
                    ts.Cancel();
                    ts = new CancellationTokenSource();
                    ct = ts.Token;
                    _lastTask = FlashWhite2();
                }

                _flashing = true;
            }
            orig(self, hitInstance);
        }
        
        private void SpellFlukeOnDoDamage(On.SpellFluke.orig_DoDamage orig, SpellFluke self, GameObject tar, 
            int upwardrecursionamount, bool burst)
        {
            if (tar == gameObject)
            {
                if (_lastTask == null || _lastTask.IsCompleted)
                {
                    _lastTask = FlashWhite2();
                }
                else
                {
                    ts.Cancel();
                    ts = new CancellationTokenSource();
                    ct = ts.Token;
                    _lastTask = FlashWhite2();
                }

                _flashing = true;
            }
            orig(self, tar, upwardrecursionamount, burst);
        }
        
        async Task FlashWhite2()
        {
            _sr.material.SetFloat("_FlashAmount", 1f);
            for (float i = 1f; i >= 0f; i -= 0.02f)
            {
                await Task.Delay(1, ct);
                _sr.material.SetFloat("_FlashAmount", i);
            }

            _flashing = false;
        }

        IEnumerator FlashWhite()
        {
            _sr.material.SetFloat("_FlashAmount", 1f);
            yield return null;
            for (float i = 1f; i >= 0f; i -= 0.01f)
            {
                _sr.material.SetFloat("_FlashAmount", i);
                yield return null;
            }
            yield return null;
            //flashing = false;
        }

        private void OnDestroy()
        {
            On.HealthManager.TakeDamage -= HealthManager_TakeDamage;
            On.SpellFluke.DoDamage -= SpellFlukeOnDoDamage;
        }
    }
}
