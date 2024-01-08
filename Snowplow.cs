using System.Collections;
using UnityEngine;

public abstract class Snowplow : MonoBehaviour
{
    public enum State
    {
        DetermineCurrent, // 确定当前状态
        RollSnowballInPlace, // 在原地滚动雪球
        DetermineDestination, // 确定目的地
        FindTarget, // 寻找目标
        NoSnowballWalk, // 没有雪球时行走
        Walk, // 行走
        ReadyToLaunch, // 准备发射
        AttackPreparation, // 攻击准备
        Hit, // 被击中
        Death // 死亡
    }

    public Snowplow nearestTarget; // 最近的目标
    public bool playerWin; // 玩家是否胜利
    public GameObject hitParticle; // 击中粒子效果
    public GameObject water; // 水的效果
    public float snowballSizeDeceleration; // 雪球大小减速
    public bool launch; // 是否发射
    public Camera mainCamera; // 主摄像机
    public bool launchEnlarge; // 发射时是否放大
    public float lifetime; // 存活时间
    public State currentState; // 当前状态
    public Animator animator; // 动画控制器
    public float snowballEnlargeSpeed; // 雪球放大速度
    public float snowballEnlargeMultiplier; // 雪球放大倍数
    public float moveSpeed; // 移动速度
    public float moveMultiplier; // 移动倍数
    public float snowballMoveSpeed; // 雪球移动速度
    public float snowballMoveMultiplier; // 雪球移动倍数
    public float snowballMaxSize; // 雪球最大尺寸
    public float snowballInitialScale; // 雪球初始尺寸
    public GameObject snowballPrefab; // 雪球预制体
    public Rigidbody rigidbody; // 刚体组件
    public bool isLanded; // 是否着陆
    public float snowballCooldown; // 雪球冷却时间
    private Transform snowball; // 雪球的Transform组件
    protected float snowballCooldownTimer; // 雪球冷却计时器

    public Transform Snowball
    {
        get => snowball;
        set
        {
            if (snowball != null && !snowball.gameObject.GetComponent<SnowballBase>().isLaunched)
                Destroy(snowball.gameObject);
            snowball = value;
        }
    }

    private void Start()
    {
        snowballSizeDeceleration = 1;
        snowballMoveSpeed = 15;
        snowballMoveMultiplier = 1;
        snowballEnlargeMultiplier = 1;
        snowballMaxSize = 1;
        launchEnlarge = false;
        moveMultiplier = 1;
        lifetime = 100;
        snowballCooldown = 2;
        isLanded = true;
        rigidbody = GetComponent<Rigidbody>();
        EnemyManager.instance.all.Add(this);
        RerollSnowball();
        Init();
    }

