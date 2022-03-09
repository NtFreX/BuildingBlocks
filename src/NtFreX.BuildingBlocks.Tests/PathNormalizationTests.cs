using NtFreX.BuildingBlocks.Standard.Extensions;
using System.IO;
using Xunit;

namespace NtFreX.BuildingBlocks.Tests
{
    public class PathNormalizationTests
    {
        [Theory]
        [InlineData(@"C:\projects\", "./test.xml", @"C:\projects\test.xml")]
        [InlineData(@"C:\projects.x", "./test.xml", @"C:\test.xml")]
        [InlineData(@"C:\projects", "./test.xml", @"C:\test.xml")]
        [InlineData(@"C:\projects\", "../test.xml", @"C:\test.xml")]
        [InlineData(@"C:\projects\", "../test/test.xml", @"C:\test\test.xml")]
        [InlineData(@"C:\projects\path\", "../test/test.xml", @"C:\projects\test\test.xml")]
        [InlineData(@"C:\projects\path\", "../test/../test.xml", @"C:\projects\test.xml")]
        public void CanNormalizePath(string root, string input, string expected)
        {
            var normalized = PathExtensions.NormalizeRelativePath(input, root);
            Assert.Equal(expected, normalized);
        }
    }
}