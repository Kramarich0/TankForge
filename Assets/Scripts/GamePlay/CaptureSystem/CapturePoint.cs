using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class CapturePoint : MonoBehaviour
{
    [Header("Capture Settings")]
    public float captureTime = 10f;
    public float captureRatePerPlayer = 1f;
    public float neutralDecaySpeed = 1f;
    public float contestDecaySpeed = 4f;
    public int ticketsPerInterval = 10;
    public float ticketInterval = 1f;

    [Header("UI")]
    public Image captureBackgroundUI;
    public Image captureProgressUI;

    [Header("Audio / Setup")]
    public CapturePointEnum pointType = CapturePointEnum.Neutral;
    public TeamEnum startingTeam = TeamEnum.Neutral;

    private TeamEnum controllingTeam = TeamEnum.Neutral;

    
    private float captureProgress = 0f;

    private Coroutine ticketCoroutine;

    private readonly Dictionary<TeamEnum, HashSet<TeamComponent>> present = new()
    {
        { TeamEnum.Friendly, new HashSet<TeamComponent>() },
        { TeamEnum.Enemy,    new HashSet<TeamComponent>() }
    };

    
    private const float EPS = 0.001f;

    void Start()
    {
        if (pointType == CapturePointEnum.Defense)
            controllingTeam = TeamEnum.Friendly;
        else if (pointType == CapturePointEnum.Attack)
            controllingTeam = TeamEnum.Enemy;
        else
            controllingTeam = startingTeam;

        if (controllingTeam == TeamEnum.Friendly) captureProgress = captureTime;
        else if (controllingTeam == TeamEnum.Enemy) captureProgress = -captureTime;
        else captureProgress = 0f;

        UpdateUI();
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
        int net = friendlyCount - enemyCount;

        
        
        
        if (controllingTeam != TeamEnum.Neutral)
        {
            

            
            if (friendlyCount == 0 && enemyCount == 0)
            {
                
            }
            
            else if (friendlyCount == enemyCount)
            {
                
                if ((controllingTeam == TeamEnum.Friendly && friendlyCount > 0) ||
                    (controllingTeam == TeamEnum.Enemy && enemyCount > 0))
                {
                    if (controllingTeam == TeamEnum.Friendly)
                    {
                        float speed = Mathf.Max(1, friendlyCount) * captureRatePerPlayer;
                        captureProgress = Mathf.MoveTowards(captureProgress, captureTime, Time.deltaTime * speed);
                    }
                    else
                    {
                        float speed = Mathf.Max(1, enemyCount) * captureRatePerPlayer;
                        captureProgress = Mathf.MoveTowards(captureProgress, -captureTime, Time.deltaTime * speed);
                    }
                }
                else
                {
                    
                    captureProgress = Mathf.MoveTowards(captureProgress, 0f, Time.deltaTime * neutralDecaySpeed);
                }
            }
            
            else if (net > 0) 
            {
                float speed = Mathf.Max(1, net) * captureRatePerPlayer;
                captureProgress = Mathf.MoveTowards(captureProgress, captureTime, Time.deltaTime * speed);
            }
            else 
            {
                float speed = Mathf.Max(1, -net) * captureRatePerPlayer;
                captureProgress = Mathf.MoveTowards(captureProgress, -captureTime, Time.deltaTime * speed);
            }
        }
        else
        {
            
            if (friendlyCount == 0 && enemyCount == 0)
            {
                captureProgress = Mathf.MoveTowards(captureProgress, 0f, Time.deltaTime * neutralDecaySpeed);
            }
            else if (friendlyCount == enemyCount)
            {
                captureProgress = Mathf.MoveTowards(captureProgress, 0f, Time.deltaTime * neutralDecaySpeed);
            }
            else if (net > 0)
            {
                float speed = Mathf.Max(1, net) * captureRatePerPlayer;
                captureProgress = Mathf.MoveTowards(captureProgress, captureTime, Time.deltaTime * speed);
            }
            else
            {
                float speed = Mathf.Max(1, -net) * captureRatePerPlayer;
                captureProgress = Mathf.MoveTowards(captureProgress, -captureTime, Time.deltaTime * speed);
            }
        }

        
        
        
        
        if (controllingTeam != TeamEnum.Neutral)
        {
            bool stillFullyOwned =
                (controllingTeam == TeamEnum.Friendly && captureProgress >= captureTime - EPS) ||
                (controllingTeam == TeamEnum.Enemy && captureProgress <= -captureTime + EPS);

            if (!stillFullyOwned)
            {
                
                LoseControl();
            }
        }

        
        
        
        if (captureProgress >= captureTime && controllingTeam != TeamEnum.Friendly)
        {
            SetControllingTeam(TeamEnum.Friendly);
        }
        else if (captureProgress <= -captureTime && controllingTeam != TeamEnum.Enemy)
        {
            SetControllingTeam(TeamEnum.Enemy);
        }

        captureProgress = Mathf.Clamp(captureProgress, -captureTime, captureTime);
        UpdateUI();
    }

    
    private void LoseControl()
    {
        if (ticketCoroutine != null)
        {
            StopCoroutine(ticketCoroutine);
            ticketCoroutine = null;
        }
        Debug.Log($"[CapturePoint] Владелец потерян (прогресс {captureProgress}). Контроль снимается.");
        controllingTeam = TeamEnum.Neutral;
    }

    private void UpdateUI()
    {
        if (captureProgressUI == null || captureBackgroundUI == null) return;

        float normalized = captureTime > 0f ? Mathf.Abs(captureProgress) / captureTime : 0f;
        captureProgressUI.fillAmount = normalized;

        if (captureProgress > 0.001f) captureProgressUI.color = Color.green;
        else if (captureProgress < -0.001f) captureProgressUI.color = Color.red;
        else captureProgressUI.color = Color.clear;

        captureBackgroundUI.fillAmount = 1f;
        captureBackgroundUI.color = Color.gray;
    }

    private void SetControllingTeam(TeamEnum team)
    {
        controllingTeam = team;
        captureProgress = (team == TeamEnum.Friendly) ? captureTime : -captureTime;

        
        if (ticketCoroutine != null) StopCoroutine(ticketCoroutine);
        ticketCoroutine = StartCoroutine(TicketDrainRoutine());

        Debug.Log($"[CapturePoint] Точка захвачена командой: {team}");
    }

    private IEnumerator TicketDrainRoutine()
    {
        
        while (controllingTeam != TeamEnum.Neutral)
        {
            yield return new WaitForSeconds(ticketInterval);

            
            bool fullyCaptured =
                (controllingTeam == TeamEnum.Friendly && captureProgress >= captureTime - EPS) ||
                (controllingTeam == TeamEnum.Enemy && captureProgress <= -captureTime + EPS);

            if (!fullyCaptured)
            {
                
                Debug.Log("[CapturePoint] TicketDrain: владение больше не полностью — останавливаем слив.");
                yield break;
            }

            TeamEnum enemyTeam = controllingTeam == TeamEnum.Friendly ? TeamEnum.Enemy : TeamEnum.Friendly;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.DrainTickets(enemyTeam, ticketsPerInterval);
                Debug.Log($"[CapturePoint] Ticket drained from {enemyTeam} by {ticketsPerInterval}");
            }
        }
    }

    public void RemoveTeamComponent(TeamComponent tc)
    {
        if (tc == null || tc.team == TeamEnum.Neutral) return;

        if (present.ContainsKey(tc.team))
        {
            present[tc.team].Remove(tc);
            Debug.Log($"[CapturePoint] Removed destroyed tank: {tc.name}");
        }
    }

    public TeamEnum GetControllingTeam() => controllingTeam;
    public float GetCaptureProgressNormalized() => captureTime > 0f ? Mathf.Abs(captureProgress) / captureTime : 0f;
}
