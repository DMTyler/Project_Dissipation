using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DGraphics.Dissipation.Simple
{
    public class SimpleDissipationRendererFeature : ScriptableRendererFeature
    {
        private DissipationRendererPass _pass;

        private class DissipationRendererPass : ScriptableRenderPass
        {
            private ProfilingSampler _profilingSampler = new("DissipationRendererPass");

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get("DissipationRendererPass");
                try
                {
                    using (new ProfilingScope(cmd, _profilingSampler))
                    {
                        SimpleMeshDissipationController.InjectCommand(cmd, context, ref renderingData);
                    }
                }
                finally
                {
                    CommandBufferPool.Release(cmd);
                }
                
            }
        }

        public override void Create()
        {
            _pass = new DissipationRendererPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass == null)
            {
                Debug.LogError("DissipationRendererPass is null");
                return;
            }

            renderer.EnqueuePass(_pass);
            _pass.renderPassEvent = RenderPassEvent.BeforeRendering;
        }
    }
}