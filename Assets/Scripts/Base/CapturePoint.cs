using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class CapturePoint : MonoBehaviour
{
    [Header("Capture Settings")]
    public float captureTime = 10f;
    public int ticketsPerInterval = 10;
    public float ticketInterval = 1f;
    [Header("UI Elements")]
    public Image captureBackgroundUI;
    public Image captureProgressUI;
    [Header("Audio")]
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

    private TeamEnum pendingCapturer = TeamEnum.Neutral;
    private TeamEnum currentFillTeam = TeamEnum.Neutral;

    void Start()
    {
        if (pointType == CapturePointEnum.Defense)
            controllingTeam = TeamEnum.Friendly;
        else if (pointType == CapturePointEnum.Attack)
            controllingTeam = TeamEnum.Enemy;
        else
            controllingTeam = startingTeam;

        captureProgress = (controllingTeam == TeamEnum.Neutral) ? 0f : captureTime;
        currentFillTeam = (controllingTeam == TeamEnum.Neutral) ? TeamEnum.Neutral : controllingTeam;

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
        Debug.Log("Some Enter");
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
        Debug.Log("Some Exit");
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

                captureProgress = Mathf.MoveTowards(captureProgress, 0f, Time.deltaTime * 0.5f);
                pendingCapturer = TeamEnum.Neutral;
                if (captureProgress <= 0f)
                    currentFillTeam = TeamEnum.Neutral;
            }
            else if (dominantTeam != TeamEnum.Neutral)
            {

                if (pendingCapturer != dominantTeam)
                    pendingCapturer = dominantTeam;

                float speed = Mathf.Max(1, (dominantTeam == TeamEnum.Friendly ? friendlyCount : enemyCount));
                captureProgress += Time.deltaTime * speed;

                currentFillTeam = pendingCapturer;

                if (captureProgress >= captureTime)
                {

                    SetControllingTeam(pendingCapturer);
                    pendingCapturer = TeamEnum.Neutral;
                }
            }
            else
            {

                captureProgress = Mathf.MoveTowards(captureProgress, 0f, Time.deltaTime * 0.5f);
                if (captureProgress <= 0f)
                {
                    pendingCapturer = TeamEnum.Neutral;
                    currentFillTeam = TeamEnum.Neutral;
                }
            }
        }

        else
        {
            if (friendlyCount > 0 && enemyCount > 0)
            {

                captureProgress = captureTime;
                currentFillTeam = controllingTeam;
                pendingCapturer = TeamEnum.Neutral;
            }
            else if (dominantTeam != TeamEnum.Neutral && dominantTeam != controllingTeam)
            {

                float speed = Mathf.Max(1, (dominantTeam == TeamEnum.Friendly ? friendlyCount : enemyCount));
                captureProgress -= Time.deltaTime * speed;


                currentFillTeam = controllingTeam;


                if (captureProgress <= 0f)
                {
                    StartNeutralCaptureBy(dominantTeam);
                }
            }
            else
            {

                captureProgress = captureTime;
                currentFillTeam = controllingTeam;
                pendingCapturer = TeamEnum.Neutral;
            }
        }

        captureProgress = Mathf.Clamp(captureProgress, 0f, captureTime);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (captureProgressUI == null || captureBackgroundUI == null) return;

        float normalizedProgress = captureTime > 0f ? captureProgress / captureTime : 0f;
        captureProgressUI.fillAmount = normalizedProgress;

        captureProgressUI.color = currentFillTeam == TeamEnum.Friendly ? Color.green :
                                currentFillTeam == TeamEnum.Enemy ? Color.red : Color.clear;

        captureBackgroundUI.fillAmount = 1f;
        captureBackgroundUI.color = Color.gray;
    }

    private void SetControllingTeam(TeamEnum team)
    {
        controllingTeam = team;
        captureProgress = captureTime;
        currentFillTeam = team;
        pendingCapturer = TeamEnum.Neutral;

        if (ticketCoroutine != null) StopCoroutine(ticketCoroutine);
        ticketCoroutine = StartCoroutine(TicketDrainRoutine());

        Debug.Log($"[CapturePoint] Точка захвачена командой: {team}");
    }



    private void StartNeutralCaptureBy(TeamEnum capturer)
    {

        if (ticketCoroutine != null) StopCoroutine(ticketCoroutine);
        ticketCoroutine = null;

        controllingTeam = TeamEnum.Neutral;
        captureProgress = 0f;
        pendingCapturer = capturer;
        currentFillTeam = TeamEnum.Neutral;
        Debug.Log($"[CapturePoint] Владелец потерян — нейтральная. Начинает захват: {capturer}");
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
    public float GetCaptureProgressNormalized() => captureTime > 0f ? (captureProgress / captureTime) : 0f;
}
