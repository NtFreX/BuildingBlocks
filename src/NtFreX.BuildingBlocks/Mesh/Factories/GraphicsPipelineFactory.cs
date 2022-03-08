using System.Collections.Concurrent;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Factories
{
    static class GraphicsPipelineFactory
    {
        private static readonly ConcurrentDictionary<GraphicsPipelineDescription, Pipeline> graphicPipelines = new ();

        public static Pipeline[] GetAll() => graphicPipelines.Values.ToArray();
        public static Pipeline GetGraphicsPipeline(
            GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, 
            ResourceLayout[] resourceLayouts, Framebuffer framebuffer, ShaderSetDescription shaders, 
            PrimitiveTopology primitiveTopology, PolygonFillMode fillMode, 
            BlendStateDescription blendStateDescription, FaceCullMode faceCullMode = FaceCullMode.None)
        {
            var pipelineDescription = new GraphicsPipelineDescription
            {
                BlendState = blendStateDescription,
                DepthStencilState = graphicsDevice.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerState = new RasterizerStateDescription(
                    cullMode: faceCullMode,
                    fillMode: fillMode,
                    frontFace: FrontFace.Clockwise,
                    depthClipEnabled: true,
                    scissorTestEnabled: false),
                PrimitiveTopology = primitiveTopology,
                ShaderSet = shaders,
                Outputs = framebuffer.OutputDescription,
                ResourceLayouts = resourceLayouts
            };

            if (graphicPipelines.TryGetValue(pipelineDescription, out var pipeline))
                return pipeline;

            var newPipeline = resourceFactory.CreateGraphicsPipeline(pipelineDescription);
            graphicPipelines.AddOrUpdate(pipelineDescription, newPipeline, (_, value) => value);
            return newPipeline;
        }

        public static void Dispose()
        {
            foreach(var pipeline in graphicPipelines.Values)
            {
                pipeline.Dispose();
            }
            graphicPipelines.Clear();
        }
    }
}
