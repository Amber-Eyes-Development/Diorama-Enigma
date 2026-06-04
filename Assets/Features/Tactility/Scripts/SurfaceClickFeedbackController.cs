using System.Collections.Generic;
using Extensions.Audio;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DioramaEnigma.Tactility
{
    /// <summary>
    /// Глобальный контроллер обратной тактильной связи по клику на объект
    /// </summary>
    public sealed class SurfaceClickFeedbackController : MonoBehaviour
    {
        [Header("Рейкаст"), Space]
        [Tooltip("Камера для рейкаста. Если не задана — Camera.main")]
        [SerializeField] private Camera targetCamera;
        [Tooltip("Слои, по которым ловится клик")]
        [SerializeField] private LayerMask clickMask = ~0;
        [Tooltip("Максимальная дальность рейкаста")]
        [Min(0f)]
        [SerializeField] private float maxDistance = 100f;

        [Header("Звук"), Space]
        [Tooltip("База соответствий «поверхность → звук»")]
        [SerializeField] private SurfaceSoundDatabase soundDatabase;
        [Tooltip("Тип аудио трека для звуков клика")]
        [SerializeField] private AudioModel model = AudioModel.Sfx;

        [Header("Анимация"), Space]
        [Tooltip("Пул worker-анимаций")]
        [SerializeField] private ClickAnimationPool animationPool;

        [Header("Партиклы"), Space]
        [Tooltip("Пул партикл-эффектов")]
        [SerializeField] private SurfaceParticlePool particlePool;
        [Tooltip("Соответствия материалов поверхностей и партикл-эффектов")]
        [SerializeField] private SurfaceParticle[] surfaceParticles;
        [Tooltip("Эффект по-умолчанию: для коллайдеров без материала или без записи")]
        [SerializeField] private ParticleSystem defaultParticle;

        private Dictionary<PhysicsMaterial, ParticleSystem> particleLookup;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
        }

        private void Update()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;

            Ray ray = targetCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, clickMask)) return;

            PlaySound(hit);
            PlayAnimation(hit);
            PlayParticle(hit);
        }

        private void PlaySound(RaycastHit hit)
        {
            if (soundDatabase == null) return;

            AudioResource sound = soundDatabase.Resolve(hit.collider.sharedMaterial);
            if (sound == null) return;

            AudioController controller = AudioController.Instance;
            if (controller == null) return;

            controller.Play(sound, hit.point, model);
        }

        private void PlayAnimation(RaycastHit hit)
        {
            if (animationPool == null) return;

            ClickAnimationMarker marker = hit.collider.GetComponentInParent<ClickAnimationMarker>();
            if (marker == null || marker.Template == null) return;

            animationPool.Play(marker.Template, marker.Target);
        }

        private void PlayParticle(RaycastHit hit)
        {
            if (particlePool == null) return;

            ParticleSystem prefab = ResolveParticle(hit.collider.sharedMaterial);
            if (prefab == null) return;

            particlePool.Play(prefab, hit.point, hit.normal);
        }

        private ParticleSystem ResolveParticle(PhysicsMaterial material)
        {
            if (material == null) return defaultParticle;

            EnsureParticleLookup();
            return particleLookup.TryGetValue(material, out ParticleSystem particle) ? particle : defaultParticle;
        }

        private void EnsureParticleLookup()
        {
            if (particleLookup != null) return;

            particleLookup = new Dictionary<PhysicsMaterial, ParticleSystem>();
            if (surfaceParticles == null) return;

            foreach (SurfaceParticle pair in surfaceParticles)
            {
                if (pair.Material == null) continue;
                particleLookup[pair.Material] = pair.Particle;
            }
        }
    }
}
