// Path: Assets/_Project/ECS/Components/Input/TouchInputComponent.cs
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Input
{
    /// <summary>
    /// Componente che memorizza lo stato degli input touch per dispositivi mobili.
    /// Permette l'interazione attraverso tap, swipe e gesti multitouch.
    /// </summary>
    [Serializable]
    public struct TouchInputComponent : IComponentData
    {
        /// <summary>
        /// Indica se c'è un touch attivo sullo schermo
        /// </summary>
        public bool IsTouching;
        
        /// <summary>
        /// Posizione corrente del touch principale
        /// </summary>
        public float2 TouchPosition;
        
        /// <summary>
        /// Posizione iniziale del touch corrente
        /// </summary>
        public float2 TouchStartPosition;
        
        /// <summary>
        /// Differenza tra posizione corrente e iniziale (per calcolare swipe)
        /// </summary>
        public float2 TouchDelta;
        
        /// <summary>
        /// Durata del touch corrente in secondi
        /// </summary>
        public float TouchDuration;
        
        /// <summary>
        /// Indica se è stato rilevato un swipe verso l'alto
        /// </summary>
        public bool SwipeUp;
        
        /// <summary>
        /// Indica se è stato rilevato un swipe verso il basso
        /// </summary>
        public bool SwipeDown;
        
        /// <summary>
        /// Indica se è stato rilevato un swipe verso sinistra
        /// </summary>
        public bool SwipeLeft;
        
        /// <summary>
        /// Indica se è stato rilevato un swipe verso destra
        /// </summary>
        public bool SwipeRight;
        
        /// <summary>
        /// Indica se è stato rilevato un tap semplice
        /// </summary>
        public bool Tap;
        
        /// <summary>
        /// Indica se è stato rilevato un doppio tap
        /// </summary>
        public bool DoubleTap;
        
        /// <summary>
        /// Indica se è stato rilevato un tap prolungato (per Focus Time)
        /// </summary>
        public bool LongPress;
        
        /// <summary>
        /// Resetta gli stati di swipe e tap per evitare duplicazioni
        /// </summary>
        public void ResetGestures()
        {
            SwipeUp = false;
            SwipeDown = false;
            SwipeLeft = false;
            SwipeRight = false;
            Tap = false;
            DoubleTap = false;
            LongPress = false;
        }
        
        /// <summary>
        /// Resetta completamente lo stato del touch
        /// </summary>
        public void ResetTouch()
        {
            IsTouching = false;
            TouchPosition = float2.zero;
            TouchStartPosition = float2.zero;
            TouchDelta = float2.zero;
            TouchDuration = 0;
            ResetGestures();
        }
        
        /// <summary>
        /// Crea una nuova istanza con valori predefiniti
        /// </summary>
        /// <returns>Un nuovo TouchInputComponent con valori predefiniti</returns>
        public static TouchInputComponent Default()
        {
            return new TouchInputComponent
            {
                IsTouching = false,
                TouchPosition = float2.zero,
                TouchStartPosition = float2.zero,
                TouchDelta = float2.zero,
                TouchDuration = 0,
                SwipeUp = false,
                SwipeDown = false,
                SwipeLeft = false,
                SwipeRight = false,
                Tap = false,
                DoubleTap = false,
                LongPress = false
            };
        }
    }
}