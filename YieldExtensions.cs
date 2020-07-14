using System;
using UnityEngine;

namespace FiveKnights
{
    public sealed class WaitSecWhile : CustomYieldInstruction
    {
        internal float m_Seconds;
        private Func<bool> m_Predicate;
        
        public WaitSecWhile(Func<bool> predicate, float seconds)
        {
            this.m_Predicate = predicate;
            this.m_Seconds = seconds;
        }
        
        public override bool keepWaiting
        {
            get
            {
                m_Seconds -= Time.fixedDeltaTime;
                return this.m_Predicate() && m_Seconds > 0f;
            }
        }
    }
}