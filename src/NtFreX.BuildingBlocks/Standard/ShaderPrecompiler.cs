using System.Text;
using System.Text.RegularExpressions;
using Veldrid;
using Veldrid.SPIRV;

namespace NtFreX.BuildingBlocks.Standard
{
    public class ShaderPrecompiler
    {
        internal abstract class ShaderSyntaxTreeNode
        {
            public abstract string Precompile();
        }

        internal class ShaderSyntaxIf : ShaderSyntaxTreeNode
        {
            internal record IfCondition (bool ConditionValue, List<ShaderSyntaxTreeNode> Children);

            public List<IfCondition> conditions;

            public ShaderSyntaxIf(List<IfCondition> conditions)
            {
                this.conditions = conditions;
            }

            public override string Precompile()
            {
                var firstTrueCondition = conditions.FirstOrDefault(x => x.ConditionValue);
                if (firstTrueCondition == null)
                    return string.Empty;

                var text = new StringBuilder();
                foreach (var content in firstTrueCondition.Children)
                {
                    text.Append(content.Precompile());
                }
                return text.ToString();
            }

            public override string ToString()
                => "{ If: " + string.Join(", ", conditions) + " }";
        }

        internal class ShaderSyntaxInclude : ShaderSyntaxTreeNode
        {
            private readonly ShaderPrecompiler currentCompiler;
            private readonly string basePath;
            private readonly string path;

            public ShaderSyntaxInclude(ShaderPrecompiler currentCompiler, string basePath, string path)
            {
                this.currentCompiler = currentCompiler;
                this.basePath = basePath;
                this.path = path;
            }

            public override string Precompile()
            {
                var folder = Path.GetDirectoryName(basePath);
                var rootedPath = path;
                if (!Path.IsPathRooted(path) && !string.IsNullOrEmpty(folder))
                    rootedPath = Path.Combine(folder, path);

                return currentCompiler.Precompile(File.ReadAllText(rootedPath), rootedPath);
            }

            public override string ToString()
                => "{ Include: path=" + path + ", base=" + basePath + " }";
        }

        internal class ShaderSyntaxText : ShaderSyntaxTreeNode
        {
            private readonly string text;

            public ShaderSyntaxText(string text)
            {
                this.text = text;
            }

            public override string Precompile()
                => text;

            public override string ToString()
                => "{ Text: " + text + " }";
        }

        internal enum ShaderSyntaxTokenType 
        { 
            If,
            Else,
            ElseIf,
            EndIf,
            Not,
            Include,
            Variable,
            Text
        }
        
        internal record ShaderSyntaxToken (ShaderSyntaxTokenType TokenType, string Text, int LineNumber, string FilePath);
        
        public class ShaderCompilationException : Exception
        {
            public string[] RawShaders { get; set; } = Array.Empty<string>();
            public string[] PreCompiledShaders { get; set; } = Array.Empty<string>();
            public string[] PreCompiledLines { get; set; } = Array.Empty<string>();

            public string? ErrorType { get; set; } = null;
            public uint? LineNumber { get; set; } = null;

            public ShaderCompilationException(string message, Exception innerException)
                : base(message, innerException) { }
        }

        private readonly List<(ShaderSyntaxTokenType TokenType, Regex Regex)> TokenMatchers = new()
        {
            { (ShaderSyntaxTokenType.If, new Regex(@"#if")) },
            { (ShaderSyntaxTokenType.ElseIf, new Regex(@"#elseif")) },
            { (ShaderSyntaxTokenType.Else, new Regex(@"#else")) },
            { (ShaderSyntaxTokenType.EndIf, new Regex(@"#endif")) },
            { (ShaderSyntaxTokenType.Not, new Regex(@"not")) },
            { (ShaderSyntaxTokenType.Not, new Regex(@"!")) },
            { (ShaderSyntaxTokenType.Include, new Regex(@"#include")) },
            { (ShaderSyntaxTokenType.Variable, new Regex(@"#{\w*}")) },
        };

        private readonly Dictionary<string, bool> flags;
        private readonly Dictionary<string, string> values;

        public ShaderPrecompiler(Dictionary<string, bool> flags, Dictionary<string, string> values)
        {
            this.flags = flags;
            this.values = values;
        }

        private static CrossCompileOptions GetOptions(GraphicsDevice gd)
        {
            SpecializationConstant[] specializations = GetSpecializations(gd);

            bool fixClipZ = (gd.BackendType == GraphicsBackend.OpenGL || gd.BackendType == GraphicsBackend.OpenGLES)
                && !gd.IsDepthRangeZeroToOne;
            bool invertY = false;

            return new CrossCompileOptions(fixClipZ, invertY, specializations);
        }

