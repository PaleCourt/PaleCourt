using System;
using UnityEngine;

namespace FiveKnights.Misc
{
    public class SuperDashCancel : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            Modding.Logger.Log("Killing self");
            HeroController.instance.CancelSuperDash();
            Destroy(this);
        }
    }
}