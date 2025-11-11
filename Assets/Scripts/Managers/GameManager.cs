using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int score = 0;
    public int friendlyTickets;
    public int enemyTickets;
    public System.Action<int, int> OnTicketsChanged;
    public System.Action<int, int> OnTankCountChanged;
    private int aliveEnemyTanks = 0;
    private int aliveFriendlyTanks = 0;
    public bool IsLevelInitialized { get; private set; }
    public int InitialFriendly { get; private set; }
    public int InitialEnemy { get; private set; }
    private bool isGameFinished = false;
    public System.Action<string> OnKillLogUpdated;
    private readonly List<string> killLog = new();
    public int maxKillLogEntries = 5;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void InitializeLevel()
    {
        int enemyBase = 0;
        int friendlyBase = 0;
        aliveEnemyTanks = 0;
        aliveFriendlyTanks = 1;

        TankAI[] allTanks = FindObjectsByType<TankAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var tank in allTanks)
        {
            if (tank.GetComponent<TeamComponent>() is TeamComponent tc)
            {
                int cost = GetTankCost(tank.tankClass);
                if (tc.team == TeamEnum.Enemy)
                {
                    enemyBase += cost;
                    aliveEnemyTanks++;
                }
                else if (tc.team == TeamEnum.Friendly)
                {
                    friendlyBase += cost;
                    aliveFriendlyTanks++;
                }
            }
        }

        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            friendlyBase += 200;
        }

        enemyTickets = enemyBase + 1000;
        friendlyTickets = friendlyBase + 1000;

        OnTicketsChanged?.Invoke(friendlyTickets, enemyTickets);
        OnTankCountChanged?.Invoke(aliveFriendlyTanks, aliveEnemyTanks);

        Debug.Log($"[GameManager] Билеты: Союзники {friendlyTickets} | Враги {enemyTickets}");
        Debug.Log($"[GameManager] Танков: Союзники {aliveFriendlyTanks} | Враги {aliveEnemyTanks}");

        InitialFriendly = friendlyTickets;
        InitialEnemy = enemyTickets;

        IsLevelInitialized = true;
    }

    int GetTankCost(TankAI.TankClass tankClass)
    {
        return tankClass switch
        {
            TankAI.TankClass.Light => 100,
            TankAI.TankClass.Medium => 200,
            TankAI.TankClass.Heavy => 300,
            _ => 150
        };
    }

    public void OnTankDestroyed(TeamEnum team, int ticketCost, string killerName = null, string victimName = null)
    {
        if (isGameFinished) return;

        string victimColor = team == TeamEnum.Friendly ? "#00FF00" : "#FF0000";
        string victimDisplay = victimName ?? (team == TeamEnum.Friendly ? "Союзник" : "Враг");
        victimDisplay = $"<color={victimColor}>{victimDisplay}</color>";

        string entry;

        if (!string.IsNullOrEmpty(killerName))
        {
            TeamEnum killerTeam = (killerName == null) ? TeamEnum.Neutral : (team == TeamEnum.Friendly ? TeamEnum.Enemy : TeamEnum.Friendly);
            string killerColor = killerTeam == TeamEnum.Friendly ? "#00FF00" : "#FF0000";
            string killerDisplay = $"<color={killerColor}>{killerName}</color>";
            bool isPlayer = gameObject.CompareTag("Player");

            entry = $"{killerDisplay} {(isPlayer ? "(Вы)" : "")} уничтожил {victimDisplay}";
        }
        else
        {
            entry = $"{victimDisplay} уничтожен";
        }

        AddKillLog(entry);

        if (team == TeamEnum.Friendly)
        {
            friendlyTickets = Mathf.Max(0, friendlyTickets - ticketCost);
            aliveFriendlyTanks = Mathf.Max(0, aliveFriendlyTanks - 1);
        }
        else if (team == TeamEnum.Enemy)
        {
            enemyTickets = Mathf.Max(0, enemyTickets - ticketCost);
            aliveEnemyTanks = Mathf.Max(0, aliveEnemyTanks - 1);
            score += ticketCost;
        }

        OnTicketsChanged?.Invoke(friendlyTickets, enemyTickets);
        OnTankCountChanged?.Invoke(aliveFriendlyTanks, aliveEnemyTanks);

        CheckVictory();
    }

    void CheckVictory()
    {
        if (isGameFinished) return;

        if (friendlyTickets <= 0)
        {
            Debug.Log("ПОРАЖЕНИЕ! Союзники уничтожены.");
            isGameFinished = true;
            GameUIManager.Instance?.ShowDefeatScreen();
        }
        else if (enemyTickets <= 0 || aliveEnemyTanks <= 0)
        {
            Debug.Log("ПОБЕДА! Враги уничтожены.");
            isGameFinished = true;
            CompleteLevel();
        }
    }

    void CompleteLevel()
    {
        int stars = CalculateStars(score);
        string sceneName = SceneManager.GetActiveScene().name;

        int levelIndex = 0;
        if (sceneName.StartsWith("Level"))
        {
            string numPart = sceneName[5..];
            if (int.TryParse(numPart, out int idx)) levelIndex = idx;
        }

        PlayerPrefs.SetInt($"Level{levelIndex}_Score", score);
        PlayerPrefs.SetInt($"Level{levelIndex}_Stars", stars);
        PlayerPrefs.SetInt($"Level{levelIndex}_Completed", 1);
        PlayerPrefs.SetInt($"Level{levelIndex + 1}_Unlocked", 1);
        PlayerPrefs.Save();

        Debug.Log($"Уровень {levelIndex} завершён! Звёзд: {stars}, Счёт: {score}");

        score += friendlyTickets;
        GameUIManager.Instance?.ShowVictoryScreen(score, stars);
    }

    int CalculateStars(int score)
    {
        if (score >= 4000) return 3;
        if (score >= 2500) return 2;
        if (score >= 1000) return 1;
        return 0;
    }

    public void OnPlayerTankDestroyed()
    {
        if (isGameFinished) return;

        Debug.Log("Игрок уничтожен. Игра окончена.");
        isGameFinished = true;
        GameUIManager.Instance?.ShowDefeatScreen();
    }

    public void DrainTickets(TeamEnum team, int amount)
    {
        if (team == TeamEnum.Friendly)
            friendlyTickets = Mathf.Max(0, friendlyTickets - amount);
        else if (team == TeamEnum.Enemy)
            enemyTickets = Mathf.Max(0, enemyTickets - amount);

        OnTicketsChanged?.Invoke(friendlyTickets, enemyTickets);
        CheckVictory();
    }

    private void AddKillLog(string entry)
    {
        killLog.Add(entry);

        if (killLog.Count > maxKillLogEntries)
            killLog.RemoveAt(0);

        OnKillLogUpdated?.Invoke(entry);
    }

}