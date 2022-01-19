using System;
using System.Collections;
using System.Collections.Generic;
using FiveKnights.BossManagement;
using FiveKnights.Misc;
using HutongGames.PlayMaker.Actions;
using SFCore.Utils;
using UnityEngine;

namespace FiveKnights.Isma
{
    public class EnemyPlantSpawn : MonoBehaviour
    {
        private SpriteRenderer _sr;
        public static bool isPhase2;
        public static int FoolCount = 0;
        public static int PillarCount = 0;
        public static int TurretCount = 0;
        public static readonly int MaxTurret = 3;
        public static readonly int MaxFool = 5;
        private const int MaxPillar = 3;
        private const float TIME_INC = 0.1f;
        private readonly float LEFT_X = (OWArenaFinder.IsInOverWorld) ? 105f : 60.3f;
        private readonly float RIGHT_X = (OWArenaFinder.IsInOverWorld) ? 135f : 90.6f;
        private static readonly float MIDDDLE = (OWArenaFinder.IsInOverWorld) ? 120 : 75f;
        private readonly float GROUND_Y = 6.05f;
        private const String FoolName = "FoolEnemy";
        private const String SpecialName = "SpecialEnemy";
        private const String GulkaName = "TurretEnemy";
        private const String PillarName = "PillarEnemy";

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.name != "SeedEnemy") return;
            if (name.Contains("Floor"))
            {
                if (isPhase2)
                {
                    Log($"Working with Pillars {PillarCount}");
                    if (PillarCount >= MaxPillar)
                    {
                        Log("Oh no too many");
                        Destroy(other.gameObject);
                        return;
                    }
                    SpawnPillar(other.transform.position);
                }
                else
                {
                    Log($"Working with Fools {FoolCount}");
                    if (FoolCount >= MaxFool)
                    {
                        Log("Oh no too many");
                        Destroy(other.gameObject);
                        return;
                    }
                    SpawnFool(other.transform.position);
                }
            }
            else if (name.Contains("Side") && !isPhase2)
            {
                Log($"Working with Gulka {TurretCount}");
                if (TurretCount >= MaxTurret)
                {
                    Log("Oh no too many");
                    Destroy(other.gameObject);
                    return;
                }
                SpawnGulka(other.transform.position);
            }
            Destroy(other.gameObject);
        }

        public IEnumerator Phase2Spawn(GameObject go)
        {
            Vector2 pos = go.transform.position;
            yield return new WaitForSeconds(2f);
            GameObject gulk = Instantiate(FiveKnights.preloadedGO["Gulka"]);
            gulk.name = SpecialName;
            Animator anim = gulk.GetComponent<Animator>();
            float rot = 180f;
            gulk.transform.SetPosition2D(pos);
            gulk.transform.localScale *= 1.4f;
            gulk.transform.SetRotation2D(rot);
            GameObject turret = Instantiate(FiveKnights.preloadedGO["PTurret"]);
            var hm = turret.GetComponent<HealthManager>();
            RemoveGeo(hm);
            turret.name = SpecialName;
            MeshRenderer mesh = turret.GetComponent<MeshRenderer>();
            PlayMakerFSM fsm = turret.LocateMyFSM("Plant Turret");
            
            // stop spike ball from doing damage to enemy
            var ball = fsm.GetAction<CreateObject>("Fire", 3).gameObject.Value;
            ball.layer = 11;

            fsm.GetAction<SetInvincible>("Wake", 2).Invincible = true; 
            hm.IsInvincible = true;
            
            mesh.enabled = false;
            List<MeshRenderer> lst = new List<MeshRenderer>();
            foreach (MeshRenderer i in turret.GetComponentsInChildren<MeshRenderer>(true))
            {
                i.enabled = false;
                lst.Add(i);
            }
            fsm.FsmVariables.FindFsmFloat("Distance Max").Value = 50f;
            fsm.GetAction<Wait>("Idle Anim", 1).time = 0.8f;
            turret.transform.SetRotation2D(rot);
            turret.SetActive(true);
            rot *= Mathf.Deg2Rad;
            turret.transform.SetPosition2D(pos.x - 0.5f * Mathf.Sin(rot), pos.y + 0.5f * Mathf.Cos(rot)); //90: x-0.5, -90: x+0.5, 180f: y-0.5
            anim.Play("SpawnGulka");
            yield return new WaitForSeconds(0.05f);
            yield return new WaitWhile(() => anim.IsPlaying());
            Destroy(gulk);
            mesh.enabled = true;
            foreach (MeshRenderer i in lst)
            {
                i.enabled = true;
            }
            fsm.SendEvent("SEE HERO");
            StartCoroutine(FinalGulkaKiller(turret));
        }

        private IEnumerator FinalGulkaKiller(GameObject gulka)
        {
            yield return new WaitWhile(() => !IsmaController.eliminateMinions);
            Log("Killing our special gulka :(");
            gulka.GetComponent<HealthManager>().Die(new float?(0f), AttackTypes.Nail, true);
        }

        private void SpawnPillar(Vector2 pos)
        {
            GameObject pillar = Instantiate(FiveKnights.preloadedGO["Plant"]);
            pillar.name = PillarName;
            pillar.transform.position = new Vector2(pos.x, 6.1f);
            pillar.AddComponent<PillarMinion>();
        }

        public class PillarMinion : MonoBehaviour
        {
            private bool dying;
            private void Awake()
            {
                gameObject.AddComponent<PlantCtrl>().IsmaFight = true;
                PillarCount++;
                IsmaController.offsetTime += TIME_INC;
            }

            private IEnumerator Start()
            {
                yield return new WaitWhile(() => GetComponent<HealthManager>() == null);
                GetComponent<HealthManager>().OnDeath += () =>
                {
                    dying = true;
                    StartCoroutine(PillarDeath());
                };
            }

            private IEnumerator PillarDeath()
            {
                if (transform.Find("PillarPogo") != null) Destroy(transform.Find("PillarPogo").gameObject);
                Animator anim = GetComponent<Animator>();
                anim.Play("PlantDie");
                yield return null;
                yield return new WaitWhile(() => anim.IsPlaying());
                PillarCount--;
                IsmaController.offsetTime -= TIME_INC;
                Destroy(gameObject);
            }

            private void Update()
            {
                if (!IsmaController.eliminateMinions || dying) return;
                dying = true;
                StartCoroutine(PillarDeath());
            }
        }
        
        private void SpawnFool(Vector2 pos)
        {
            GameObject parent = new GameObject("FoolParent");
            parent.transform.position = pos;
            GameObject initFool = Instantiate(FiveKnights.preloadedGO["Fool"], parent.transform, true);
            initFool.name = "init" + FoolName;
            initFool.SetActive(false);
            GameObject finalFool = Instantiate(FiveKnights.preloadedGO["PTrap"], parent.transform, true);
            finalFool.name = "final" + FoolName;
            finalFool.SetActive(false);
            parent.AddComponent<FoolMinion>();
        }

        public class FoolMinion : MonoBehaviour
        {
            private GameObject initFool;
            private GameObject finalFool;
            private HealthManager hm;
            private float xPos;
            private void Awake()
            {
                initFool = transform.Find("init" + FoolName).gameObject;
                finalFool = transform.Find("final" + FoolName).gameObject;
                hm = finalFool.GetComponent<HealthManager>();
                RemoveGeo(hm);
                xPos = transform.position.x;
                FoolCount++;
                IsmaController.offsetTime += TIME_INC;
                hm.OnDeath += () =>
                {
                    IsmaController.offsetTime -= TIME_INC;
                    FoolCount--;
                };
            }

            private IEnumerator Start()
            {
                initFool.SetActive(true);
                Animator anim = initFool.GetComponent<Animator>();
                initFool.transform.SetPosition2D(xPos, 6.05f); //6.2
                initFool.transform.localScale *= 1.4f;
                yield return anim.PlayBlocking("SpawnFool");
                Destroy(initFool);
                finalFool.transform.SetPosition2D(xPos, 8.65f); //8.8
                tk2dSpriteAnimator tk = finalFool.GetComponent<tk2dSpriteAnimator>();
                PlayMakerFSM fsm = finalFool.LocateMyFSM("Plant Trap Control");
                fsm.enabled = false;
                finalFool.SetActive(true);
                tk.Play("Retract");
                // Doing this to stop them from doing damage when they first spawn
                yield return new WaitWhile(() =>
                {
                    var f = finalFool.GetComponent<BoxCollider2D>();
                    if (f != null) f.enabled = false;
                    return tk.IsPlaying("Retract");
                });
                fsm.enabled = true;
                fsm.SetState("Init");
                fsm.GetAction<Wait>("Ready", 2).time = 0.4f;
            }

            private void Update()
            {
                if (!IsmaController.killAllMinions) return;
                Destroy(gameObject);
            }

            private void OnDestroy()
            {
                if (finalFool != null && hm != null)
                {
                    finalFool.transform.parent = null;
                    hm.Die(new float?(0f), AttackTypes.Nail, true);
                }
            }
        }
        
        private void SpawnGulka(Vector2 pos)
        {
            GameObject parent = new GameObject("GulkaParent");
            parent.transform.position = pos;
            GameObject initGulka = Instantiate(FiveKnights.preloadedGO["Gulka"], parent.transform, true);
            initGulka.name = "init" + GulkaName;
            initGulka.SetActive(false);
            GameObject finalGulka = Instantiate(FiveKnights.preloadedGO["PTurret"]);
            finalGulka.name = "final" + GulkaName;
            parent.AddComponent<GulkaMinion>().finalGulka = finalGulka;
        }
        
        public class GulkaMinion : MonoBehaviour
        {
            private GameObject initGulka;
            public GameObject finalGulka;
            private HealthManager hm;
            private Vector2 pos;
            private void Awake()
            {
                initGulka = transform.Find("init" + GulkaName).gameObject;
                pos = transform.position;
                TurretCount++;
                IsmaController.offsetTime += TIME_INC;
            }

            private IEnumerator Start()
            {
                hm = finalGulka.GetComponent<HealthManager>();
                RemoveGeo(hm);
                hm.OnDeath += () =>
                {
                    IsmaController.offsetTime -= TIME_INC;
                    TurretCount--;
                    Destroy(gameObject);
                };
                
                initGulka.SetActive(true);
                Animator anim = initGulka.GetComponent<Animator>();
                float rot = pos.x > MIDDDLE ? 90f : -90f;
                initGulka.transform.SetPosition2D(pos.x + (pos.x > MIDDDLE ? 0f : -0.3f), pos.y);
                initGulka.transform.localScale *= 1.4f;
                initGulka.transform.SetRotation2D(rot);
                MeshRenderer mesh = finalGulka.GetComponent<MeshRenderer>();
                PlayMakerFSM fsm = finalGulka.LocateMyFSM("Plant Turret");
                
                // stop spike ball from doing damage to enemy
                var ball = fsm.GetAction<CreateObject>("Fire", 3).gameObject.Value;
                ball.layer = 11;

                mesh.enabled = false;
                List<MeshRenderer> lst = new List<MeshRenderer>();
                foreach (MeshRenderer i in finalGulka.GetComponentsInChildren<MeshRenderer>(true))
                {
                    i.enabled = false;
                    lst.Add(i);
                }
                fsm.FsmVariables.FindFsmFloat("Distance Max").Value = 50f;
                finalGulka.transform.SetRotation2D(rot);
                finalGulka.SetActive(true);
                rot *= Mathf.Deg2Rad;
                finalGulka.transform.SetPosition2D(pos.x, pos.y + 0.5f * Mathf.Cos(rot)); 
                anim.Play("SpawnGulka");
                yield return new WaitForSeconds(0.05f);
                yield return new WaitWhile(() => anim.IsPlaying());
                Destroy(initGulka);
                mesh.enabled = true;
                foreach (MeshRenderer i in lst)
                {
                    i.enabled = true;
                }
                fsm.SendEvent("SEE HERO");
            }

            private void Update()
            {
                if (!IsmaController.killAllMinions) return;
                Destroy(gameObject);
            }

            private void OnDestroy()
            {
                if (hm != null)
                {
                    hm.Die(new float?(0f), AttackTypes.Nail, true);
                }
            }
        }

        private static void RemoveGeo(HealthManager hm)
        {
            hm.SetGeoLarge(0);
            hm.SetGeoMedium(0);
            hm.SetGeoSmall(0);
        }

        private static void Log(object obj)
        {
            if (!FiveKnights.isDebug) return;
            Modding.Logger.Log("[Enemy Plant] " + obj);
        }
    }
}
