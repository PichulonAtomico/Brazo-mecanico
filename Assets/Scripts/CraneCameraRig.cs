using UnityEngine;

namespace GruaTorre
{
    public class CraneCameraRig : MonoBehaviour
    {
        public enum CameraMode
        {
            Overview = 0,
            Jib = 1,
            Gripper = 2
        }

        [Header("Referencias de seguimiento")]
        public Transform jibTransform;
        public Transform trolleyTransform;
        public Transform gripperHeadTransform;

        [Header("Vista General")]
        public Vector3 overviewPosition = new Vector3(13.5f, 11.5f, 13.5f);
        public Vector3 overviewLookTarget = new Vector3(2.8f, 3.2f, 2.2f);

        [Header("Suavizado")]
        public float positionLerpSpeed = 6f;
        public float rotationLerpSpeed = 8f;

        public CameraMode CurrentMode { get; private set; } = CameraMode.Overview;

        public void SetMode(int modeIndex)
        {
            CurrentMode = (CameraMode)Mathf.Clamp(modeIndex, 0, 2);
        }

        public void SetOverview() => CurrentMode = CameraMode.Overview;
        public void SetJibView() => CurrentMode = CameraMode.Jib;
        public void SetGripperView() => CurrentMode = CameraMode.Gripper;

        private void LateUpdate()
        {
            Vector3 desiredPos = overviewPosition;
            Vector3 lookTarget = overviewLookTarget;

            switch (CurrentMode)
            {
                case CameraMode.Overview:
                    desiredPos = overviewPosition;
                    lookTarget = overviewLookTarget;
                    break;

                case CameraMode.Jib:
                    if (jibTransform != null && gripperHeadTransform != null)
                    {
                        desiredPos = jibTransform.TransformPoint(new Vector3(3.2f, 1.8f, -1.6f));
                        lookTarget = gripperHeadTransform.position + Vector3.down * 1.1f;
                    }
                    break;

                case CameraMode.Gripper:
                    if (gripperHeadTransform != null && jibTransform != null)
                    {
                        Vector3 radialOut = jibTransform.forward;
                        Vector3 side = jibTransform.right;
                        desiredPos = gripperHeadTransform.position + side * 3.8f - radialOut * 2.2f + Vector3.up * 2.4f;
                        lookTarget = gripperHeadTransform.position + Vector3.down * 1.2f;
                    }
                    break;
            }

            float posT = 1f - Mathf.Exp(-positionLerpSpeed * Time.deltaTime);
            float rotT = 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime);

            transform.position = Vector3.Lerp(transform.position, desiredPos, posT);

            Vector3 dir = lookTarget - transform.position;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotT);
            }
        }
    }
}
