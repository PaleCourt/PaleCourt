using System.Collections;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace FiveKnights
{
    public class DungCloud : MonoBehaviour
    {
        private PlayMakerFSM _dungControl;

        private void Awake()
        {
            gameObject.SetActive(true);
        }

        private IEnumerator Start()
        {
            Log("DungCloud Start");
            yield return new WaitWhile(() => !gameObject.LocateMyFSM("Control"));

            Log("Getting Control FSM");
            _dungControl = gameObject.LocateMyFSM("Control");
            Log("_dungControl null? " + (_dungControl == null));

            foreach (FsmState state in _dungControl.FsmStates)
            {
                Log("State Name: " + state.Name);
            }

            Log("Getting Pt Normal");
            GameObject pt = _dungControl.Fsm.GetFsmGameObject("Pt Normal").Value;
            Log("Pt Normal null? " + (pt == null));
            Log("Getting PSR");
            var psr = pt.GetComponent<ParticleSystemRenderer>();
            Log("Modifying PSR Properties");
            psr.material = new Material(Shader.Find("Particles/Additive"));
            psr.material.mainTexture = Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(x => x.name == "Default-Particle");
            psr.material.color = Color.white;

            _dungControl.InsertMethod("Wait", 0, ChangeDungParticles);
        }

        private void ChangeDungParticles()
        {
            Log("Getting Pt Normal");
            GameObject pt = GameObject.Find("Pt Normal");
        }

        private void Log(object message) => Modding.Logger.Log("[Dung Cloud] " + message);
    }
}
