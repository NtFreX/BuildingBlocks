using System.Linq;
using System.Numerics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Desktop
{
    class QubeModel
    {
        public static VertexPositionColor[] GetVertices(Vector3 offset) => GetVertices().Select(x => new VertexPositionColor(x.Position + offset, x.Color)).ToArray();

        public static VertexPositionColor[] GetVertices() => new[] {
            new VertexPositionColor(new Vector3(-0.5f, +0.5f, -0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(+0.5f, +0.5f, -0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(+0.5f, +0.5f, +0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(-0.5f, +0.5f, +0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(-0.5f,-0.5f, +0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(+0.5f,-0.5f, +0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(+0.5f,-0.5f, -0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(-0.5f,-0.5f, -0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(-0.5f, +0.5f, -0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(-0.5f, +0.5f, +0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(-0.5f, -0.5f, +0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(-0.5f, -0.5f, -0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(+0.5f, +0.5f, +0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(+0.5f, +0.5f, -0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(+0.5f, -0.5f, -0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(+0.5f, -0.5f, +0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(+0.5f, +0.5f, -0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(-0.5f, +0.5f, -0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(-0.5f, -0.5f, -0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(+0.5f, -0.5f, -0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(-0.5f, +0.5f, +0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(+0.5f, +0.5f, +0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(+0.5f, -0.5f, +0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector3(-0.5f, -0.5f, +0.5f), RgbaFloat.Red),
        };

        public static ushort[] GetIndices() => new ushort[] {                 
            0,1,2, 0,2,3,
            4,5,6, 4,6,7,
            8,9,10, 8,10,11,
            12,13,14, 12,14,15,
            16,17,18, 16,18,19,
            20,21,22, 20,22,23, 
        };
    }
}
