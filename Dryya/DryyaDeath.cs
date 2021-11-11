using System.Collections;
using FiveKnights.Ogrim;
using UnityEngine;

namespace FiveKnights.Dryya
{
    public class DryyaDeath : MonoBehaviour
    {
        private IEnumerator Start()
        {
            WDController.Instance.PlayMusic(null, 1f);
            yield return new WaitForSeconds(2.0f);
            var bossSceneController = GameObject.Find("Boss Scene Controller");
            var bsc = bossSceneController.GetComponent<BossSceneController>();
            GameObject transition = Instantiate(bsc.transitionPrefab);
            PlayMakerFSM transitionsFSM = transition.LocateMyFSM("Transitions");
            transitionsFSM.SetState("Out Statue");
            yield return new WaitForSeconds(1.0f);
            bsc.DoDreamReturn();
        }
    }
}