using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using EmeraldAI.Utility;
using JUTPS;
namespace EmeraldAI
{
    [RequireComponent(typeof(TargetPositionModifier))]
    [RequireComponent(typeof(FactionExtension))]
    public class JUPlayerBridge : EmeraldPlayerBridge
    {
        private JUHealth CharacterHealth;

        public override void Start()
        {
            //You should set the StartHealth and Health variables equal to that of your character controller here.
            base.Start();
            CharacterHealth = GetComponent<JUHealth>();
            
        }

        public override void DamageCharacterController(int DamageAmount, Transform Target)
        {
            CharacterHealth.DoDamage(DamageAmount);
        }

        public override bool IsAttacking()
        {
            //Used for detecting when this target is attacking.
            return false;
        }

        public override bool IsBlocking()
        {
            //Used for detecting when this target is blocking.
            return false;
        }

        public override bool IsDodging()
        {
            //Used for detecting when this target is dodging.
            return false;
        }

        public override void TriggerStun(float StunLength)
        {
            //Custom trigger mechanics can go here, but are not required
        }
    }
}
