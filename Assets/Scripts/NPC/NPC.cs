using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class NPC : MonoBehaviour
{
    [Header("Home & Life")]
    public Transform home;
    public float minOutsideTime = 300f;
    public float maxOutsideTime = 600f;
    public Vector2 respawnDelay = new Vector2(1f, 5f);

    [Header("Idle")]
    public Vector2 idleTimeRange = new Vector2(2f, 5f);

    [Header("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed = 4.5f;

    [Header("Chance (%)")]
    [Range(0, 100)] public float walkChance = 70f;
    [Range(0, 100)] public float runChance = 25f;
    [Range(0, 100)] public float idleChance = 5f;

    [Header("Animation Thresholds")]
    public float walkSpeedThreshold = 0.1f;
    public float runSpeedThreshold = 3f;

    NavMeshAgent agent;
    Animator anim;

    float outsideTimer;
    float idleTimer;
    float allowedOutsideTime;

    bool goingHome;
    bool isIdle;

    int sidewalkMask;
    System.Action onDestroyed;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        sidewalkMask = 1 << NavMesh.GetAreaFromName("SideWalk");
        agent.areaMask = sidewalkMask;

        if (anim != null)
            anim.applyRootMotion = false;
    }

    public void Init(Transform homeTransform, System.Action destroyedCallback)
    {
        home = homeTransform;
        onDestroyed = destroyedCallback;

        transform.position = home.position;
        agent.Warp(home.position);
        agent.isStopped = true;

        allowedOutsideTime = Random.Range(minOutsideTime, maxOutsideTime);
        outsideTimer = 0f;

        StartCoroutine(StartAfterDelay(3f));
    }

    IEnumerator StartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        DecideNextAction();
    }

    void Update()
    {
        UpdateAnimation();

        if (!goingHome)
        {
            outsideTimer += Time.deltaTime;
            if (outsideTimer >= allowedOutsideTime)
                GoHome();
        }

        if (isIdle)
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
                DecideNextAction();
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            if (goingHome)
            {
                onDestroyed?.Invoke();
                Destroy(gameObject);
            }
            else
            {
                DecideNextAction();
            }
        }
    }

    void DecideNextAction()
    {
        isIdle = false;
        agent.isStopped = false;

        float roll = Random.Range(0f, 100f);

        if (roll < idleChance)
        {
            StartIdle();
        }
        else if (roll < idleChance + walkChance)
        {
            agent.speed = walkSpeed;
            WanderToNewPoint();
        }
        else
        {
            agent.speed = runSpeed;
            WanderToNewPoint();
        }
    }

    void StartIdle()
    {
        isIdle = true;
        agent.isStopped = true;
        idleTimer = Random.Range(idleTimeRange.x, idleTimeRange.y);
    }

    void WanderToNewPoint()
    {
        Vector3 randomPoint = GetRandomSidewalkPoint();
        agent.SetDestination(randomPoint);
    }

    void GoHome()
    {
        goingHome = true;
        isIdle = false;
        agent.isStopped = false;
        agent.speed = walkSpeed;
        agent.SetDestination(home.position);
    }

    void UpdateAnimation()
    {
        if (anim == null) return;

        float speed = agent.velocity.magnitude;

        if (isIdle)
        {
            anim.SetBool("isWalking", false);
            anim.SetBool("isRunning", false);
        }
        else if (speed > runSpeedThreshold)
        {
            anim.SetBool("isWalking", false);
            anim.SetBool("isRunning", true);
        }
        else if (speed > walkSpeedThreshold)
        {
            anim.SetBool("isWalking", true);
            anim.SetBool("isRunning", false);
        }
        else
        {
            anim.SetBool("isWalking", false);
            anim.SetBool("isRunning", false);
        }
    }

    Vector3 GetRandomSidewalkPoint()
    {
        var tri = NavMesh.CalculateTriangulation();
        if (tri.indices.Length < 3) return transform.position;

        int i = Random.Range(0, tri.indices.Length / 3) * 3;

        Vector3 v1 = tri.vertices[tri.indices[i]];
        Vector3 v2 = tri.vertices[tri.indices[i + 1]];
        Vector3 v3 = tri.vertices[tri.indices[i + 2]];

        float r1 = Random.value;
        float r2 = Random.value;
        if (r1 + r2 > 1f) { r1 = 1f - r1; r2 = 1f - r2; }

        Vector3 p = v1 + r1 * (v2 - v1) + r2 * (v3 - v1);

        if (NavMesh.SamplePosition(p, out NavMeshHit hit, 2f, sidewalkMask))
            return hit.position;

        return transform.position;
    }
}
