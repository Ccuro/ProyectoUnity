using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC : MonoBehaviour
{
    [SerializeField]
    private string[] patternKind;
    [SerializeField]
    private Sprite stancePose;
    [SerializeField]
    private float patternDelay;
    [SerializeField]
    private float moveSpeed;
    [SerializeField]
    private float limitMin; // �̵������� X��ǥ �ּҰ�
    [SerializeField]
    private float limitMax; // �̵������� X��ǥ �ִ밪

    private SpriteRenderer sprite;
    private Animator animator;
    private Rigidbody2D rigid;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        Think();
    }

    void Think()
    {
        string pattern = patternKind[Random.Range(0, 5)];
        StartCoroutine(pattern);
    }

    IEnumerator Stance()
    {
        animator.enabled = false;
        sprite.sprite = stancePose;

        yield return new WaitForSeconds(patternDelay);
        Think();
    }

    IEnumerator Idle()
    {
        animator.enabled = true;
        animator.SetBool("onMove", false);

        yield return new WaitForSeconds(patternDelay);
        Think();
    }

    IEnumerator Move()
    {
        int num = Random.Range(0, 2); // 0 : ����, 1 : ������
        float moveTime = 3f;
        Vector2 direction;
        animator.enabled = true;
        animator.SetBool("onMove", true);

        if (num == 0)
        {
            sprite.flipX = true;
            direction = Vector2.left;
        }
        else
        {
            sprite.flipX = false;
            direction = Vector2.right;
        }

        while (moveTime > 0)
        {
            moveTime -= Time.deltaTime;
            rigid.linearVelocity = direction * moveSpeed;
            
            if (limitMin > transform.position.x)
            {
                sprite.flipX = false;
                rigid.linearVelocity = direction = Vector2.right;
            }
            else if (limitMax < transform.position.x)
            {
                sprite.flipX = true;
                rigid.linearVelocity = direction = Vector2.left;
            }

            yield return null;
        }

        rigid.linearVelocity = Vector2.zero;
        Think();
    }
}