using System;
using System.Collections;
using System.Linq;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using SFCore.Utils;

namespace FiveKnights
{
    public class Dung : MonoBehaviour
    {
        private PlayMakerFSM _dungControl;

        private void Awake()
        {
            _dungControl = gameObject.LocateMyFSM("Control");
        }

        private IEnumerator Start()
        {
            Log("Dung Start");

            gameObject.transform.Log();

            yield return new WaitWhile(() => this == null);

            _dungControl.GetAction<Wait>("Emit Pause", 2).time.Value = 0.1f;
            Log("Getting dungTrail");
            GameObject dungTrail =
                _dungControl.GetAction<SpawnObjectFromGlobalPoolOverTime>("Equipped", 0).gameObject.Value;
            Log("Getting dungTrail FSM");
            PlayMakerFSM dungTrailControl = dungTrail.LocateMyFSM("Control");
            Log("Getting dungPt");
            GameObject dungPt = dungTrailControl.Fsm.GetFsmGameObject("Pt Normal").Value;
            Log("Getting dung PS");

            ParticleSystem dungPtPS = dungPt.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = dungPtPS.main;
            main.duration = 5.0f;

            ParticleSystem.MinMaxGradient startColor = main.startColor;
            startColor.color = Color.white;
            Log("Modifying dungTrailControl FSM");
            dungTrailControl.GetAction<Wait>("Wait", 0).time.Value = 3.0f;

            var dungPtPSR = dungPt.GetComponent<ParticleSystemRenderer>();
            dungPtPSR.material = new Material(Shader.Find("Particles/Additive"));
            dungPtPSR.material.mainTexture = Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(x => x.name == "Default-Particle");
            dungPtPSR.material.color = Color.white;

            GameObject dung = _dungControl.Fsm.GetFsmGameObject("Particle 1").Value;
            var dungPSR = dung.GetComponent<ParticleSystemRenderer>();
            dungPSR.material = new Material(Shader.Find("Particles/Additive"));
            dungPSR.material.mainTexture = Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(x => x.name == "Default-Particle");
            dungPSR.material.color = Color.white;

            Log("Print Dung Hierarchy Tree");
            dung.transform.Log();
            Log("Print Dung Trail Hierarchy Tree");
            dungTrail.transform.Log();
        }

        private void Log(object message) => Modding.Logger.Log("[FiveKnights][Dung] " + message);
    }
}
