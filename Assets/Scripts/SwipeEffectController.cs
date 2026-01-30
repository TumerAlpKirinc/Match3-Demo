using UnityEngine;
using System.Collections;

public class SwipeEffectController : MonoBehaviour
{
    
    public float swipeDuration = 0.2f;
    public Vector3 startPosition;
    public Vector3 endPosition;

    public void StartSwipe()
    {
        StartCoroutine(MoveAndDestroy());
    }

    private IEnumerator MoveAndDestroy()
    {
        float timer = 0f;
        while (timer < swipeDuration)
        {
            transform.position = Vector3.Lerp(startPosition, endPosition, timer / swipeDuration);
            timer += Time.deltaTime;
            yield return null; 
        }
        transform.position = endPosition;

        Destroy(gameObject);
    }
}