        public static SpecializationConstant[] GetSpecializations(GraphicsDevice gd)
        {
            bool glOrGles = gd.BackendType == GraphicsBackend.OpenGL || gd.BackendType == GraphicsBackend.OpenGLES;

            List<SpecializationConstant> specializations = new List<SpecializationConstant>();
            specializations.Add(new SpecializationConstant(100, gd.IsClipSpaceYInverted));
            specializations.Add(new SpecializationConstant(101, glOrGles)); // TextureCoordinatesInvertedY
            specializations.Add(new SpecializationConstant(102, gd.IsDepthRangeZeroToOne));

            PixelFormat swapchainFormat = gd.MainSwapchain.Framebuffer.OutputDescription.ColorAttachments[0].Format;
            bool swapchainIsSrgb = swapchainFormat == PixelFormat.B8_G8_R8_A8_UNorm_SRgb
                || swapchainFormat == PixelFormat.R8_G8_B8_A8_UNorm_SRgb;
            specializations.Add(new SpecializationConstant(103, swapchainIsSrgb));

            return specializations.ToArray();
        }

        public static Shader[] CompileVertexAndFragmentShaders(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Dictionary<string, bool> flags, Dictionary<string, string> values, string path, bool isDebug = false, string entryPoint = "main")
        {
            // TODO: use SpecializationConstant instead of custom variable replace?
            var precomipler = new ShaderPrecompiler(flags, values);
            var vertPath = path + ".vert";
            var fragPath = path + ".frag";
            var rawVertShader = File.ReadAllText(vertPath);
            var rawFragShader = File.ReadAllText(fragPath);
            var vertShader = precomipler.Precompile(rawVertShader, vertPath);
            var fragShader = precomipler.Precompile(rawFragShader, fragPath);

            var vertexShaderDesc = new ShaderDescription(
                ShaderStages.Vertex,
                Encoding.UTF8.GetBytes(vertShader),
                entryPoint, isDebug);
            var fragmentShaderDesc = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(fragShader),
                entryPoint, isDebug);

