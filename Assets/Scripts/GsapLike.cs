using System;
using System.Collections;
using UnityEngine;

public class GsapLike : MonoBehaviour
{
    public static IEnumerator FromTo(Func<float> getter, Action<float> setter, float from, float to, float duration)
    {
        yield return FromTo(getter, setter, from, to, duration, null);
    }
    
    public static IEnumerator FromTo(Func<float> getter, Action<float> setter, float from, float to, float duration, Action onComplete)
    {
        // set init
        setter(from);
        
        float elapsed = 0;
        while (elapsed < duration)
        {
            setter(getter() + (to - from) * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null; // wait for frame
        }
    
        setter(to);
        onComplete?.Invoke();
        yield return null; // wait for frame
    }

    public static IEnumerator FromToCycle(Func<float> getter, Action<float> setter, float from, float to,
        float duration, Func<bool> needStop)
    {
        yield return FromToCycle(getter, setter, from, to, duration, null, needStop);
    }

    public static IEnumerator FromToCycle(Func<float> getter, Action<float> setter, float from, float to,
        float duration, Action onComplete, Func<bool> needStop)
    {
        while (!needStop())
        {
            // set init
            setter(from);
        
            float elapsed = 0;
            while (elapsed < duration)
            {
                setter(getter() + (to - from) * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null; // wait for frame
            }
    
            setter(to);
            yield return null; // wait for frame   
        }

        onComplete?.Invoke();
        yield return null;
    }
}