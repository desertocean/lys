using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EmeraldAI;
namespace JUTPS.Utilities
{
    [AddComponentMenu("JU TPS/Utilities/JUAIBridge")]
    public class JUAIBridge : MonoBehaviour
    {

        EmeraldAI.IDamageable m_IDamageable;
 

        void Start()
        {
            m_IDamageable = GetComponent<EmeraldAI.IDamageable>();
        }

 
        public void DoDamage(JUHealth.DamageInfo damageInfo)
        {
            m_IDamageable.Damage((int)damageInfo.Damage, null, 40);
        }
    }
}