            try
            {
                return resourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc, GetOptions(graphicsDevice));
            }
            catch (Exception exce)
            {
                const string compileError = "Compilation failed: ";
                uint? rawLineNumber = null;
                string? errorType = null;
                if(exce.Message.StartsWith(compileError))
                {
                    var endErrorTypeIndex = exce.Message.IndexOf(":", compileError.Length);
                    var endLineIndex = exce.Message.IndexOf(":", endErrorTypeIndex + 1);

                    errorType = exce.Message.Substring(compileError.Length, endErrorTypeIndex - compileError.Length);

                    var lineText = exce.Message.Substring(endErrorTypeIndex + 1, endLineIndex - endErrorTypeIndex - 1);
                    rawLineNumber = uint.Parse(lineText);
                }

                throw new ShaderCompilationException("Compiling the fragment or vertex shader failed", exce)
                {
                    RawShaders = new [] { rawVertShader, rawFragShader },
                    PreCompiledShaders = new [] { vertShader, fragShader },
                    PreCompiledLines = rawLineNumber == null ? Array.Empty<string>() : new[] { GetPrecompiledLine(vertShader, rawLineNumber), GetPrecompiledLine(fragShader, rawLineNumber) },
                    LineNumber = rawLineNumber,
                    ErrorType = errorType
                };
            }
        }

        private static string GetPrecompiledLine(string shaderCode, uint? lineNuber)
        {
            if (lineNuber == null)
                return string.Empty;

            var lines = shaderCode.Split(Environment.NewLine);
            if (lines.Length + 1 <= lineNuber)
                return string.Empty;

            return lines[lineNuber.Value - 1];
        }

        private (Match Match, ShaderSyntaxTokenType TokenType)? TryMatchToken(string token)
        {
            foreach(var matcher in TokenMatchers)
            {
                var match = matcher.Regex.Match(token);
                if (match.Success)
                    return (match, matcher.TokenType);
            }
            return null;
        }

        private List<ShaderSyntaxToken> ParseTokens(int lineNumber, Span<string> tokens, string filePath) 
        {
            var parsedTokens = new List<ShaderSyntaxToken>();

            if (tokens.Length > 0)
            {
                var tokenValue = tokens[0];
                var tokenDefinitionMatch = TryMatchToken(tokenValue);
                var tokenDefintion = tokenDefinitionMatch == null ? ShaderSyntaxTokenType.Text : tokenDefinitionMatch.Value.TokenType;
                var match = tokenDefinitionMatch == null ? null : tokenDefinitionMatch.Value.Match;

                if (match != null && match.Index != 0)
                {
                    parsedTokens.Add(new ShaderSyntaxToken(ShaderSyntaxTokenType.Text, tokenValue.Substring(0, match.Index), lineNumber, filePath));
                }

                parsedTokens.Add(new ShaderSyntaxToken(tokenDefintion, match != null ? tokenValue.Substring(match.Index, match.Length) : tokenValue, lineNumber, filePath));

                if (match != null && match.Index + match.Length != tokenValue.Length)
                {
                    parsedTokens.Add(new ShaderSyntaxToken(ShaderSyntaxTokenType.Text, tokenValue.Substring(match.Index + match.Length, tokenValue.Length - match.Index - match.Length), lineNumber, filePath));
                }
            }

            if(tokens.Length > 1) 
            {
                parsedTokens.AddRange(ParseTokens(lineNumber, tokens.Slice(1), filePath));
            }

            return parsedTokens;
        }

        private bool TryGetNotToken(List<ShaderSyntaxToken> tokens, ref int tokenIndex)
        {
            for (; tokenIndex < tokens.Count; tokenIndex++)
            {
                if (tokens[tokenIndex].TokenType == ShaderSyntaxTokenType.Not)
                {
                    tokenIndex++;
                    return true;
                }
                else if (tokens[tokenIndex].TokenType != ShaderSyntaxTokenType.Text || !string.IsNullOrWhiteSpace(tokens[tokenIndex].Text.Replace(Environment.NewLine, "")))
                    return false;
            }
            return false;
        }

        private List<ShaderSyntaxToken> GetIfContent(List<ShaderSyntaxToken> tokens, ref int tokenIndex)
        {
            var items = new List<ShaderSyntaxToken>();
            var depth = 0;
            for (; tokenIndex < tokens.Count; tokenIndex++)
            {
                var type = tokens[tokenIndex].TokenType;
                if (type == ShaderSyntaxTokenType.If)
                    depth++;
                else if ((type == ShaderSyntaxTokenType.Else || type == ShaderSyntaxTokenType.ElseIf || type == ShaderSyntaxTokenType.EndIf) && depth == 0)
                    break;
                else if(type == ShaderSyntaxTokenType.EndIf)
                    depth--;

                items.Add(tokens[tokenIndex]);
            }
            return items;
        }

        private ShaderSyntaxIf ParseIf(List<ShaderSyntaxToken> tokens, ref int tokenIndex)
        {
            tokenIndex++;

            var conditions = new List<ShaderSyntaxIf.IfCondition>();
            var not = TryGetNotToken(tokens, ref tokenIndex);
            var conditionValue = flags[tokens[tokenIndex++].Text];
            conditions.Add(new ShaderSyntaxIf.IfCondition(not ? !conditionValue : conditionValue, ParseSyntaxTree(GetIfContent(tokens, ref tokenIndex)).ToList()));

            while (tokenIndex < tokens.Count && tokens[tokenIndex].TokenType != ShaderSyntaxTokenType.EndIf)
            {
                if (tokens[tokenIndex].TokenType == ShaderSyntaxTokenType.Else)
                {
                    tokenIndex++;
                    conditions.Add(new ShaderSyntaxIf.IfCondition(true, ParseSyntaxTree(GetIfContent(tokens, ref tokenIndex)).ToList()));
                }
                else if (tokens[tokenIndex].TokenType == ShaderSyntaxTokenType.ElseIf)
                {
                    tokenIndex++;
                    not = TryGetNotToken(tokens, ref tokenIndex);
                    conditionValue = flags[tokens[tokenIndex++].Text];
                    conditions.Add(new ShaderSyntaxIf.IfCondition(not ? !conditionValue : conditionValue, ParseSyntaxTree(GetIfContent(tokens, ref tokenIndex)).ToList()));
                }
                else
                {
                    break;
                }
            }
            return new ShaderSyntaxIf(conditions);
        }

        private IEnumerable<ShaderSyntaxTreeNode> ParseSyntaxTree(List<ShaderSyntaxToken> tokens)
        {
            for(var tokenIndex = 0; tokenIndex < tokens.Count; tokenIndex++)
            {
                if(tokens[tokenIndex].TokenType == ShaderSyntaxTokenType.If)
                {
                    yield return ParseIf(tokens, ref tokenIndex);
                }
                else if (tokens[tokenIndex].TokenType == ShaderSyntaxTokenType.Variable)
                {
                    var text = tokens[tokenIndex].Text;
                    var variableName = text.Substring(2, text.Length - 3);
                    yield return new ShaderSyntaxText((tokenIndex == 0 ? string.Empty : " ") + values[variableName]);
                }
                else if(tokens[tokenIndex].TokenType == ShaderSyntaxTokenType.Include)
                {
                    tokenIndex++;
                    if (tokens[tokenIndex].TokenType != ShaderSyntaxTokenType.Text)
                        throw new Exception($"[Line: {tokens[tokenIndex].LineNumber}, File] A file path must follow the include statement");

                    yield return new ShaderSyntaxInclude(this, basePath: tokens[tokenIndex].FilePath, path: tokens[tokenIndex].Text);
                }
                else
                {
                    yield return new ShaderSyntaxText((tokenIndex == 0 ? string.Empty : " ") + tokens[tokenIndex].Text);
                }
            }
        }

        public string Precompile(string shaderText, string filePath)
        {
            var tokens = new List<ShaderSyntaxToken>();
            var lineNumber = 1;
            foreach(var line in shaderText.Split(Environment.NewLine))
            {

                var textTokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                tokens.AddRange(ParseTokens(lineNumber, textTokens, filePath));
                tokens.Add(new ShaderSyntaxToken(ShaderSyntaxTokenType.Text, Environment.NewLine, lineNumber, filePath));

                lineNumber++;
            }

            var syntaxTree = ParseSyntaxTree(tokens);
            var text = new StringBuilder();
            foreach(var node in syntaxTree)
            {
                text.Append(node.Precompile());
            }

            return text.ToString();
        }
    }
}
