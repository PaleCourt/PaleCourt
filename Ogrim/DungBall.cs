using System.Collections;
using FiveKnights.Isma;
using UnityEngine;

namespace FiveKnights.Ogrim
{
    internal class DungBall : MonoBehaviour
    {
        private bool _hit;
        public GameObject particles;
        public bool usingThornPillars;
        
        private void FixedUpdate()
        {
            if (!_hit && gameObject.transform.GetPositionY() < 7.4f)
            {
                if(EnemyPlantSpawn.PillarCount < EnemyPlantSpawn.MAXPILLAR && 
                    transform.position.x > 67f && transform.position.x < 85f && !usingThornPillars)
				{
                    GameObject pillar = Instantiate(FiveKnights.preloadedGO["Plant"]);
                    pillar.name = "PillarEnemy";
                    pillar.transform.position = new Vector2(transform.position.x, 6.1f);
                    pillar.AddComponent<EnemyPlantSpawn.PillarMinion>();
                }
                StartCoroutine(DelayedKill());
                _hit = true;
                particles.transform.position = transform.position;
                particles.GetComponent<ParticleSystem>().Play();
            }
        }

        private IEnumerator DelayedKill()
        {
            yield return new WaitForSeconds(1.5f); 
            Destroy(gameObject);
        }

        private IEnumerator SpawnDungPillar(Vector2 pos)
        {
            pos.y = 7.4f;
            GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
            for (int i = 0; i < 3; i++)
            {
                var DungPill1 = Instantiate(FiveKnights.preloadedGO["pillar"]);
                var DungPill2 = Instantiate(FiveKnights.preloadedGO["pillar"]);
                DungPill1.transform.localScale *= 0.5f;
                DungPill2.transform.localScale *= 0.5f;
                Vector3 spikeSc2 = DungPill2.transform.localScale;
                Destroy(DungPill1.LocateMyFSM("Control"));
                Destroy(DungPill2.LocateMyFSM("Control"));
                DungPill1.SetActive(true);
                DungPill2.SetActive(true);
                DungPill1.AddComponent<DungPillar>();
                DungPill2.AddComponent<DungPillar>();
                DungPill1.transform.SetPosition2D(pos.x + (i * 0.8f) + .7f, pos.y + 1.5f);
                DungPill2.transform.SetPosition2D(pos.x - (i * 0.8f) - .7f, pos.y + 1.5f);
                DungPill2.transform.localScale = new Vector3(-1f * spikeSc2.x, spikeSc2.y, spikeSc2.z);
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}