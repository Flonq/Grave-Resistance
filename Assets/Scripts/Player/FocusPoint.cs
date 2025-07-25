using UnityEngine;

/// <summary>
/// Player objesine eklenir. Camera limitlerini belirler.
/// </summary>
public class FocusPoint : MonoBehaviour
{
    [SerializeField]
    private float _yawLimit = 180f; // GTA SA için tam dönüş freedom

    [SerializeField]
    private float _pitchLimit = 60f; // Yukarı/aşağı bakma limiti

    public float YawLimit { get { return _yawLimit; } }
    public float PitchLimit { get { return _pitchLimit; } }
}
