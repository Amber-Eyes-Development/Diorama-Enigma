using System;
using Extensions.Audio;
using UnityEngine;
using UnityEngine.Audio;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Действие: 3D one-shot звук на событие шага
    /// </summary>
    [Serializable]
    public sealed class SoundAction : ViewAction
    {
        /// <summary> Звуковой ресурс </summary>
        public AudioResource Sound => sound;
        /// <summary> Тип аудио трека </summary>
        public AudioModel Model => model;
        /// <summary> Зацикливать звук (на откате/смене состояния зацикленный источник останавливается) </summary>
        public bool Loop => loop;

        [Tooltip("Звуковой ресурс")]
        [SerializeField] private AudioResource sound;
        [Tooltip("Тип аудио трека")]
        [SerializeField] private AudioModel model = AudioModel.Sfx;
        [Tooltip("Зацикливать звук. На откате (reversible) зацикленный источник останавливается и возвращается в пул.\n" +
                 "Для большего контроля над аудио через Interactable лучше использовать InteractableObjectsSwitcher + AudioLoopPlayer")]
        [SerializeField] private bool loop;
    }
}
