using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using GoFish.Core;

public class DebugHandsOverlay : MonoBehaviour
{
    [SerializeField] GameBootstrap bootstrap;
    [SerializeField] TextMeshProUGUI text;

    [Header("Debug Options")]
    [SerializeField] bool showOnStart = true;
    [SerializeField] bool showOpponentHands = true;
    [SerializeField] bool showEngineContext = true;
    [SerializeField] KeyCode toggleKey = KeyCode.F2;

    bool _visible;

    void Awake()
    {
        if (bootstrap == null) bootstrap = FindFirstObjectByType<GameBootstrap>();

        if (!Debug.isDebugBuild)
        {
            if (text != null) text.gameObject.SetActive(false);
            enabled = false;
            return;
        }

        _visible = showOnStart;

        if (text != null)
            text.gameObject.SetActive(_visible);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            _visible = !_visible;
            if (text != null) text.gameObject.SetActive(_visible);
        }

        if (!_visible || text == null) return;
        if (bootstrap == null || bootstrap.State == null || bootstrap.Engine == null)
        {
            text.text = "Debug: waiting for GameBootstrap/Engine...";
            return;
        }

        var state = bootstrap.State;
        var engine = bootstrap.Engine;

        var sb = new StringBuilder(512);

        sb.AppendLine($"Deck: {state.DeckCount} | Phase: {engine.Phase} | Turn: {state.GetPlayer(state.CurrentTurnPlayerId).Name}");

        if (showEngineContext)
        {
            sb.AppendLine($"AskCtx: asker={engine.CurrentAskerId} target={engine.CurrentTargetId} rank={Card.RankShort(engine.CurrentRank)} reqCount={engine.RequiredCount}");
            if (engine.CandidatePool != null && engine.CandidatePool.Count > 0)
            {
                sb.Append("Pool: ");
                for (int i = 0; i < engine.CandidatePool.Count; i++)
                    sb.Append(Card.SuitSymbol(engine.CandidatePool[i])).Append(' ');
                sb.AppendLine();
            }
        }

        sb.AppendLine("----- HANDS -----");
        for (int p = 0; p < state.PlayerCount; p++)
        {
            if (!Viewer.Is(p) && !showOpponentHands)
            {
                sb.AppendLine($"{state.GetPlayer(p).Name}: ({state.GetPlayer(p).Hand.Count} cards) [hidden]");
                continue;
            }

            var ps = state.GetPlayer(p);
            sb.Append($"{ps.Name} ({ps.Hand.Count}): ");

            var cards = new List<Card>(ps.Hand.Cards);
            cards.Sort();

            for (int i = 0; i < cards.Count; i++)
            {
                sb.Append(cards[i].ToString());
                if (i < cards.Count - 1) sb.Append(", ");
            }
            sb.AppendLine();
        }

        text.text = sb.ToString();
    }
}
