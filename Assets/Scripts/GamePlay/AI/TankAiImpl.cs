using UnityEngine;

public class TankAIImpl
{
    readonly TankAI owner;
    readonly AIInit init;
    readonly AIAudio audio;
    readonly AIStats stats;
    readonly AIPerception perception;
    readonly AINavigation navigation;
    readonly AICombat combat;
    readonly AIWeapons weapons;

    readonly AIStateHandler stateHandler;

    public TankAIImpl(TankAI owner)
    {
        this.owner = owner;
        init = new AIInit(owner);
        audio = new AIAudio(owner);
        stats = new AIStats(owner);
        perception = new AIPerception(owner);
        navigation = new AINavigation(owner);
        combat = new AICombat(owner);
        weapons = new AIWeapons(owner);

        stateHandler = new AIStateHandler(owner, perception, navigation, combat, weapons);
    }

    public void Awake() => init.Awake();

    public void Start() => init.Start();

    public void Update()
    {
        if (GameUIManager.Instance != null && GameUIManager.Instance.IsPaused) return;

        audio.UpdateEngineAudio();

        if (owner.agent != null)
            owner.agent.speed = owner.moveSpeed;

        stateHandler.UpdateState();
    }

    public void OnDrawGizmos()
    {
        stateHandler?.OnDrawGizmos();
    }
}
