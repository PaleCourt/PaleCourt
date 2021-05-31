using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FiveKnights.BossManagement;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using SFCore.Utils;
using UnityEngine;

namespace FiveKnights.Isma
{
    public class EnemyPlantSpawn : MonoBehaviour
    {
        private SpriteRenderer _sr;
        public static bool isPhase2;
        public List<GameObject> PlantG { get; set; }
        public List<GameObject> PlantF { get; set; }
        private List<float> PlantP = new List<float>();
        public bool isSpecialTurret;
        private GameObject plant;
        private Vector2 pos;
        private const int MAX_GULKA = 5;
        private const int MAX_TRAP = 5;
        private const int MAX_PILLAR = 6;
        private const float TIME_INC = 0.32f;
        private readonly float LEFT_X = (OWArenaFinder.IsInOverWorld) ? 105f : 60.3f;
        private readonly float RIGHT_X = (OWArenaFinder.IsInOverWorld) ? 135f : 90.6f;
        private readonly float MIDDDLE = (OWArenaFinder.IsInOverWorld) ? 120 : 75f;
        private readonly float GROUND_Y = 5.9f;

        private IEnumerator Start()
        {
            if (isSpecialTurret)
            {
                pos = gameObject.transform.position;
                StartCoroutine(Phase2Spawn());
                yield break;
            }
            CollisionCheck cc = gameObject.AddComponent<CollisionCheck>();
            yield return new WaitWhile(() => !cc.Hit);
            pos = gameObject.transform.position;
            _sr = gameObject.GetComponent<SpriteRenderer>();
            bool skip = false;
            Vector2 gulkaB1 = new Vector2(LEFT_X + 0.7f,GROUND_Y + 3.6f);
            Vector2 gulkaB2 = new Vector2(RIGHT_X - 1.6f, GROUND_Y + 13.1f);
            Vector2 foolB1 = new Vector2(LEFT_X - 0.3f, GROUND_Y + 4.1f);
            Vector2 foolB2 = new Vector2(RIGHT_X - 2.6f, GROUND_Y + 4.1f);
            if (isPhase2)
            {
                foolB1 = new Vector2(LEFT_X + 8.2f, GROUND_Y + 4.1f);
                foolB2 = new Vector2(RIGHT_X - 6.6f, GROUND_Y + 4.1f);
            }
            if (!isPhase2 && (pos.x > gulkaB2.x || pos.x < gulkaB1.x) && pos.y > gulkaB1.y && pos.y < gulkaB2.y)
            {
                foreach (GameObject i in PlantG.Where(x => Vector2.Distance(x.transform.position, pos) < 3f)) skip = true;
                Log("PLANTG C " + PlantG.Count);
                if (skip || PlantG.Count > MAX_GULKA) //A
                {
                    Destroy(gameObject);
                    yield break;
                }
                IsmaController.offsetTime += TIME_INC;
                _sr.enabled = false;
                StartCoroutine(SpawnGulka());
            }
            else if ((pos.x < foolB2.x && pos.x > foolB1.x) && pos.y < foolB2.y)
            {
                Log("PLANTF C " + PlantF.Count);
                if (PlantF.Count > MAX_TRAP) //B
                {
                    Destroy(gameObject);
                    yield break;
                }
                _sr.enabled = false;
                if (isPhase2)
                {
                    Log("PLANT P " + PlantP.Count);
                    if (PlantP.Count > MAX_PILLAR)
                    {
                        Destroy(gameObject);
                        yield break;
                    }
                    StartCoroutine(PlantPillar());
                }
                else
                {
                    IsmaController.offsetTime += TIME_INC;
                    StartCoroutine(SpawnFool());
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        IEnumerator PlantPillar()
        {
            bool skip = false;
            foreach (float i in PlantP.Where(x => Math.Abs(pos.x - x) < 2f)) skip = true;
            if (skip)
            {
                yield break;
            }
            GameObject pillar = Instantiate(FiveKnights.preloadedGO["Plant"]);
            PlantP.Add(pos.x);
            pillar.transform.position = new Vector2(pos.x, 6.1f);
            pillar.AddComponent<PlantCtrl>().IsmaFight = true;
            yield return new WaitForSeconds(0.1f);
            pillar.GetComponent<HealthManager>().OnDeath -= EnemyPlantSpawn_OnDeath;
            pillar.GetComponent<HealthManager>().OnDeath += EnemyPlantSpawn_OnDeath;
            plant = pillar;
            StartCoroutine(WallKill(pillar.GetComponent<HealthManager>()));
        }

        private IEnumerator Phase2Spawn()
        {
            yield return new WaitForSeconds(2f);
            GameObject gulk = Instantiate(FiveKnights.preloadedGO["Gulka"]);
            Animator anim = gulk.GetComponent<Animator>();
            float rot = 180f;
            gulk.transform.SetPosition2D(pos);
            gulk.transform.localScale *= 1.4f;
            gulk.transform.SetRotation2D(rot);
            GameObject turret = Instantiate(FiveKnights.preloadedGO["PTurret"]);
            plant = turret;
            MeshRenderer mesh = turret.GetComponent<MeshRenderer>();
            PlayMakerFSM fsm = turret.LocateMyFSM("Plant Turret");
            mesh.enabled = false;
            List<MeshRenderer> lst = new List<MeshRenderer>();
            foreach (MeshRenderer i in turret.GetComponentsInChildren<MeshRenderer>(true))
            {
                i.enabled = false;
                lst.Add(i);
            }
            fsm.FsmVariables.FindFsmFloat("Distance Max").Value = 50f;
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
            turret.GetComponent<HealthManager>().OnDeath -= EnemyPlantSpawn_OnDeath;
            turret.GetComponent<HealthManager>().OnDeath += EnemyPlantSpawn_OnDeath;
            StartCoroutine(WallKill(turret.GetComponent<HealthManager>()));
        }

        private IEnumerator SpawnGulka()
        {
            GameObject gulk = Instantiate(FiveKnights.preloadedGO["Gulka"]);
            Animator anim = gulk.GetComponent<Animator>();
            float rot = pos.x > MIDDDLE ? 90f : -90f;
            gulk.transform.SetPosition2D(pos.x + (pos.x > MIDDDLE ? 0f : -0.3f), pos.y);
            gulk.transform.localScale *= 1.4f;
            gulk.transform.SetRotation2D(rot);
            GameObject turret = Instantiate(FiveKnights.preloadedGO["PTurret"]);
            plant = turret;
            PlantG.Add(turret);
            MeshRenderer mesh = turret.GetComponent<MeshRenderer>();
            PlayMakerFSM fsm = turret.LocateMyFSM("Plant Turret");
            mesh.enabled = false;
            List<MeshRenderer> lst = new List<MeshRenderer>();
            foreach (MeshRenderer i in turret.GetComponentsInChildren<MeshRenderer>(true))
            {
                i.enabled = false;
                lst.Add(i);
            }
            fsm.FsmVariables.FindFsmFloat("Distance Max").Value = 50f;
            turret.transform.SetRotation2D(rot);
            turret.SetActive(true);
            rot *= Mathf.Deg2Rad;
            turret.transform.SetPosition2D(pos.x, pos.y + 0.5f * Mathf.Cos(rot)); //90: x-0.5, -90: x+0.5, 180f: y-0.5
            anim.Play("SpawnGulka");
            yield return new WaitForSeconds(0.05f);
            yield return new WaitWhile(() => anim.IsPlaying());
            Destroy(gulk);
            mesh.enabled = true;
            foreach (MeshRenderer i in lst)
            {
                i.enabled = true;
            }
            turret.GetComponent<HealthManager>().OnDeath -= EnemyPlantSpawn_OnDeath;
            turret.GetComponent<HealthManager>().OnDeath += EnemyPlantSpawn_OnDeath;
            StartCoroutine(WallKill(turret.GetComponent<HealthManager>()));
        }

        private IEnumerator SpawnFool()
        {
            GameObject fool = Instantiate(FiveKnights.preloadedGO["Fool"]);
            Animator anim = fool.GetComponent<Animator>();
            fool.transform.SetPosition2D(pos.x, 6.05f); //6.2
            fool.transform.localScale *= 1.4f;
            anim.Play("SpawnFool");
            yield return null;
            yield return new WaitWhile(() => anim.IsPlaying());
            Destroy(fool);
            GameObject trap = Instantiate(FiveKnights.preloadedGO["PTrap"]);
            plant = trap;
            PlantF.Add(trap);
            trap.transform.SetPosition2D(pos.x, 8.65f); //8.8
            tk2dSpriteAnimator tk = trap.GetComponent<tk2dSpriteAnimator>();
            PlayMakerFSM fsm = trap.LocateMyFSM("Plant Trap Control");
            fsm.enabled = false;
            trap.SetActive(true);
            tk.Play("Retract");
            yield return new WaitWhile(() => tk.IsPlaying("Retract"));
            fsm.enabled = true;
            fsm.SetState("Init");
            fsm.GetAction<Wait>("Ready", 2).time = 0.4f;
            //fsm.RemoveTransition("", "");

            trap.GetComponent<HealthManager>().OnDeath -= EnemyPlantSpawn_OnDeath;
            trap.GetComponent<HealthManager>().OnDeath += EnemyPlantSpawn_OnDeath;
            StartCoroutine(WallKill(trap.GetComponent<HealthManager>()));
        }

        private IEnumerator WallKill(HealthManager hm)
        {
            yield return new WaitWhile(() => !IsmaController.killAllMinions);
            isPhase2 = true;
            hm.Die(new float?(0f), AttackTypes.Nail, true);
        }

        private void EnemyPlantSpawn_OnDeath()
        {
            if (isSpecialTurret)
            {
                if (!IsmaController.eliminateMinions) StartCoroutine(Phase2Spawn());
            }
            else if (plant.name.Contains("Trap"))
            {
                IsmaController.offsetTime -= TIME_INC;
                PlantF.Remove(plant);
            }
            else if (plant.name.Contains("Turret"))
            {
                IsmaController.offsetTime -= TIME_INC;
                PlantG.Remove(plant);
            }
            else if (plant.name.Contains("Plant"))
            {
                StartCoroutine(PillarDeath());
                return;
            }
            if(!isSpecialTurret) Destroy(gameObject);
        }

        private IEnumerator PillarDeath()
        {
            Animator anim = plant.GetComponent<Animator>();
            anim.Play("PlantDie");
            yield return null;
            yield return new WaitWhile(() => anim.IsPlaying());
            PlantP.Remove(plant.transform.GetPositionX());
            Destroy(plant);
            Destroy(gameObject); 
        }

        private static void Log(object obj)
        {
            Modding.Logger.Log("[Enemy Plant] " + obj);
        }
    }
}
