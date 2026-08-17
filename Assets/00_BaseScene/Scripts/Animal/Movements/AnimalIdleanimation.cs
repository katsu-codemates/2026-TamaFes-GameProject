using System.Collections;
using DG.Tweening;
using UnityEngine;

public class AnimalIdleanimation : MonoBehaviour
{
    Tween idleTween;
    Tween jumpTween;
    SpriteRenderer spriteRenderer;
    Coroutine randomMoveCoroutine;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        IdleMotion();
        randomMoveCoroutine = StartCoroutine(RandomMove());
    }

    void IdleMotion()
    {
        Vector3 startPosition = transform.position;
        startPosition.y += 0.2f;
        idleTween = transform.DOMove(startPosition, 1f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject); // このオブジェクトが破棄されたときに自動でkillされる
    }

    IEnumerator RandomMove()
    {
        while (true)
        {
            float waitTime = Random.Range(0.1f, 10f);
            yield return new WaitForSeconds(waitTime);
            Debug.Log($"動く：{waitTime}秒待ちました");

            if (idleTween != null && idleTween.IsActive()) 
            {
                idleTween.Kill();
            }

            int jumpCount = Random.Range(1, 4);
            for (int i = 0; i < jumpCount; i++)
            {
                float moveX = Random.Range(-2f, 2f);
                if (moveX < 0)
                {
                    spriteRenderer.flipX = true; // 反対に進むので画像を反転
                }
                else
                {
                    spriteRenderer.flipX = false;
                }
                Vector3 currentPosition = transform.position;
                currentPosition.x += moveX;
                
                jumpTween = transform.DOJump(currentPosition, 1f, 1, 1f)
                    .SetLink(gameObject);

                yield return jumpTween.WaitForCompletion();
            }

            IdleMotion();
        }
    }

    // このゲームオブジェクトが破棄されたときに呼び出されるメソッド。
    void OnDestroy()
    {
        if (randomMoveCoroutine != null)
        {
            StopCoroutine(randomMoveCoroutine);
        }
 
        if (idleTween != null && idleTween.IsActive())
        {
            idleTween.Kill();
        }
 
        if (jumpTween != null && jumpTween.IsActive())
        {
            jumpTween.Kill();
        }
    }
}