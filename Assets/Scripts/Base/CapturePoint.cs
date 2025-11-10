using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class CapturePoint : MonoBehaviour
{
    [Header("Capture Settings")]
    public float captureTime = 5f;
    public int ticketsPerInterval = 1;
    public float ticketInterval = 1f;
    public Image captureProgressUI;
    public CapturePointEnum pointType = CapturePointEnum.Neutral;
    public TeamEnum startingTeam = TeamEnum.Neutral;
    private TeamEnum controllingTeam = TeamEnum.Neutral;
    private float captureProgress = 0f;
    private Coroutine ticketCoroutine;

    private readonly Dictionary<TeamEnum, HashSet<TeamComponent>> present = new()
    {
        { TeamEnum.Friendly, new HashSet<TeamComponent>() },
        { TeamEnum.Enemy, new HashSet<TeamComponent>() }
    };

    void Start()
    {
        if (pointType == CapturePointEnum.Defense)
            SetControllingTeam(TeamEnum.Friendly);
        else if (pointType == CapturePointEnum.Attack)
            SetControllingTeam(TeamEnum.Enemy);
        else
            controllingTeam = startingTeam;

        captureProgress = (controllingTeam == TeamEnum.Neutral) ? 0f : captureTime;

        Debug.Log($"[CapturePoint] Start: controllingTeam={controllingTeam}, pointType={pointType}, startingTeam={startingTeam}");
    }

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.Log("[CapturePoint] Collider set to Trigger");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<TeamComponent>(out var tc))
            tc = other.GetComponentInParent<TeamComponent>();

        if (tc != null && tc.team != TeamEnum.Neutral)
        {
            var set = present[tc.team];
            if (set.Add(tc))
            {
                Debug.Log($"[CapturePoint] Entered: {tc.gameObject.name}, team={tc.team}. Count now: {set.Count}");
            }
        }
        else
        {
            Debug.Log($"[CapturePoint] Entered by {other.name}, but no TeamComponent (or Neutral)");
        }
    }

    void OnTriggerExit(Collider other)
    {
        // аналогично: ищем компонент в родителях
        if (!other.TryGetComponent<TeamComponent>(out var tc))
            tc = other.GetComponentInParent<TeamComponent>();

        if (tc != null && tc.team != TeamEnum.Neutral)
        {
            var set = present[tc.team];
            if (set.Remove(tc))
            {
                Debug.Log($"[CapturePoint] Exited: {tc.gameObject.name}, team={tc.team}. Count now: {set.Count}");
            }
        }
    }

    void Update()
    {
        int friendlyCount = present[TeamEnum.Friendly].Count;
        int enemyCount = present[TeamEnum.Enemy].Count;

        TeamEnum dominantTeam = TeamEnum.Neutral;
        if (friendlyCount > enemyCount) dominantTeam = TeamEnum.Friendly;
        else if (enemyCount > friendlyCount) dominantTeam = TeamEnum.Enemy;

        if (controllingTeam == TeamEnum.Neutral)
        {
            if (friendlyCount > 0 && enemyCount > 0)
            {
                captureProgress = Mathf.MoveTowards(captureProgress, 0f, Time.deltaTime);
            }
            else if (dominantTeam != TeamEnum.Neutral)
            {
                float speed = Mathf.Max(1, dominantTeam == TeamEnum.Friendly ? friendlyCount : enemyCount);
                captureProgress += Time.deltaTime * speed;
                if (captureProgress >= captureTime)
                    SetControllingTeam(dominantTeam);
            }
            else
            {
                captureProgress = Mathf.MoveTowards(captureProgress, 0f, Time.deltaTime * 0.5f);
            }
        }
        else
        {
            if (friendlyCount > 0 && enemyCount > 0)
            {
                captureProgress = captureTime;
            }
            else if (dominantTeam != TeamEnum.Neutral && dominantTeam != controllingTeam)
            {
                float speed = Mathf.Max(1, (dominantTeam == TeamEnum.Friendly ? friendlyCount : enemyCount));
                captureProgress -= Time.deltaTime * speed;
                if (captureProgress <= 0f)
                    SetControllingTeam(dominantTeam);
            }
            else
            {
                captureProgress = captureTime;
            }
        }

        captureProgress = Mathf.Clamp(captureProgress, 0f, captureTime);

        if (captureProgressUI != null)
        {
            captureProgressUI.fillAmount = captureProgress / captureTime;
            captureProgressUI.color = controllingTeam == TeamEnum.Friendly ? Color.green :
                                      controllingTeam == TeamEnum.Enemy ? Color.red : Color.gray;
        }
    }

    private void SetControllingTeam(TeamEnum team)
    {
        controllingTeam = team;
        captureProgress = captureTime;

        if (ticketCoroutine != null) StopCoroutine(ticketCoroutine);
        ticketCoroutine = StartCoroutine(TicketDrainRoutine());

        Debug.Log($"[CapturePoint] Точка захвачена командой: {team}");
    }

    private IEnumerator TicketDrainRoutine()
    {
        while (controllingTeam != TeamEnum.Neutral)
        {
            yield return new WaitForSeconds(ticketInterval);

            TeamEnum enemyTeam = controllingTeam == TeamEnum.Friendly ? TeamEnum.Enemy : TeamEnum.Friendly;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.DrainTickets(enemyTeam, ticketsPerInterval);
                Debug.Log($"[CapturePoint] Ticket drained from {enemyTeam} by {ticketsPerInterval}");
            }
        }
    }

    public TeamEnum GetControllingTeam() => controllingTeam;
    public float GetCaptureProgressNormalized() => captureProgress / captureTime;
}
