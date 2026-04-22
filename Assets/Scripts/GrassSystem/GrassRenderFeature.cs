// GrassRenderFeature is no longer needed.
// GrassComputeScript now calls Graphics.DrawMeshInstancedIndirect directly from Update,
// which bypasses URP's render pass pipeline entirely and avoids all D3D12 SRV binding issues.
// Keep this file so the URP Renderer asset reference doesn't break — it is a harmless no-op.
using UnityEngine.Rendering.Universal;

public class GrassRenderFeature : ScriptableRendererFeature
{
    public override void Create() { }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) { }
}
