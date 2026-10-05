using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoFish.Core;

// Shows each player's books in their lay area; new ones pop in after the cards have landed
public class BooksLayAnimator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] GameBootstrap bootstrap;
    [SerializeField] TableSeats seats;
    [SerializeField] TableAnimations animations;

    [Tooltip("Longest wait for card flights and the turn overlay before new books are shown")]
    [SerializeField] float maxWaitForAnimations = 5f;

    int _seenBooksSeq;
    bool _initialized;
    float _waitingSince = -1f;

    void Awake()
    {
        if (bootstrap == null) bootstrap = FindFirstObjectByType<GameBootstrap>();
        if (seats == null) seats = FindFirstObjectByType<TableSeats>();
        if (animations == null) animations = FindFirstObjectByType<TableAnimations>();
    }

    void Update()
    {
        if (bootstrap == null || bootstrap.State == null) return;
        var state = bootstrap.State;

        if (!_initialized)
        {
            for (int pid = 0; pid < state.PlayerCount; pid++)
            {
                var lay = seats.Books(pid);
                if (lay != null) lay.SyncFromPlayer(state.GetPlayer(pid), animateNew: false);
            }
            _seenBooksSeq = state.BooksSequenceId;
            _initialized = true;
            return;
        }

        if (state.BooksSequenceId == _seenBooksSeq || WaitingForAnimations()) return;
        _seenBooksSeq = state.BooksSequenceId;

        // Check every lay area, so two players laying books in the same move both show up
        for (int pid = 0; pid < state.PlayerCount; pid++)
        {
            var lay = seats.Books(pid);
            if (lay == null) continue;

            var missing = new List<Rank>();
            foreach (var rank in state.GetPlayer(pid).BooksLaid)
                if (!lay.HasBook(rank)) missing.Add(rank);

            if (missing.Count > 0)
                StartCoroutine(AddBooks(lay, missing));
        }
    }

    bool WaitingForAnimations()
    {
        if (animations == null || !animations.IsBusy)
        {
            _waitingSince = -1f;
            return false;
        }

        if (_waitingSince < 0f) _waitingSince = Time.time;
        if (Time.time - _waitingSince < maxWaitForAnimations) return true;

        _waitingSince = -1f;
        return false;
    }

    IEnumerator AddBooks(LayAreaView lay, List<Rank> ranks)
    {
        Sfx.Play(SfxId.Book);
        foreach (var rank in ranks)
        {
            lay.EnsureBook(rank, animatePop: true);
            yield return new WaitForSeconds(0.05f);
        }
    }
}
