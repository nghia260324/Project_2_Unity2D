using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Setting")]
    public float moveSpeed;
    public float jumpForce;
    public float currentDistance;
    public float currentTime;

    public Animator doubleJump;
    public Transform respawn;

    public LayerMask layerGrounded;
    public LayerMask layerActiveCollider;
    public TextMeshProUGUI fillDistance;

    private Animator m_Animator;
    private Rigidbody2D m_Rigidbody;
    private BoxCollider2D m_BoxCollider;
    private CoinManager m_CoinManager;
    private HeartManager m_HeartManager;
    private BulletManager m_BulletManager;

    private bool isDoubleJump;
    private bool isFalling;
    private bool isSpike;
    private bool isProtect;

    public Vector3 defPos;

    private float lastDistance;

    private void Awake()
    {
        respawn = GameObject.Find("Confiner").transform.Find("ReSpawn");
    }
    private void Start()
    {
        m_Animator = GetComponent<Animator>();
        m_Rigidbody = GetComponent<Rigidbody2D>();
        m_BoxCollider = GetComponent<BoxCollider2D>();
        m_CoinManager = GetComponent<CoinManager>();
        m_HeartManager = GetComponent<HeartManager>();
        m_BulletManager = GetComponent<BulletManager>();

        defPos = transform.position;
        isFalling = false;
        InvokeRepeating("UpdateDistanceMoved", 1f, 1f);
    }

    private void Update()
    {
        if (transform.position.x > defPos.x)
        {
            transform.position = Vector2.MoveTowards(transform.position, new Vector2(defPos.x,transform.position.y),1f);
        }
        if (isFalling)
        {
            isProtect = true;
            isDoubleJump = true;
            m_Animator.SetTrigger("hurt");
            transform.position = Vector2.MoveTowards(transform.position, respawn.position, 1f);
            StartCoroutine(IsFalling());
            StartCoroutine(IsProtect());
            return;
        }

        currentTime += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Jumping();
        }
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePosition = Input.mousePosition;
            float screenWidth = Screen.width;

            if (mousePosition.x < screenWidth / 2)
            {
                Jumping();
            }
            else
            {
                m_BulletManager.Attack();
            }
        }

        if (IsGrounded())
        {
            m_Animator.SetBool("isJumping", false);
        } else
        {
            m_Animator.SetBool("isJumping", true);
        }
    }



    private void Jumping()
    {
        if (IsGrounded() || isDoubleJump)
        {
            if (!IsGrounded())
            {
                doubleJump.SetTrigger("doubleJump");
                isDoubleJump = false;
            }
            else
            {
                isDoubleJump = true;
            }
            m_Rigidbody.velocity = Vector2.up * jumpForce;
            AudioManager.instance.PlaySFXJump();
        }
    }

    IEnumerator IsProtect()
    {
        yield return new WaitForSeconds(5f);
        isProtect = false;
    }
    private void UpdateDistanceMoved()
    {
        if (isFalling) { return; }
        lastDistance = currentDistance;
        currentDistance += moveSpeed * 2;
        //fillDistance.text = "Distance: " + currentDistance + "m";
        StartCoroutine(IncreaseNumberOverTime(currentDistance));
    }
    IEnumerator IncreaseNumberOverTime(float targetNumber)
    {
        float duration = 1f;
        float timer = 0f;

        while (lastDistance < targetNumber)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            lastDistance = (int)Mathf.Lerp(lastDistance, targetNumber, progress);
            fillDistance.text = "Distance: " + lastDistance + "m";
            yield return null;
        }
        fillDistance.text = "Distance: " + targetNumber + "m";
    }

    private bool IsGrounded()
    {
        return Physics2D.BoxCast(m_BoxCollider.bounds.center, m_BoxCollider.bounds.size, 0f, Vector2.down, 0.1f, layerGrounded);
    }

    public bool IsActiveCollider()
    {
        return Physics2D.BoxCast(m_BoxCollider.bounds.center, m_BoxCollider.bounds.size, 0f, Vector2.down, 0.1f, layerActiveCollider);
    }

    public void TakeDamage()
    {
        m_Animator.SetTrigger("hurt");
        if (isSpike || isProtect) return;
        m_HeartManager.UpdateHeart();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Coin"))
        {
            m_CoinManager.UpdateCoin(1);
            Destroy(collision.gameObject);
        }
        if (collision.gameObject.CompareTag("Chest"))
        {
            collision.gameObject.GetComponent<ChestManager>().OpenChest();
        }
        if (collision.gameObject.CompareTag("Falling"))
        {
            isFalling = true;
            TakeDamage();
        }
        if (collision.gameObject.CompareTag("Spike"))
        {
            TakeDamage();
            isSpike = true;
            StartCoroutine(IsSpike());
            //m_Rigidbody.velocity = new Vector2(respawn.position.x, m_Rigidbody.velocity.y);
            //transform.position = Vector2.MoveTowards(transform.position, new Vector2(respawn.position.x,transform.position.y), 5f);

        }
    }

    IEnumerator IsSpike()
    {
        yield return new WaitForSeconds(2f);
        isSpike = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            collision.gameObject.GetComponent<EnemyController>().DieAndRemove();
            TakeDamage();
        }
        if (collision.gameObject.CompareTag("Falling"))
        {
            isFalling = true;
            TakeDamage();
        }
    }

    IEnumerator IsFalling()
    {
        yield return new WaitForSeconds(2f);
        isFalling = false;
    }

}
