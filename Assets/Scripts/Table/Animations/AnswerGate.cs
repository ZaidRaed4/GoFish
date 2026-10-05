using UnityEngine;
using GoFish.Core;

public class AnswerGate
{
    readonly TurnOverlayAnimator _overlay;
    readonly float _maxWait;
    float _waitingSince = -1f;

    public AnswerGate(TurnOverlayAnimator overlay, float maxWait)
    {
        _overlay = overlay;
        _maxWait = maxWait;
    }

    public bool IsClosedFor(GameEvent ev)
    {
        if (_overlay == null || _overlay.FirstUnrevealedIndex > ev.Index)
        {
            _waitingSince = -1f;
            return false;
        }

        if (_waitingSince < 0f) _waitingSince = Time.time;
        if (Time.time - _waitingSince < _maxWait) return true;

        _waitingSince = -1f;
        return false;
    }
}
