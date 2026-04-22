using System.Collections.Generic;
using UnityEngine;

// Attach to any object that should bend nearby grass (player, NPC, projectile, etc.).
// GrassComputeScript reads ShaderInteractor.all each frame — no extra setup required.
public class ShaderInteractor : MonoBehaviour
{
    public float radius = 1f;

    public static readonly List<ShaderInteractor> all = new List<ShaderInteractor>();

    void OnEnable()  => all.Add(this);
    void OnDisable() => all.Remove(this);
}
