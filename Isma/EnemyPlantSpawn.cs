using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FiveKnights.BossManagement;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using SFCore.Utils;
using Vasi;
using Random = UnityEngine.Random;

namespace FiveKnights.Isma
{
    public class EnemyPlantSpawn : MonoBehaviour
    {
        public static bool isPhase2;
        public static int FoolCount = 0;
        public static int PillarCount = 0;
        public static int TurretCount = 0;
        public const int MAX_TURRET = 2;
        public const int MAX_FOOL = 3;
        public const int MAX_PILLAR = 3;
        private List<GameObject> foolList = new List<GameObject>();

        private const float TIME_INC = 0.1f;
        private readonly float LEFT_X = OWArenaFinder.IsInOverWorld ? 105f : 60.3f;
        private readonly float RIGHT_X = OWArenaFinder.IsInOverWorld ? 135f : 90.6f;
        private readonly float MIDDLE = OWArenaFinder.IsInOverWorld ? 120f : 75f;
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
                    if (PillarCount >= MAX_PILLAR)
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
                    if (FoolCount >= MAX_FOOL)
                    {
                        Log("Oh no too many");
                        Destroy(other.gameObject);
                        return;
                    }
                    for(int i = 0; i < foolList.Count; i++)
                    {
                        if(foolList[i] == null)
                        {
                            foolList.RemoveAt(i);
                            i--;
                            continue;
                        }
                        if(Mathf.Abs(foolList[i].transform.position.x - other.transform.position.x) < 2f)
                        {
                            Log("Fool is too close to another fool");
                            Destroy(other.gameObject);
                            return;
                        }
                    }
                    SpawnFool(other.transform.position);
                }
            }
            else if (name.Contains("Side") && !isPhase2)
            {
                Log($"Working with Gulka {TurretCount}");
                if (TurretCount >= MAX_TURRET)
                {
                    Log("Oh no too many");
                    Destroy(other.gameObject);
                    return;
                }
                SpawnGulka(other.transform.position);
            }
            Destroy(other.gameObject);
        }

        public void Phase2Spawn()
        {
            GameObject turOrig1 = new GameObject();
            turOrig1.transform.position = new Vector2(LEFT_X + 10f, GROUND_Y + 14f);
            GameObject turOrig2 = new GameObject();
            turOrig2.transform.position = new Vector2(RIGHT_X - 10f, GROUND_Y + 14f);
            
            GameObject turret1 = Instantiate(FiveKnights.preloadedGO["PTurret"]);
            GameObject turret2 = Instantiate(FiveKnights.preloadedGO["PTurret"]);
            
            StartCoroutine(Phase2Spawn(turOrig1, turret1));
            StartCoroutine(Phase2Spawn(turOrig2, turret2));
            
            StartCoroutine(WaitForInitFinish(turret1.LocateMyFSM("Plant Turret"), turret2.LocateMyFSM("Plant Turret")));
        }

        private IEnumerator WaitForInitFinish(PlayMakerFSM fsm1, PlayMakerFSM fsm2)
        {
            string stopState = "Idle Anim";
            yield return new WaitUntil(() => _hasP2GulkaInit);
            while (fsm1.gameObject != null && fsm2.gameObject != null)
            {
                fsm1.enabled = fsm2.enabled = true;
                fsm1.SetState("Shoot Antic"); fsm2.SetState("Shoot Antic");
                
                yield return new WaitUntil(() => fsm1.ActiveStateName == stopState || fsm2.ActiveStateName == stopState);
                if (fsm1.ActiveStateName == stopState)
                {
                    fsm1.enabled = false;
                    fsm1.GetComponent<tk2dSpriteAnimator>().Play("Idle");
                    yield return new WaitUntil(() => fsm2.ActiveStateName == stopState);
                    fsm2.enabled = false;
                    fsm2.GetComponent<tk2dSpriteAnimator>().Play("Idle");
                }
                else
                {
                    fsm2.enabled = false;
                    fsm2.GetComponent<tk2dSpriteAnimator>().Play("Idle");
                    yield return new WaitUntil(() => fsm1.ActiveStateName == stopState);
                    fsm1.enabled = false;
                    fsm1.GetComponent<tk2dSpriteAnimator>().Play("Idle");
                }
                yield return new WaitForSeconds(2f + 0.1f * Random.Range(0, 4));
            }
        }

        private bool _hasP2GulkaInit;
        
        private IEnumerator Phase2Spawn(GameObject go, GameObject turret)
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

            GameObject seal = Instantiate(FiveKnights.preloadedGO["Seal"], gulk.transform.position + Vector3.down, Quaternion.identity);
            seal.transform.localScale = new Vector3(2f, 2f, 1f);
            seal.layer = (int)GlobalEnums.PhysLayers.INTERACTIVE_OBJECT;
            seal.AddComponent<GulkaSeal>();

            var hm = turret.GetComponent<HealthManager>();
            RemoveGeo(hm);
            turret.name = SpecialName;
            MeshRenderer mesh = turret.GetComponent<MeshRenderer>();
            PlayMakerFSM fsm = turret.LocateMyFSM("Plant Turret");
            
            // stop spike ball from doing damage to enemy
            var ball = fsm.GetAction<CreateObject>("Fire", 3).gameObject.Value;
            if (!ball.GetComponent<ModifiedSpit>())
            {
                ball.AddComponent<ModifiedSpit>();
            }

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
            fsm.GetAction<Wait>("Idle Anim", 1).time = 1.5f;
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
            fsm.enabled = false;
            _hasP2GulkaInit = true;
            StartCoroutine(FinalGulkaKiller(turret));
        }

        private IEnumerator FinalGulkaKiller(GameObject gulka)
        {
            yield return new WaitWhile(() => !IsmaController.eliminateMinions);
            Log("Killing our special gulka :(");
            gulka.GetComponent<HealthManager>().Die(new float?(0f), AttackTypes.Nail, true);
            List<tk2dSprite> sprites = new List<tk2dSprite>(FindObjectsOfType<tk2dSprite>());
            foreach(tk2dSprite sprite in sprites)
            {
                if(sprite.gameObject.name == "Cover" || sprite.gameObject.name == "Under")
                {
                    Destroy(sprite.gameObject);
                }
            }
        }

        private void SpawnPillar(Vector2 pos)
        {
            GameObject pillar = Instantiate(FiveKnights.preloadedGO["Plant"]);
            pillar.name = PillarName;
            pillar.transform.position = new Vector3(pos.x, 6.1f, 0.1f);
            pillar.AddComponent<PillarMinion>();
        }

        public class PillarMinion : MonoBehaviour
        {
            private bool dying;
            private void Awake()
            {
                gameObject.AddComponent<PlantCtrl>();
                PillarCount++;
                IsmaController.offsetTime += TIME_INC;
            }

            private IEnumerator Start()
            {
                yield return new WaitWhile(() => GetComponent<HealthManager>() == null);
                GetComponent<HealthManager>().OnDeath += () =>
                {
                    StartCoroutine(PillarDeath(0f));
                };
                if(!OWArenaFinder.IsInOverWorld && (transform.position.x < 60.3f || transform.position.x > 90.6f))
				{
                    GetComponent<HealthManager>().SendDeathEvent();
                }
                StartCoroutine(PillarDeath(15f));
            }

            private IEnumerator PillarDeath(float delay)
            {
                if(dying) yield break;
                yield return new WaitForSeconds(delay);
                if(dying) yield break;
                dying = true;
                Destroy(GetComponent<BoxCollider2D>());
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
                StartCoroutine(PillarDeath(0f));
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
            foolList.Add(finalFool);
            parent.AddComponent<FoolMinion>();
        }

        public class FoolMinion : MonoBehaviour
        {
            private GameObject initFool;
            private GameObject finalFool;
            private HealthManager hm;
            private BoxCollider2D bc;
            private const float InitY = 6.01f;
            private const float FinalY = 8.61f; //.65
            private readonly float OffsetY = CustomWP.boss == CustomWP.Boss.Isma && !OWArenaFinder.IsInOverWorld ? 0.57f : 0f;
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
                hm.IsInvincible = true;
                Animator anim = initFool.GetComponent<Animator>();
                initFool.transform.SetPosition2D(xPos, InitY + OffsetY);
                initFool.transform.localScale *= 1.4f;
                yield return anim.PlayBlocking("SpawnFool");
                Destroy(initFool);
                finalFool.transform.SetPosition2D(xPos, FinalY + OffsetY);
                tk2dSpriteAnimator tk = finalFool.GetComponent<tk2dSpriteAnimator>();
                PlayMakerFSM fsm = finalFool.LocateMyFSM("Plant Trap Control");
                fsm.enabled = false;
                finalFool.SetActive(true);
                tk.Play("Retract");
                // Doing this to stop them from doing damage when they first spawn
                yield return new WaitWhile(() =>
                {
                    bc = finalFool.GetComponent<BoxCollider2D>();
                    if(bc != null) bc.enabled = false;
                    return tk.IsPlaying("Retract");
                });
                fsm.enabled = true;
                fsm.SetState("Init");
                fsm.GetAction<Wait>("Ready", 2).time = 0.55f;
                hm.IsInvincible = false;
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
            private GameObject cover;
            private GameObject under;
            private HealthManager hm;
            private Vector2 pos;
            private readonly float LEFT = OWArenaFinder.IsInOverWorld ? 105.55f : 60.27f;
            private readonly float RIGHT = OWArenaFinder.IsInOverWorld ? 137.02f : 91.73f;
            private readonly float MIDDLE = OWArenaFinder.IsInOverWorld ? 120f : 75f;

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
                hm.GetComponent<HealthManager>().hp = 1;
                RemoveGeo(hm);
                hm.OnDeath += () =>
                {
                    IsmaController.offsetTime -= TIME_INC;
                    TurretCount--;
                    GameManager.instance.StartCoroutine(CorpseDropThroughFloor());
                    Destroy(cover);
                    Destroy(under);
                    Destroy(gameObject);
                };
                
                initGulka.SetActive(true);
                Animator anim = initGulka.GetComponent<Animator>();
                float rot = pos.x > MIDDLE ? 90f : -90f;
				initGulka.transform.SetPosition2D(pos.x > MIDDLE ? RIGHT + 0.19f : LEFT - 0.19f, pos.y); // I think MIDDLE isn't geting reset between Godhome and Overworld
				initGulka.transform.localScale *= 1.4f;
                initGulka.transform.SetRotation2D(rot);
                MeshRenderer mesh = finalGulka.GetComponent<MeshRenderer>();
                Destroy(finalGulka.GetComponent<SetZ>());
                PlayMakerFSM fsm = finalGulka.LocateMyFSM("Plant Turret");

                // stop spike ball from doing damage to enemy
                var ball = fsm.GetAction<CreateObject>("Fire", 3).gameObject.Value;
                if (!ball.GetComponent<ModifiedSpit>())
                {
                    ball.AddComponent<ModifiedSpit>();
                }
                // Prevent hiding
                fsm.RemoveFsmGlobalTransition("HIDE");

                if(!isPhase2) fsm.GetAction<Wait>("Idle Anim", 1).time.Value = 1.1f;

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

                finalGulka.transform.position = new Vector3(pos.x > MIDDLE ? RIGHT - 0.29f: LEFT + 0.29f, pos.y, -0.09f);
                cover = fsm.GetFsmGameObjectVariable("Cover").Value;
                under = fsm.GetFsmGameObjectVariable("Under").Value;
                cover.transform.position = new Vector3(pos.x > MIDDLE ? RIGHT - 0.16f : LEFT + 0.16f, pos.y, -0.1f);
                under.transform.position = new Vector3(pos.x > MIDDLE ? RIGHT - 0.19f : LEFT + 0.19f, pos.y, -0.08f);

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

            // Recreation of DropThroughFloor from the Corpse MonoBehaviour
            private IEnumerator CorpseDropThroughFloor()
			{
                yield return null;
                GameObject corpse = GameObject.Find("Corpse Plant Turret(Clone)");
                corpse.name = "Corpse Plant Turret that will fall through the floor soon";
                yield return new WaitForSeconds(Random.Range(3f, 6f));
                Collider2D[] cols = corpse.GetComponentsInChildren<Collider2D>();
                for(int i = 0; i < cols.Length; i++)
                {
                    cols[i].enabled = false;
                }
                if(corpse.GetComponent<Rigidbody2D>())
                {
                    corpse.GetComponent<Rigidbody2D>().isKinematic = false;
                }
                yield return new WaitForSeconds(1f);
                Destroy(corpse);
                yield break;
            }
        }
        
        class ModifiedSpit : MonoBehaviour
        {
            private PlayMakerFSM _fsmEnemyDmg;
            private PlayMakerFSM _fsmSpit;
            private void Awake()
            {
                gameObject.layer = (int)GlobalEnums.PhysLayers.PROJECTILES;
                _fsmEnemyDmg = gameObject.LocateMyFSM("damages_enemy");
                _fsmEnemyDmg.enabled = false;
                _fsmSpit = gameObject.LocateMyFSM("spike ball control");
                Trigger2dEventLayer triggerEvent = _fsmSpit.GetAction<Trigger2dEventLayer>("Idle", 1);
                triggerEvent.trigger = PlayMakerUnity2d.Trigger2DType.OnTriggerStay2D;
            }

            private void Start() => StartCoroutine(WaitForPlayerHit());

            private IEnumerator WaitForPlayerHit()
            {
                yield return new WaitUntil(() => _fsmSpit.ActiveStateName == "Nail Hit");
                gameObject.layer = 17;
                _fsmEnemyDmg.enabled = true;
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
