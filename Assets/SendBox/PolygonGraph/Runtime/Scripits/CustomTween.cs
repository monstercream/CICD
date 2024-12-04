using UnityEngine;
using System;
using System.Collections.Generic;

namespace PolygonGraph
{
    public static class CustomTween
    {
        private class TweenData
        {
            public float currentTime = 0f;
            public float startValue;
            public float endValue;
            public float duration;
            public Action<float> onFloatUpdate;
            public Action<int> onIntUpdate;
            public bool isPlaying = true;
            public bool isInteger;
        }

        private static List<TweenData> activeTweens = new List<TweenData>();
        private static bool isInitialized = false;
        private static GameObject updateRunner;

        private static float OutQuad(float t)
        {
            return t * ( 2 - t );
        }

        private static void Initialize()
        {
            if( isInitialized ) return;

            updateRunner = new GameObject( "CustomDOTweenRunner" );
            updateRunner.AddComponent<TweenRunner>();
            GameObject.DontDestroyOnLoad( updateRunner );
            isInitialized = true;
        }

        public static void DOFloat(float startValue, float endValue, float duration, Action<float> onUpdate)
        {
            Initialize();

            var tween = new TweenData
            {
                startValue = startValue,
                endValue = endValue,
                duration = duration,
                onFloatUpdate = onUpdate,
                isInteger = false
            };

            activeTweens.Add( tween );
        }

        public static void DOInt(int startValue, int endValue, float duration, Action<int> onUpdate)
        {
            Initialize();

            var tween = new TweenData
            {
                startValue = startValue,
                endValue = endValue,
                duration = duration,
                onIntUpdate = onUpdate,
                isInteger = true
            };

            activeTweens.Add( tween );
        }

        private class TweenRunner : MonoBehaviour
        {
            private void Update()
            {
                for( int i = activeTweens.Count - 1; i >= 0; i-- )
                {
                    var tween = activeTweens[ i ];
                    if( !tween.isPlaying )
                    {
                        activeTweens.RemoveAt( i );
                        continue;
                    }

                    if( tween.currentTime < tween.duration )
                    {
                        tween.currentTime += Time.deltaTime;
                        float t = Mathf.Clamp01( tween.currentTime / tween.duration );

                        float easedT = OutQuad( t );
                        float currentValue = Mathf.Lerp( tween.startValue, tween.endValue, easedT );

                        if( tween.isInteger )
                        {
                            tween.onIntUpdate?.Invoke( Mathf.RoundToInt( currentValue ) );
                        }
                        else
                        {
                            tween.onFloatUpdate?.Invoke( currentValue );
                        }
                    }
                    else
                    {
                        if( tween.isInteger )
                        {
                            tween.onIntUpdate?.Invoke( Mathf.RoundToInt( tween.endValue ) );
                        }
                        else
                        {
                            tween.onFloatUpdate?.Invoke( tween.endValue );
                        }
                        tween.isPlaying = false;
                    }
                }
            }
        }
    }
}