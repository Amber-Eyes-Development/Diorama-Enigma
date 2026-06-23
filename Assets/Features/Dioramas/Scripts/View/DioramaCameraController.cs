using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Управляет камерой: кадрирует активную диораму, реагируя на событие смены фокуса спавнера
    /// </summary>
    /// <remarks>
    /// Навигация — плавный доезд (только пока едет), старт/переукладка — мгновенно. Позиция камеры —
    /// чистая функция активного слота: смещение берётся относительно «домашнего» слота 0
    /// (<see cref="DioramaSpawner.HomePoint"/>), поэтому при резюме на любой диораме рассинхрона нет
    /// </remarks>
    public sealed class DioramaCameraController : MonoBehaviour
    {
        [Tooltip("Спавнер — источник событий смены фокуса")]
        [SerializeField] private DioramaSpawner spawner;
        [Tooltip("Перемещаемый риг камеры (стартовая позиция кадрирует первую активную диораму)")]
        [SerializeField] private Transform cameraRig;
        [Tooltip("Скорость доезда камеры, ед/сек")]
        [Min(0f)]
        [SerializeField] private float moveSpeed = 60f;

        private Vector3 offset;
        private bool hasOffset;
        private Vector3 target;
        private bool moving;

        private void OnEnable()
        {
            if (spawner == null || cameraRig == null)
            {
                ServiceDebug.LogError(this, "spawner или cameraRig не назначен");
                return;
            }

            // смещение — относительно слота 0 (дизайнерский кадр), а НЕ активной диорамы:
            // иначе при резюме на не-нулевой диораме offset «съезжает» и навигация уводит камеру
            if (!hasOffset)
            {
                offset = cameraRig.position - spawner.HomePoint;
                hasOffset = true;
            }

            spawner.onFocusChanged += OnFocusChanged;

            // если спавнер уже стартовал (поздняя подписка) — встать по текущему фокусу
            if (spawner.HasFocus) OnFocusChanged(new DioramaFocus(spawner.ActiveFramePoint, false));
        }

        private void OnDisable()
        {
            if (spawner != null) spawner.onFocusChanged -= OnFocusChanged;
        }

        // доезд только во время перехода; вне его кадр пропускается
        private void Update()
        {
            if (!moving) return;

            cameraRig.position = Vector3.MoveTowards(cameraRig.position, target, moveSpeed * Time.deltaTime);
            if (cameraRig.position == target) moving = false;
        }

        private void OnFocusChanged(DioramaFocus focus)
        {
            target = focus.Point + offset;

            if (focus.Animate)
            {
                moving = true;
            }
            else
            {
                cameraRig.position = target;
                moving = false;
            }
        }
    }
}
