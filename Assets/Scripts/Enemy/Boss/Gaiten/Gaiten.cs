using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gaiten : MonoBehaviour
{
    [Header("Status")]
    public int atk;
    [SerializeField]
    private int maxHP;
    private int hp;

    [Header("Action")]
    // #1. �̵�
    public Vector2 moveDir = Vector2.left;
    [SerializeField]
    private float moveTime;
    [SerializeField]
    private float moveSpeed;
    // #2. �ǰ�
    private bool onDamage;
    private bool onDie;
    [SerializeField]
    private float hitTime;
    // #3. �����̵�
    [SerializeField]
    private float blinkTime;
    // #4. ������
    [SerializeField]
    private float patternDelay = 0.7f;
    private WaitForSeconds patternWait;

    [Header("Detect")]
    [SerializeField]
    private Transform sight;
    [SerializeField]
    private float detectRange;
    [SerializeField]
    private float attackRange;
    [SerializeField]
    private float blinkRange;
    private RaycastHit2D meleeDetect;
    private RaycastHit2D leftDetect;
    private RaycastHit2D rightDetect;

    [Header("Attack")]
    // ���� �ڽ�
    [SerializeField]
    private GameObject attackBox;
    public bool onAttack;
    // #1. ��� ����
    [SerializeField]
    private float dashTime;
    [SerializeField]
    private float dashSpeed;
    [SerializeField]
    private float dashSight;
    // #2. ���� ����
    [SerializeField]
    private float jumpTime;
    [SerializeField]
    private float jumpDelay;
    [SerializeField]
    private float jumpAttackTime;
    [SerializeField]
    private float jumpFallTime;
    [SerializeField]
    private float jumpAttackDelay;
    [SerializeField]
    private float jumpForceX;
    [SerializeField]
    private float jumpForceY;
    [SerializeField]
    private float jumpAttackForce;
    // #3. �ɾ� ����
    [SerializeField]
    private float crouchTime;
    [SerializeField]
    private float crouchMoveSpeed;
    [SerializeField]
    private float crouchSight;
    [SerializeField]
    private float crouchAttackDelay;
    // #4. ���� �߻�
    [SerializeField]
    private GameObject rosary;
    [SerializeField]
    private float shotDelay;
    // #5. ������
    [SerializeField]
    private float attackDelay;
    private WaitForSeconds attackWait;

    [Header("Collider")]
    private Vector2 idleColOff = new Vector2(-0.08087763f, -0.2648114f); // ��� Collider2D�� Offset
    private Vector2 idleColSize = new Vector2(0.4757214f, 1.345377f); // ��� Collider2D�� Size
    private Vector2 jAColOff = new Vector2(0.2912853f, 0.01643795f); // ���� ���� ���¿����� Collider2D�� Offset
    private Vector2 jAleColSize = new Vector2(1.094467f, 0.7854486f); // ���� ���� ���¿����� Collider2D�� Size
    private Vector2 crouchColOff = new Vector2(-0.2080189f, -0.4854448f); // ���� ���¿����� Collider2D�� Offset
    private Vector2 crouchColSize = new Vector2(0.5657727f, 0.9041105f); // ���� ���¿����� Collider2D�� Size

    [Header("Component")]
    private SpriteRenderer sprite;
    private Rigidbody2D rigid;
    private BoxCollider2D collider;
    private Animator animator;

    void Awake()
    {
        patternWait = new WaitForSeconds(patternDelay);
        attackWait = new WaitForSeconds(attackDelay);
        sprite = GetComponent<SpriteRenderer>();
        rigid = GetComponent<Rigidbody2D>();
        collider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        hp = maxHP;
        collider.offset = idleColOff;
        collider.size = idleColSize;
        Invoke("Think", 3.5f);
    }

    // #1. Ž�� ����� �������� ��� �Ǵ� �̵�
    void Think()
    {
        if (onDamage)
            return;

        moveDir = Detect();

        if (moveDir == Vector2.zero)
        {
            Invoke("Think", patternDelay);
        }
        else
        {
            int pattern = 0;

            if (hp <= 25)
                pattern = Random.Range(0, 4); // 3�� ���ð�� ���ֹ߻�
            else
                pattern = Random.Range(0, 3);

            switch (pattern)
            {
                case 0:
                case 1:
                case 2:
                    StartCoroutine("Move");
                    break;
                case 3:
                    StartCoroutine("RosarySpawn");
                    break;
            }
        }
    }

    // #2. Ž�� ����
    Vector2 Detect()
    {
        Vector2 detect = Vector2.left;

        // #. PlayerScan (��)
        Debug.DrawRay(sight.position, Vector2.left * detectRange, Color.yellow);
        leftDetect = Physics2D.Raycast(sight.position, Vector2.left, detectRange, LayerMask.GetMask("Player"));

        // #. PlayerScan (��)
        Debug.DrawRay(sight.position, Vector2.right * detectRange, Color.yellow);
        rightDetect = Physics2D.Raycast(sight.position, Vector2.right, detectRange, LayerMask.GetMask("Player"));

        if (leftDetect)
        {
            detect = Vector2.left;
            transform.localScale = new Vector3(-0.9f, 0.9f, 1);
        }
        else if (rightDetect)
        {
            detect = Vector2.right;
            transform.localScale = new Vector3(0.9f, 0.9f, 1);
        }
        else
            detect = Vector2.zero;

        return detect;
    }

    // #3. �̵� ����
    IEnumerator Move()
    {
        float moveT = moveTime;
        animator.SetBool("onMove", true);

        while (moveT > 0)
        {
            moveT -= Time.deltaTime;
            rigid.linearVelocity = moveDir * moveSpeed;

            // #. �̵��� Physics2D�� �÷��̾� Ž��
            Debug.DrawRay(sight.position, moveDir * attackRange, Color.red);
            meleeDetect = Physics2D.Raycast(sight.position, moveDir, attackRange, LayerMask.GetMask("Player"));
            
            // #. �����Ÿ��� ���ٽ� ���� ����
            if (meleeDetect)
                AttackReady();

            yield return null;
        }

        rigid.linearVelocity = Vector2.zero;

        // #. �̵��� ������ Blink�� ���� Physics2D�� �÷��̾� Ž�� (�÷��̾ �ݴ��ʿ� �����ÿ� �߰��Ѵ�)
        Debug.DrawRay(sight.position, moveDir * blinkRange, Color.yellow);
        RaycastHit2D blinkDetect = Physics2D.Raycast(sight.position, moveDir, blinkRange, LayerMask.GetMask("Player"));
        
        // #. Ž���ÿ� �÷��̾�� �����̵�
        if (blinkDetect)
        {
            float target = blinkDetect.collider.transform.position.x;
            StartCoroutine("Blink", target);
        }
        // #. ã�� ������ ��� Think ����
        else
        {
            animator.SetBool("onMove", false);
            yield return patternWait;
            Think();
        }
    }

    // #4. ���� �غ�
    void AttackReady()
    {
        StopCoroutine("Move");
        rigid.linearVelocity = Vector2.zero;
        animator.SetBool("onMove", false);
        animator.SetBool("onAttack", true);

        onAttack = true;
        int attackType = 0;
        if (transform.position.x < -16 || transform.position.x > 22)
            attackType = Random.Range(0, 8);
        else
            attackType = Random.Range(0, 10);

        animator.SetBool("onAttack", true);
        GaitenAttackBox attack = attackBox.GetComponent<GaitenAttackBox>();

        switch (attackType)
        {
            // #. ���� ������
            case 0:
            case 1:
            case 2:
                attack.ChangeCollider(0);
                StartCoroutine("PistAttack");
                break;
            // #. ��������
            case 3:
            case 4:
            case 5:
            case 6:
                attack.ChangeCollider(1);
                StartCoroutine("KickAttack");
                break;
            // #. �ɾ� ������
            case 7:
                attack.ChangeCollider(2);
                StartCoroutine("CrouchAttack");
                break;
            // #. ���� ������
            case 8:
            case 9:
                attack.ChangeCollider(3);
                StartCoroutine("JumpAttack");
                break;
        }
    }
    
    // #5. ���� ������
    IEnumerator PistAttack()
    {
        animator.SetTrigger("doPistAttack");

        yield return attackWait;
        animator.SetBool("onAttack", false);

        yield return patternWait;
        Think();
    }

    // #6. ��������
    IEnumerator KickAttack()
    {
        animator.SetTrigger("doDash");
        float time = dashTime;
        RaycastHit2D dashDetect;

        // #. ���� ��ȿ�Ÿ� Ȯ���� ���� �÷��̾�� ���
        while (time > 0 && !onDamage)
        {
            time -= Time.deltaTime;
            rigid.linearVelocity = moveDir * dashSpeed;

            // #. ����� Physics2D�� �÷��̾� Ž��
            Debug.DrawRay(sight.position, moveDir * dashSight, Color.red);
            dashDetect = Physics2D.Raycast(sight.position, moveDir, dashSight, LayerMask.GetMask("Player"));
            
            if (dashDetect || transform.position.x < -16 || transform.position.x > 22)
                break;

            yield return null;
        }

        rigid.linearVelocity = Vector2.zero;
        animator.SetTrigger("doKickAttack");

        yield return attackWait;
        animator.SetBool("onAttack", false);

        yield return patternWait;
        Think();
    }

    // #7. ���� ������
    IEnumerator JumpAttack()
    {
        animator.SetTrigger("doJump");
        float time = jumpTime;
        int curAtk = atk;
        collider.isTrigger = true;

        // #. ������ ����
        while (time > 0)
        {
            time -= Time.deltaTime;
            rigid.linearVelocity = moveDir * jumpForceX + Vector2.up * jumpForceY;
            yield return null;
        }

        rigid.linearVelocity = Vector2.zero;
        rigid.gravityScale = 0;
        Vector2 dir = Detect();
        if (dir != Vector2.zero)
            moveDir = dir;

        // #. ���� ����
        yield return new WaitForSeconds(jumpDelay);
        animator.SetTrigger("doJumpAttack");
        atk *= 2;
        collider.offset = jAColOff;
        collider.size = jAleColSize;
        OnAttackBox();
        time = jumpAttackTime;
        
        while (time > 0 && !onDamage)
        {
            time -= Time.deltaTime;
            rigid.linearVelocity = moveDir * jumpAttackForce;
            
            if (transform.position.x < -16 || transform.position.x > 22)
                break;

            yield return null;
        }

        rigid.linearVelocity = Vector2.zero;

        // #. ���� ������ ���� �� ����
        yield return new WaitForSeconds(jumpAttackTime);
        atk = curAtk;
        OffAttackBox();
        collider.isTrigger = false;
        rigid.linearVelocity = Vector2.zero;
        rigid.gravityScale = 1;

        // #. �ٴڿ� ������ ����
        yield return new WaitForSeconds(jumpFallTime);
        collider.offset = idleColOff;
        collider.size = idleColSize;
        animator.SetBool("onCrouch", true);
        animator.SetBool("onAttack", false);

        yield return new WaitForSeconds(jumpAttackDelay);
        animator.SetBool("onCrouch", false);

        yield return patternWait;
        Think();
    }

    // #8. �ɾ� ������
    IEnumerator CrouchAttack()
    {
        animator.SetBool("onCrouch", true);
        collider.offset = crouchColOff;
        collider.size = crouchColSize;
        float time = crouchTime;
        RaycastHit2D crouchDetect;

        // #. ���� ��ȿ�Ÿ� Ȯ���� ���� �÷��̾ ª�� �̵�
        while (time > 0 && !onDamage)
        {
            time -= Time.deltaTime;
            rigid.linearVelocity = moveDir * crouchMoveSpeed;

            // #. �̵���  Physics2D�� �÷��̾� Ž��
            Debug.DrawRay(sight.position, moveDir * crouchSight, Color.red);
            crouchDetect = Physics2D.Raycast(sight.position, moveDir, crouchSight, LayerMask.GetMask("Player"));

            if (crouchDetect || transform.position.x < -16 || transform.position.x > 22)
                break;

            yield return null;
        }

        rigid.linearVelocity = Vector2.zero;

        // #. ���� ����
        yield return new WaitForSeconds(crouchAttackDelay);
        animator.SetTrigger("onCrouchAttack");

        yield return attackWait;
        animator.SetBool("onAttack", false);

        yield return patternWait;
        animator.SetBool("onCrouch", false);
        collider.offset = idleColOff;
        collider.size = idleColSize;
        Think();
    }

    // #9. ���� ���� Ȱ��ȭ
    public void OnAttackBox()
    {
        attackBox.GetComponent<BoxCollider2D>().enabled = true;
    }

    // #10. ���� ���� ��Ȱ��ȭ
    public void OffAttackBox()
    {
        onAttack = false;
        attackBox.GetComponent<BoxCollider2D>().enabled = false;
    }

    // #11. ���� �߻� (2������ �߰� ����)
    IEnumerator RosarySpawn()
    {
        GameObject spawnRosary = Instantiate(rosary, new Vector2(transform.position.x + moveDir.x * 0.75f, transform.position.y), Quaternion.identity);
        spawnRosary.SetActive(true);

        yield return attackWait;
        Vector2 shotDir = Detect();

        if (shotDir == Vector2.zero)
            shotDir = moveDir;
        
        spawnRosary.GetComponent<Rosary>().BulletSpawn(shotDir);

        yield return new WaitForSeconds(shotDelay);
        yield return patternWait;
        Think();
    }

    // #12. ���� �̵�
    IEnumerator Blink(float dest)
    {
        int dir = Random.Range(0, 2); // 0 : �÷��̾� ����, 1 : �÷��̾� ������
        Vector3 blinkPos = new Vector3(dest + (dir == 0 ? -1.25f : 1.25f), transform.position.y, 1);
        animator.enabled = false;

        yield return new WaitForSeconds(0.5f);
        Color color = sprite.color;
        color.a = 1;
        float delay = blinkTime;

        // #. �������� ������� �Ѵ�
        while (delay > 0)
        {
            delay -= Time.deltaTime;
            sprite.color = new Color(1, 1, 1, delay);
            yield return null;
        }

        transform.localScale = new Vector3(dir == 0 ? 0.9f : -0.9f, 0.9f, 1);

        yield return new WaitForSeconds(0.1f);
        animator.enabled = true;
        transform.position = blinkPos;
        delay = blinkTime;

        // #. �������� �ٽ� �����Ų��
        while (delay > 0)
        {
            delay -= Time.deltaTime;
            sprite.color = new Color(1, 1, 1, 1 - delay);
            yield return null;
        }

        Think();
    }

    // #13. �ǰ� ó��
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (onDie)
            return;

        if (collision.gameObject.CompareTag("PlayerHitBox"))
        {
            int damage = collision.gameObject.GetComponentInParent<Player>().playerATK;
            hp -= damage;
            Vector2 dir = Vector2.zero;
            dir = collision.gameObject.transform.position.x > transform.position.x ? Vector2.left : Vector2.right;
            StartCoroutine("HitProcess", dir);
        }
    }

    IEnumerator HitProcess(Vector2 hitDir)
    {
        if (hp <= 0)
        {
            onDie = true;
            StartCoroutine("DieProcess");
        }

        if (!onAttack)
        {
            onDamage = true;
            rigid.AddForce(hitDir * 1.5f, ForceMode2D.Impulse);
            animator.SetBool("onHit", true);

            yield return new WaitForSeconds(hitTime);
            animator.SetBool("onHit", false);
            onDamage = false;
            Think();
        }
    }

    // #14. ���� ó��
    IEnumerator DieProcess()
    {
        StopCoroutine("HitProcess");
        PatternStop();
        animator.SetBool("onHit", true);

        yield return new WaitForSeconds(3f);
        StartCoroutine(GameManager.instance.StageTransition(12));
    }

    void PatternStop()
    {
        rigid.linearVelocity = Vector2.zero;
        animator.SetBool("onMove", false);
        animator.SetBool("onAttack", false);
        animator.SetBool("onGround", true);
        animator.SetBool("onCrouch", false);
        animator.ResetTrigger("doJump");
        animator.ResetTrigger("doDash");
        animator.ResetTrigger("doPistAttack");
        animator.ResetTrigger("doKickAttack");
        animator.ResetTrigger("doJumpAttack");
        animator.ResetTrigger("doCrouchAttack");
        StopCoroutine("Move");
        StopCoroutine("PistAttack");
        StopCoroutine("KickAttack");
        StopCoroutine("JumpAttack");
        StopCoroutine("CrouchAttack");
    }
}