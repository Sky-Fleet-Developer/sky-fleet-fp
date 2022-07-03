using System;
using System.Collections.Generic;
using Core.Data.GameSettings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TerrainEditor.Control
{
    public class CameraFly : MonoBehaviour
    {
       public float rotationSpeed = 450;
        public float scrollSpeed = 10;
        public float moveSpeed = 0.05f;
        public AnimationCurve scrollPerArm = AnimationCurve.Linear(0, 0.5f, 500, 50);

        private float armLength = 10;

        private Vector2 mouseDelta;
        private GraphicRaycaster graphicRaycaster;
        private EventSystem m_EventSystem;
       
        public int GetQueue => 100;

        private void Awake()
        {
            mouseDelta = Vector2.zero;
            graphicRaycaster = FindObjectOfType<GraphicRaycaster>();
            m_EventSystem = FindObjectOfType<EventSystem>();
        }
        private bool control = false;
        public bool UpdateControl(bool used)
        {
            if (used) return used;
            mouseDelta.x = Input.GetAxis("Mouse X");
            mouseDelta.y = Input.GetAxis("Mouse Y");

            if (Input.GetKey(KeyCode.LeftAlt))
            {
                if (Input.GetKey(KeyCode.Mouse0))
                {
                    RotateAroundArm();
                    used = true;
                    control = true;
                }
                if (Input.GetKey(KeyCode.Mouse1))
                {
                    ScaleArm(-mouseDelta.y * 0.05f);
                    used = true;
                }
            }
            else
            {
                if (Input.GetKey(KeyCode.Mouse1))
                {
                    RotateSelf();
                    used = true;
                }
            }
            if (Input.GetKey(KeyCode.Mouse2))
            {
                MoveParalel();
                used = true;
            }

            if(graphicRaycaster)
            {
                List<RaycastResult> result = new List<RaycastResult>();
                graphicRaycaster.Raycast(new PointerEventData(m_EventSystem) {position = Input.mousePosition}, result);
                if (result.Count > 0) return true;
            }

            ScaleArm(Input.GetAxis("Mouse ScrollWheel"));

            if (control)
            {
                used = true;
                if (Input.GetKeyUp(KeyCode.Mouse0)) control = false;
            }

            return used;
        }

        private void RotateSelf()
        {
            transform.Rotate(Vector3.up * mouseDelta.x * rotationSpeed * Time.deltaTime);
            transform.Rotate(Vector3.left * mouseDelta.y * rotationSpeed * Time.deltaTime);
            Vector3 ea = transform.eulerAngles;
            ea.z = 0;
            transform.eulerAngles = ea;
        }

        private void RotateAroundArm()
        {
            Vector3 armPosition = transform.position + transform.forward * armLength;
            RotateSelf();
            transform.position = armPosition - transform.forward * armLength;
        }

        private void MoveParalel()
        {
            transform.position -= (transform.right * mouseDelta.x + transform.up * mouseDelta.y) * moveSpeed * Mathf.Clamp(armLength, 1, Mathf.Infinity);
        }

        private void ScaleArm(float axe)
        {
            float arm = armLength;
            armLength = Mathf.Clamp(armLength - axe * scrollSpeed * scrollPerArm.Evaluate(armLength), 0, 500);
            transform.position += (arm - armLength) * transform.forward;
        }
    }
}
