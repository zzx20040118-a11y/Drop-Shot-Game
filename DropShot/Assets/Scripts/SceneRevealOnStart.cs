using System.Collections;
using UnityEngine;
using HeyHeyThere.SceneTransitionsFree;

public class SceneRevealOnStart : MonoBehaviour
{
    public string transitionStyle = "iris";
    [Min(0.01f)] public float transitionDuration = 0.6f;

    IEnumerator Start()
    {
        yield return null;
        yield return SceneTransition.Reveal(
            transitionStyle,
            new SceneTransition.Options
            {
                Duration = transitionDuration,
                Color = Color.black
            }
        );
    }
}