    private void Update()
    {
        MyUpdate();
        if (playerWin) return;
        switch (currentState)
        {
            case State.NoSnowballWalk:
                NoSnowballWalk();
                break;
            case State.Walk:
                Walk();
                AttackDetection();
                break;
            case State.ReadyToLaunch:
                ReadyToLaunch();
                break;
            case State.AttackPreparation:
                // 什么都不做，只是等待
                if (snowball != null) snowball.gameObject.GetComponent<Snowball>().snowballRoll.roll = false;
                break;
            case State.Hit:
                Hit();
                break;
            case State.Death:
                // 生成水的效果
                Instantiate(water, new Vector3
                {
                    x = transform.position.x,
                    y = 0.7f,
                    z = transform.position.z
                }, water.transform.rotation);
                SoundEffect.instance.Play(SoundEffects.CharacterFallsIntoWater);
                Destroy(gameObject);
                Destroy(snowball != null ? snowball.gameObject : null);
                EnemyManager.instance.Remove(this);
                // 检查当前存活的角色
                if (EnemyManager.instance.all.Count == 1)
                {
                    if (EnemyManager.instance.all[0] is Player)
                    {
                        SoundEffect.instance.Play(SoundEffects.GameWin);
                        Debug.Log("Win");
                    }
                }
                SnowBlockManager.instance.cooldown -= 0.16f;
                if (EnemyManager.instance.all.Count <= 4) EnemyManager.sleep *= 0.92f;
                break;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Underwater"))
        {
            Debug.Log("Death");
            moveSpeed = 0;
            currentState = State.Death;
        }
        else if (!isLanded && collision.gameObject.CompareTag("SnowBlock"))
        {
            isLanded = true;
            RerollSnowball();
        }
        else if (CompareTag("Player") && collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("6");
            // 玩家与敌人碰撞，玩家被击退
            var knockback = transform.position - collision.gameObject.transform.position;
            knockback.y = 0;
            knockback.Normalize();
            rigidbody.velocity = knockback * 6.5f;
            moveSpeed = 0;
            StartCoroutine(KnockbackCoroutine());
        }
    }

    protected virtual void Init()
    {
    }

    protected virtual void MyUpdate()
    {
    }

    protected abstract void Move();
    protected abstract void AttackDetection();

    protected virtual void NoSnowballWalk()
    {
        Move();
        snowballCooldownTimer -= Time.deltaTime;
        if (snowballCooldownTimer <= 0)
        {
            snowballCooldownTimer = snowballCooldown;
            // 雪球出现
            snowball = Instantiate(snowballPrefab).transform;
            snowball.GetComponent<SnowballBase>().Ascription = this;
            if (this is Enemy) snowball.GetComponent<SnowballBase>().isEnemy = true;
            currentState = State.Walk;
            snowballEnlargeSpeed = 0.2f;
        }
    }

    protected virtual void Walk()
    {
        CheckSnowball();
        Move();
    }

    protected virtual void CheckSnowball()
    {
        if (snowball != null && snowball.lossyScale.x > 0.5f)
            RollSnowball();
        else
            NoSnowballSituation();
    }

    protected virtual void ReadyToLaunch()
    {
        if (snowball != null && snowball.localScale.x > 0.2)
        {
            currentState = State.AttackPreparation;
            snowball.gameObject.GetComponent<Snowball>().snowballEnlargeMultiplier = 0;
            snowballThrow();
        }
        else
        {
            currentState = State.Walk;
        }
    }

    protected virtual void Hit()
    {
        if (isLanded)
            currentState = State.Walk;
        else
            Move();
    }

    public void RerollSnowball()
    {
        NoSnowballSituation();
        snowballCooldownTimer = snowballCooldown;
        currentState = State.NoSnowballWalk;
        if (this is Player)
        {
        }
    }

    public void Launch()
    {
        if (snowball != null)
        {
            SoundEffect.instance.Play(SoundEffects.LaunchSound);
            snowball.GetComponent<Snowball>().isLaunched = true;
            snowball.gameObject.transform.LookAt(new Vector3
            {
                x = snowball.GetComponent<Snowball>().Orientation.position.x,
                y = snowball.gameObject.transform.position.y,
                z = snowball.GetComponent<Snowball>().Orientation.position.z
            });
            if (this is Player)
            {
                mainCamera.TargetSize = 80;
                launch = true;
                mainCamera.Instance.CameraTweenSpeed = 70;
            }
            snowball.gameObject.GetComponent<SnowballBase>().snowballRoll.roll = true;
            snowball.gameObject.GetComponent<SnowballBase>().snowballRoll.speed *= 2;
        }
        snowball = null;
        RerollSnowball();
    }

    protected virtual void RollSnowball()
    {
        animator.SetBool("Dash Forward", true);
        animator.SetBool("Run Forward", false);
    }

    protected virtual void NoSnowballSituation()
    {
        animator.SetBool("Run Forward", true);
        animator.SetBool("Dash Forward", false);
    }

    protected virtual void snowballThrow()
    {
        animator.SetTrigger("Attack 02");
        animator.SetBool("Run Forward", false);
        animator.SetBool("Dash Forward", false);
    }

    protected virtual void TakeDamage()
    {
        animator.SetTrigger("Take Damage");
        animator.SetBool("Run Forward", false);
        animator.SetBool("Dash Forward", false);
    }

    protected virtual void Death()
    {
        animator.SetTrigger("Die");
        animator.SetBool("Run Forward", false);
        animator.SetBool("Dash Forward", false);
    }

    public void SnowballCollision(Vector3 knockbackAndKnockback)
    {
        currentState = State.Hit;
        rigidbody.velocity = knockbackAndKnockback;
        isLanded = false;
        if (snowball != null) Destroy(snowball.gameObject);
        TakeDamage();
    }

    private IEnumerator KnockbackCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        moveSpeed = 4;
    }
}