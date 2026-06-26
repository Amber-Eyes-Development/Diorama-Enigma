using Extensions.RuntimeReferences;
using UnityEngine;

namespace Extensions.SceneFlow
{
    /// <summary>
    /// Канал рантайм-ссылки на экран загрузки
    /// </summary>
    /// <remarks>
    /// Окно загрузки публикует себя сюда при появлении (его динамически поднимает контроллер окон),
    /// а контроллер сцен читает экран, чтобы проиграть появление/исчезновение и дождаться их завершения
    /// </remarks>
    [CreateAssetMenu(menuName = "Extensions/SceneFlow/" + nameof(LoadingScreenReference))]
    public sealed class LoadingScreenReference : RuntimeReference<ILoadingScreen> { }
}
