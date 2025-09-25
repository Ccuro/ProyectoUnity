using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player instance = null;

    [Header("Status")]
    public int maxHP;
    public int playerHP;
    [Range(0, 10)]
    public int playerATK;
    [SerializeField][Range(0, 10)]
    private float runSpeed = 3;
    private float defaultSpeed;
    [SerializeField][Range(0, 12)]
    private float jumpForce;

    [Header("State")]
    [HideInInspector]
    public bool onMove;
    private bool onGround;
    private bool onCrouch;
    [HideInInspector]
    public bool onDash;
    private bool isWall;
    private bool isWallJump;
    private bool onAttack;
    private bool onDamaged;
    private bool onDie;

    [Header("Move")]
    [SerializeField]
    private int jumpCount;
    private int jumpCnt;
    [SerializeField]
    private float checkDistance;
    private float inputX;
    private float inputY;
    [HideInInspector]
    public float isRight = 1; // �ٶ󺸴� ���� 1 = ������, -1 = ����
    [SerializeField]
    private float slidingSpeed;
    [SerializeField]
    private float wallJumpPower;

    [Header("Action")]
    [SerializeField]
    private float dashSpeed;
    [SerializeField]
    private float defaultDashTime;
    private float dashTime;
    private bool dashCool;
    [SerializeField]
    private float dashCooldown;
    public GameObject targetObject;

    [Header("Attack")]
    [SerializeField]
    private int atkCount;
    private int atkCnt;
    [SerializeField]
    private GameObject attackBox;

    [Header("Hit")]
    [SerializeField]
    private float crouchHitPos; // ���� ���¿����� HitBox Y��ǥ
    private float invincibilityTime = 0;
    [SerializeField]
    private GameObject hitBox;
    [SerializeField]
    private Sprite hitPose;

    [Header("Physics")]
    [SerializeField]
    private Transform groundCheckFront; // �ٴ� üũ position
    [SerializeField]
    private Transform groundCheckBack; // �ٴ� üũ position
    private float standColOffsetY = -0.1564108f;
    private float standColSizeY = 1.030928f;
    private float crouchColOffsetY = -0.4838978f;
    private float crouchColSizeY = 0.3759539f;
    [SerializeField]
    private Transform wallCheck;
    [SerializeField]
    private float wallCheckDistance;
    [SerializeField]
    private LayerMask groundLayer;
    [SerializeField]
    private LayerMask wallLayer;

    [Header("Component")]
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rigid;
    private BoxCollider2D boxCollider;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            if (instance != this)
                Destroy(this.gameObject);
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        rigid = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        defaultSpeed = runSpeed;
        dashCool = true;
        jumpCnt = jumpCount;
        atkCnt = atkCount;
    }

    void Update()
    {
        GetInput();
        Crouch();
        Jump();
        Sliding();
        Interaction();
        Invincibilit();
    }

    void FixedUpdate()
    {
        Move();
        //LadderMove();
    }

    // #1. ������ ���� Ű �Է¹ޱ�
    void GetInput()
    {
        inputX = Input.GetAxisRaw("Horizontal");
        inputY = Input.GetAxisRaw("Vertical");

        if (Input.GetButtonDown("Attack") && atkCount > 0 && !onMove && onGround && !onCrouch && !onDamaged && !onDie && !onDash)
        {
            Attack();
        }

        if (Input.GetButtonDown("Dash") && !onAttack && onGround && !onDamaged && !onDie && !onDash && dashCool)
        {
            StartCoroutine("Dash");
        }
    }

    // #2. �̵� ����
    void Move()
    {
        if (onCrouch || onAttack || onDamaged || onDie || isWallJump || onDash)
            return;

        // #. ĳ���� �̵�
        rigid.linearVelocity = new Vector2((inputX) * runSpeed, rigid.linearVelocity.y);
        onMove = rigid.linearVelocity.x != 0 ? true : false;

        // #. ĳ������ ���ʰ� ������ �ٴ� üũ�� ����
        bool groundFront = Physics2D.Raycast(groundCheckFront.position, Vector2.down, checkDistance, groundLayer);
        bool groundBack = Physics2D.Raycast(groundCheckBack.position, Vector2.down, checkDistance, groundLayer);

        // #. ���� ���¿��� �� �Ǵ� ���ʿ� �ٴ��� �����Ǹ� �ٴڿ� �پ �̵��ϵ��� ����
        if (!onGround && (groundFront || groundBack))
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, 0);

        // #. �� �Ǵ� ������ �ٴ��� �����Ǹ� isGround�� ������
        if (groundFront || groundBack)
        {
            onGround = true;
            jumpCnt = jumpCount;
        }
        else
            onGround = false;

        animator.SetBool("onGround", onGround);

        if (inputX != 0 && !isWallJump)
        {
            // #. ����Ű�� ������ ����� ĳ���Ͱ� �ٶ󺸴� ������ �ٸ��� ĳ������ ������ ��ȯ
            if ((inputX > 0 && isRight < 0) || (inputX < 0 && isRight > 0))
            {
                FlipPlayer();
            }

            animator.SetBool("onMove", true);
        }
        else
        {
            animator.SetBool("onMove", false);
        }
    }

    // #. ���� ��ȯ
    void FlipPlayer()
    {
        // #. ������ ��ȯ
        transform.eulerAngles = new Vector3(0, Mathf.Abs(transform.eulerAngles.y - 180), 0);
        isRight *= -1;
    }

    // #3. �ɱ�
    void Crouch()
    {
        if (onMove || !onGround || onAttack || onDamaged || onDie || isWall || onDash)
            return;

        // #. ĳ���� ���̱�
        onCrouch = inputY < 0 ? true : false;
        animator.SetBool("onCrouch", onCrouch);

        if (onCrouch)
        {
            hitBox.transform.localPosition = new Vector2(0, crouchHitPos);
            rigid.linearVelocity = new Vector2(0, rigid.linearVelocity.y);
            boxCollider.offset = new Vector2(boxCollider.offset.x, crouchColOffsetY);
            boxCollider.size = new Vector2(boxCollider.size.x, crouchColSizeY);
        }
        else
        {
            hitBox.transform.localPosition = Vector2.zero;
            boxCollider.offset = new Vector2(boxCollider.offset.x, standColOffsetY);
            boxCollider.size = new Vector2(boxCollider.size.x, standColSizeY);
        }
    }

    // #4. ����
    void Jump()
    {
        if (Input.GetButtonDown("Jump") && jumpCnt > 0 && !onAttack && !onDamaged && !onDie && !onCrouch)
        {
            // #. ĳ���� ����
            rigid.linearVelocity = Vector2.up * jumpForce;
            animator.SetTrigger("doJump");
        }
        if (Input.GetButtonUp("Jump"))
        {
            jumpCnt--;
        }
    }

    // #5. ���
    IEnumerator Dash()
    {
        dashCool = false;
        onDash = true;
        invincibilityTime += 0.7f;
        gameObject.layer = 9;
        animator.SetBool("onDash", true);
        dashTime = defaultDashTime;

        if (onMove && !onCrouch)
        {
            while (dashTime > 0 && inputX != 0)
            {
                rigid.linearVelocity = new Vector2(inputX * dashSpeed, rigid.linearVelocity.y);
                dashTime -= Time.deltaTime;
                yield return null;
            }

            if (dashTime > 0)
            {
                dashTime = 0;
                gameObject.layer = 3;
                animator.SetBool("onMove", false);
            }

            yield return new WaitForSeconds(dashTime);
            runSpeed = defaultSpeed;
            animator.SetBool("onDash", false);
        }
        else if (!onMove && onCrouch)
        {
            onCrouch = true;
            animator.SetBool("onCrouch", true);
            rigid.AddForce(Vector2.left * -isRight * 280);

            yield return new WaitForSeconds(dashTime);
            rigid.linearVelocity = new Vector2(0, rigid.linearVelocity.y);
            animator.SetBool("onDash", false);
        }
        else if (!onMove && !onCrouch)
        {
            while (dashTime > 0 && inputX == 0)
            {
                dashTime -= Time.deltaTime;
                rigid.linearVelocity = new Vector2(dashSpeed * isRight, 0);
                animator.SetBool("onMove", true);
                animator.SetBool("onDash", true);
                yield return null;
            }

            dashTime = 0;
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, rigid.linearVelocity.y);
            animator.SetBool("onMove", false);
            animator.SetBool("onDash", false);
        }
        onDash = false;
        gameObject.layer = 3;

        yield return new WaitForSeconds(dashCooldown);
        dashCool = true;
    }

    // #6. �� Ÿ��
    void Sliding()
    {
        if (!onGround)
        {
            isWall = Physics2D.Raycast(wallCheck.position, Vector2.right * isRight, wallCheckDistance, wallLayer);
            animator.SetBool("onSliding", isWall);

            if (isWall)
            {
                rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, rigid.linearVelocity.y * slidingSpeed);
                isWallJump = false;

                if (Input.GetButtonDown("Jump"))
                {
                    isWallJump = true;
                    animator.SetTrigger("doJump");
                    Invoke("FreezeX", 0.3f);
                    rigid.linearVelocity = new Vector2(-isRight * wallJumpPower, 0.9f * wallJumpPower);
                    FlipPlayer();
                }
            }
        }
        else
        {
            animator.SetBool("onSliding", false);
        }
    }

    void FreezeX()
    {
        isWallJump = false;
    }

    // #7. ��ȣ�ۿ�
    void Interaction()
    {
        if (Input.GetButtonDown("Interaction") && targetObject != null)
        {
            switch (targetObject.GetComponent<Object>().objectType.ToString())
            {
                case "Gate":
                    targetObject.GetComponent<Gate>().UseGate();
                    break;

                case "VillageGate":
                    targetObject.GetComponent<VillageGate>().UseVillageGate();
                    break;

                case "PortalRing":
                    Vector2 destination = targetObject.GetComponent<Portal>().targetPortal;
                    Vector2 offset = targetObject.GetComponent<Portal>().offsetBackground;
                    StartCoroutine(GameManager.instance.Teleport());
                    break;

                case "TreasureBox":
                    targetObject.GetComponent<TreasureBox>().Spawn();
                    break;
                /*
                case "Ladder":
                    transform.position = new Vector2(targetObject.transform.position.x, transform.position.y);     
                    onLadder = true;
                    break;
                */
            }
        }
    }

    // #8. ���� ����
    void Attack()
    {
        onAttack = true;
        atkCnt--;
        animator.SetTrigger("doAttack");
    }

    // #. ���� �ڽ� Ȱ��ȭ
    public IEnumerator OnAttackBox()
    {
        attackBox.SetActive(true);

        yield return new WaitForSeconds(0.1f);
        attackBox.SetActive(false);
    }

    // #. ���� ���� ��Ȱ��ȭ
    void OffAttack()
    {
        onAttack = false;
        atkCnt = atkCount;
    }

    // #9. �ǰ� ����
    public void OnDamaged(Vector2 targetPos, int damage)
    {
        if (onDamaged || onDie)
            return;

        playerHP -= damage;
        GameManager.instance.HPSetting("Damage");
        rigid.linearVelocity = Vector2.zero;

        if (playerHP <= 0)
            Die();
        else
        {
            onDamaged = true;
            invincibilityTime += 2f;

            // #. ���̾� ���� (Invincibility)
            gameObject.layer = 9;

            // #. �÷� ����
            spriteRenderer.color = new Color(1, 1, 1, 0.6f);

            // #. �˹�
            int dirc = transform.position.x - targetPos.x > 0 ? 1 : -1;
            rigid.AddForce(new Vector2(dirc, 0) * 2, ForceMode2D.Impulse);
            animator.SetBool("onCrouch", false);
            animator.SetTrigger("doDamaged");

            // #. �ǰ� ���� ����
            StartCoroutine("OffDamaged");
        }
    }

    // #. ��� �ǰ� ����
    public IEnumerator Holding(int damage, Vector2 knockback)
    {
        if (!onDamaged)
        {
            onDamaged = true;
            animator.enabled = false;
            spriteRenderer.sprite = hitPose;

            yield return new WaitForSeconds(2f);
            playerHP -= damage;
            GameManager.instance.HPSetting("Damage");

            // #. �˹�
            boxCollider.isTrigger = true;
            rigid.gravityScale = 0;
            animator.enabled = true;
            animator.SetTrigger("doDamaged");
            rigid.AddForce(knockback * 2, ForceMode2D.Impulse);

            yield return new WaitForSeconds(0.5f);
            boxCollider.isTrigger = false;
            rigid.gravityScale = 1.6f;
            onDamaged = false;
        }
    }

    // #. ������ ó���� ��������
    void Invincibilit()
    {
        float time = Mathf.Clamp(invincibilityTime, 0, 2);

        if (time > 0)
        {
            invincibilityTime -= Time.deltaTime;
            hitBox.SetActive(false);
        }
        else
        {
            hitBox.SetActive(true);
        }
    }

    // #. �ǰ� ���� ��Ȱ��ȭ
    IEnumerator OffDamaged()
    {
        yield return new WaitForSeconds(0.5f);
        onDamaged = false;

        yield return new WaitForSeconds(2f);
        gameObject.layer = 3;
        spriteRenderer.color = new Color(1, 1, 1, 1);

        // ������ �ǰ� ���׿� ���� ���� ����
        if (onAttack)
            OffAttack();
    }

    // #10. �÷��̾� ���� ����
    void Die()
    {
        onDie = true;
        gameObject.layer = 9;
        animator.SetTrigger("doDie");

        StartCoroutine(GameManager.instance.GameOver());
    }

    // #11. �÷��̾� ��Ȱ
    public void Resurrection()
    {
        transform.position = new Vector2(GameManager.instance.startPointX[GameManager.instance.stageNum], GameManager.instance.startPointY[GameManager.instance.stageNum]);
        onDie = false;
        playerHP = maxHP;
        UIManager.instance.SetHP(playerHP);
        animator.SetTrigger("doResurrection");
        gameObject.layer = 3;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Object"))
            targetObject = collision.gameObject;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Object"))
            targetObject = null;
    }

    // #. �ٴ� üũ Ray�� ��ȭ�鿡 ǥ��
    void OnDrawGizmos()
    {
        if (onGround)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(groundCheckFront.position, Vector2.down * checkDistance);
            Gizmos.DrawRay(groundCheckBack.position, Vector2.down * checkDistance);
        }
        else if (isWall)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(wallCheck.position, Vector2.right * isRight * wallCheckDistance);
        }
    }

    /*
    void LadderMove()
    {
        if (onLadder)
        {
            gameObject.layer = 11;
            onGround = false;
            animator.SetBool("ladderMove", true);
            animator.SetBool("onMove", inputY != 0 ? true : false);
            rigid.gravityScale = 0;
            bool ladderCheck = false;

            if (inputY != 0)
            {
                animator.speed = 1f;
                rigid.velocity = new Vector2(0, inputY * defaultSpeed * 0.4f);

                switch (inputY)
                {
                    case 1:
                        ladderCheck = Physics2D.Raycast(ladderCheckTop.position, Vector2.up, checkDistance * 0.6f, groundLayer);
                        break;
                    case -1:
                        ladderCheck = Physics2D.Raycast(ladderCheckBot.position, Vector2.down, checkDistance * 0.6f, groundLayer);
                        break;
                }

                if (ladderCheck)
                {
                    gameObject.layer = 3;
                    rigid.gravityScale = 1.6f;
                    float moveDir = inputY == 1 ? 0.7f : 0.2f;
                    transform.position = new Vector2(transform.position.x, transform.position.y + moveDir);

                    Debug.Log("�ٴڰ���");
                    animator.SetBool("ladderMove", false);
                    onGround = true;
                    onLadder = false;
                }
            }
            else
            {
                animator.speed = 0f;
                rigid.velocity = Vector2.zero;
            }
        }
    }
    */
}