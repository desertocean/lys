using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PampelGames.GoreSimulator;
using JUTPS;
namespace JUTPS.Utilities
{
    [AddComponentMenu("JU TPS/Utilities/LYS Damage Trigger")]
    public class LYSDamageTrigger : MonoBehaviour
    {
        [SerializeReference]
        public GoreSimulator goreSimulator;
        public float pistolForce = 10f;
        public void DoDamage(JUHealth.DamageInfo damageInfo)
        {

            //var ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            //if (Physics.Raycast(ray, out var hitInfo, Mathf.Infinity, enemyLayer))
            //{
            //    if (hitInfo.collider.TryGetComponent<IGoreObject>(out var goreObject))
            //    {
            //        var forceDirection = (hitInfo.point - pistolAnimator.transform.position).normalized;
            //        goreObject.ExecuteCut(hitInfo.point, forceDirection * pistolForce);
            //    }
            //}
            //Debug.Log("=========== "+ damageInfo.HitPosition+" "+ damageInfo.HitDirection);

            var forceDirection = damageInfo.HitDirection.normalized;
            goreSimulator.ExecuteCut(damageInfo.HitPosition, forceDirection * pistolForce);
            damageInfo.juHealth.DoDamage(damageInfo.juHealth.MaxHealth,false);
        }
    }
}



