using System.Collections.Generic;
using UnityEngine;
using GoFish.Core;

// Creates the match state; the engine follows once the opening deal has finished
public class GameBootstrap : MonoBehaviour
{
    [SerializeField, Range(3, 4)] int playerCount = 4;

    [Tooltip("New deal every match. Turn off to replay the fixed seed below (testing).")]
    [SerializeField] bool randomSeed = true;
    public int seed = 123;

    public TablePresenter presenter;

    public GameState State { get; private set; }
    public GameEngine Engine { get; private set; }

    void Awake()
    {
        if (randomSeed)
            seed = Random.Range(1, int.MaxValue);
    }

    void Start()
    {
        var names = new List<string>();
        for (int i = 0; i < playerCount; i++)
            names.Add(i == 0 ? "You" : $"Bot {i}");

        State = new GameState(names, seed, startingPlayerId: 0);

        if (presenter != null)
            presenter.Bind(State);
    }

    public void CreateEngineAfterDeal()
    {
        if (Engine == null)
            Engine = new GameEngine(State);
    }
}
