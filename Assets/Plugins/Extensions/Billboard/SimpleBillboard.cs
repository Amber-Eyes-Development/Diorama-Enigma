using Extensions.Log;
using UnityEngine;

namespace Extensions.Billboard
{
    /// <summary>
    /// Билборд — объект всегда повёрнут лицом к камере
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        [Header("Камера"), Space]
        
        [SerializeField]
        [Tooltip("Камера-цель. Если не задана — используется Camera.main")]
        private Camera targetCamera;

        [Header("Направление"), Space]
        
        [SerializeField]
        [Tooltip("Режим поворота билборда: \nFullFaceCamera — полный поворот: объект смотрит прямо в камеру (сферический билборд)" +
                 "\nYAxisOnly — только по оси Y: объект поворачивается горизонтально (цилиндрический билборд)")]
        private BillboardMode mode = BillboardMode.FullFaceCamera;
        [SerializeField]
        [Tooltip("Инвертировать направление — объект смотрит от камеры, а не на неё")]
        private bool invertDirection = false;

        private Transform cameraTransform;
        private Transform cachedTransform;
        
        private void Awake()
        {
            cachedTransform = transform;
            ResolveCamera();
        }

        private void OnEnable()
        {
            if (cameraTransform == null)
                ResolveCamera();
        }

        private void LateUpdate()
        {
            if (cameraTransform == null)
            {
                ResolveCamera();
                return;
            }

            ApplyRotation();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (targetCamera == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
            else if (targetCamera != null)
                cameraTransform = targetCamera.transform;
        }
#endif
        
        private void ResolveCamera()
        {
            if (targetCamera != null)
            {
                cameraTransform = targetCamera.transform;
                return;
            }

            Camera main = Camera.main;
            if (main != null) cameraTransform = main.transform;
            else ServiceDebug.LogError("Не найдена Camera.main, билборд не инициализирован");
        }

        #region Поворот
        
        private void ApplyRotation()
        {
            Vector3 directionToCamera = cameraTransform.position - cachedTransform.position;

            if (directionToCamera == Vector3.zero) return;

            switch (mode)
            {
                case BillboardMode.FullFaceCamera:
                    ApplyFullRotation(directionToCamera);
                    break;

                case BillboardMode.YAxisOnly:
                    ApplyYAxisRotation(directionToCamera);
                    break;
            }
        }

        private void ApplyFullRotation(Vector3 directionToCamera)
        {
            Vector3 forward = invertDirection ? directionToCamera : -directionToCamera;
            cachedTransform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        private void ApplyYAxisRotation(Vector3 directionToCamera)
        {
            directionToCamera.y = 0f;
            if (directionToCamera == Vector3.zero) return;

            Vector3 forward = invertDirection ? directionToCamera : -directionToCamera;
            cachedTransform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
        
        #endregion
    }
}