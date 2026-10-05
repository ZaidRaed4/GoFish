using UnityEngine;

[CreateAssetMenu(menuName = "GoFish/UI/Hand Curve Profile", fileName = "HandCurveProfile")]
public class HandCurveProfile : ScriptableObject
{
    [Header("Curves (t = 0 left ... 1 right)")]
    public AnimationCurve yCurve = AnimationCurve.EaseInOut(0, 0, 1, 0);
    public AnimationCurve rotCurve = AnimationCurve.EaseInOut(0, 0, 1, 0);

    [Header("Amplitudes")]
    [Tooltip("UI units up at the curve's peak")]
    public float yAmplitude = 50f;
    [Tooltip("Degrees of Z rotation at the curve's extremes")]
    public float rotAmplitude = 12f;

    [Header("Spacing")]
    [Tooltip("Distance between card centres before the hand gets too wide")]
    public float xSpacing = 90f;
    public float centerX = 0f;
}
