/* ================================================================
   ---------------------------------------------------
   Project   :    Unreal FPS
   Publisher :    Infinite Dawn
   Author    :    Tamerlan Favilevich
   ---------------------------------------------------
   Copyright © Tamerlan Favilevich 2017 - 2018 All rights reserved.
   ================================================================ */

using System;
using UnityEngine;

namespace UnrealFPS
{
    [Serializable]
    public class NGGyroLook
    {
        [Range(0.0f, 50.0f)] public float XSensitivity = 4f;
        [Range(0.0f, 50.0f)] public float YSensitivity = 3f;
        [HideInInspector] public Quaternion m_CharacterTargetRot;
        [HideInInspector] public Quaternion m_CameraTargetRot;
        public bool smooth=true;
        [Range(0.0f, 50.0f)] public float smoothTime = 5f;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="camera"></param>
        public void Init(Transform character, Transform camera)
        {
            m_CharacterTargetRot = character.localRotation;
            m_CameraTargetRot = camera.localRotation;
            Input.gyro.enabled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="camera"></param>
        public void LookRotation(Transform character, Transform camera)
        {
 
            float xRot = Input.gyro.rotationRateUnbiased.x * XSensitivity ;
            float yRot = Input.gyro.rotationRateUnbiased.y * YSensitivity;
            m_CharacterTargetRot *= Quaternion.Euler(0f, -yRot, 0f);
            m_CameraTargetRot *= Quaternion.Euler(-xRot, 0f, 0f);

 
            if (smooth)
            {
                character.localRotation = Quaternion.Slerp(character.localRotation, m_CharacterTargetRot, smoothTime * Time.deltaTime);
                camera.localRotation = Quaternion.Slerp(camera.localRotation, m_CameraTargetRot, smoothTime * Time.deltaTime);
            }
            else
            {
                character.localRotation = m_CharacterTargetRot;
                camera.localRotation = m_CameraTargetRot;
            }
        }

 
    }
}