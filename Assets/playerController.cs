using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class playerController : MonoBehaviour
{
    private Animator anim = null;
    private Rigidbody rb; //リジッドボディを取得するための変数
    public float upForce = 8; //上方向にかける力
    Rigidbody2D rigidBody2D;
    private float jumpForce = 10.0f;
    public GroundCheck ground;
    private bool isGround = false;
    public float speed;
    private string enemyTag = "Enemy";
    private string deadAreaTag = "DeadArea";
    private bool isDead = false;
    private CapsuleCollider2D capcol = null;
    [Header("踏みつけ判定の高さの割合")] public float stepOnRate;
    private bool isOtherJump = false;
    private float otherJumpHeight = 0.0f;
    private bool isDown = false;
    private float jumpPos = 0.0f;
    private bool isJump = false;
    private float jumpTime = 0.0f;
    public float gravity;
    public float jumpSpeed;//ジャンプする速度
    public float jumpHeight;//ジャンプの最高点
    private bool isContinue = false;
    private float continueTime = 0.0f;
    private float blinkTime = 0.0f;
    private SpriteRenderer sr = null;
    private bool nonDownAnim = false;
    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        rigidBody2D = GetComponent<Rigidbody2D>();
        capcol = GetComponent<CapsuleCollider2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (isContinue)
        {
            //明滅　ついている時に戻る
            if (blinkTime > 0.2f)
            {
                sr.enabled = true;
                blinkTime = 0.0f;
            }
            //明滅　消えているとき
            else if (blinkTime > 0.1f)
            {
                sr.enabled = false;
            }
            //明滅　ついているとき
            else
            {
                sr.enabled = true;
            }

            //1秒たったら明滅終わり
            if (continueTime > 1.0f)
            {
                isContinue = false;
                blinkTime = 0f;
                continueTime = 0f;
                sr.enabled = true;
            }
            else
            {
                blinkTime += Time.deltaTime;
                continueTime += Time.deltaTime;
            }
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        anim.SetBool("isGround", isGround);
        //接地判定を得る
        isGround = ground.IsGround();

        //キー入力されたら行動する
        float horizontalKey = Input.GetAxis("Horizontal");
        float xSpeed = 0.0f;
        float ySpeed = -gravity;
        float verticalKey = Input.GetAxis("Vertical");
        if (!isDead && !GManager.instance.isGameOver)
        {
            if (isGround)
            {
                if (verticalKey > 0)
                {
                    ySpeed = jumpSpeed;
                    jumpPos = transform.position.y; //ジャンプした位置を記録する
                    isJump = true;
                    anim.SetBool("jump", isJump);
                }
                else
                {
                    isJump = false;
                    anim.SetBool("jump", isJump);
                }
            }
            else if (isJump)
            {
                //上ボタンを押されている。かつ、現在の高さがジャンプした位置から自分の決めた位置より下ならジャンプを継続する
                if (verticalKey > 0 && jumpPos + jumpHeight > transform.position.y)
                {
                    ySpeed = jumpSpeed;
                }
                else
                {
                    isJump = false;
                    anim.SetBool("jump", isJump);
                }
            }
            if (horizontalKey > 0)
            {
                transform.localScale = new Vector3(-1, 1, 1);
                anim.SetBool("run", true);
                xSpeed = speed;
            }
            else if (horizontalKey < 0)
            {
                transform.localScale = new Vector3(1, 1, 1);
                anim.SetBool("run", true);
                xSpeed = -speed;
            }
            else
            {
                anim.SetBool("run", false);
                xSpeed = 0.0f;
            }
            rigidBody2D.velocity = new Vector2(xSpeed, ySpeed);
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.tag == enemyTag)
        {
            //踏みつけ判定になる高さ
            float stepOnHeight = (capcol.size.y * (stepOnRate / 100f));
            //踏みつけ判定のワールド座標
            float judgePos = transform.position.y - (capcol.size.y / 2f) + stepOnHeight;
            foreach (ContactPoint2D p in collision.contacts)
            {
                if (p.point.y < judgePos)
                {
                    ObjectCollision o = collision.gameObject.GetComponent<ObjectCollision>();
                    if (o != null)
                    {
                        otherJumpHeight = o.boundHeight;    //踏んづけたものから跳ねる高さを取得する
                        o.playerStepOn = true;        //踏んづけたものに対して踏んづけた事を通知する
                        jumpPos = transform.position.y; //ジャンプした位置を記録する
                        isOtherJump = true;
                        isJump = false;
                        anim.SetBool("jump", isJump);
                        jumpTime = 0.0f;
                    }
                    else
                    {
                        Debug.Log("ObjectCollisionが付いてないよ!");
                    }
                }
                else
                {
                    ReceiveDamage(true);
                    break;
                }
            }
        }
    }
    // 敵に当たった時
    private void ReceiveDamage(bool downAnim)
    {
        if (isDown)
        {
            return;
        }
        else
        {
            if (downAnim)
            {
                anim.Play("dead");
            }
            else
            {
                nonDownAnim = true;
            }
            isDead = true;
            GManager.instance.SubHeartNum();
        }
    }


    //ダウンアニメーションが完了しているかどうか
    private bool IsDownAnimEnd()
    {
        if (isDead && anim != null)
        {
            AnimatorStateInfo currentState = anim.GetCurrentAnimatorStateInfo(0);
            if (currentState.IsName("dead"))
            {
                if (currentState.normalizedTime >= 1)
                {
                    return true;
                }
            }
        }
        return false;
    }

    // 落ちた時の判定
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == deadAreaTag)
        {
            ReceiveDamage(false);
        }
    }

    // コンテニュー待機状態か
    public bool IsContinueWaiting()
    {
        if (GManager.instance.isGameOver)
        {
            // ゲームオーバー
            return false;
        }
        else
        {
            // ゲームオーバーでない
            return IsDownAnimEnd() || nonDownAnim;
        }
    }

    // コンテニュー
    public void ContinuePlayer()
    {
        isDead = false;
        isContinue = true;
        anim.Play("standPeople");
        isJump = false;
        isOtherJump = false;
        nonDownAnim = false;
    }
}
