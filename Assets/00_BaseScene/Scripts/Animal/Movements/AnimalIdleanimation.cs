using System.Collections;
using DG.Tweening;
using UnityEngine;

public class AnimalIdleanimation : MonoBehaviour
{
    Tween idleTween;
    SpriteRenderer spriteRenderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        IdleMotion();
        StartCoroutine(RandomMove());

    }

    // Update is called once per frame
    void Update()
    {

    }
    void IdleMotion()
    {
        Vector3 startPosition = transform.position;
        startPosition.y += 0.2f;
        idleTween = transform.DOMove(startPosition, 1f)
            .SetLoops(-1, LoopType.Yoyo);

    }
    IEnumerator RandomMove()
    {
        while (true)
        {
            float waitTime = Random.Range(0.1f, 10f);
            yield return new WaitForSeconds(waitTime);
            Debug.Log($"動く：{waitTime}秒待ちました");

            idleTween.Kill();
            
            int jumpCount = Random.Range(1, 4);
            for (int i = 0; i < jumpCount; i++)
            {
                float moveX = Random.Range(-2f, 2f);
                if (moveX < 0)
                {
                    spriteRenderer.flipX = true;
                }
                else
                {
                    spriteRenderer.flipX = false;
                }
                Vector3 currentPosition = transform.position;
                currentPosition.x += moveX;
                yield return transform.DOJump(currentPosition, 1f, 1, 1f)
                .WaitForCompletion();
            }


            IdleMotion();

        }
    }
}