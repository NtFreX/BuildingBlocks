using NtFreX.BuildingBlocks.Standard.Extensions;
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

        internal class ShaderIncludeContext
        {
            private HashSet<string> includes = new();

            public bool TryAddInclude(string path, string basePath, out string normalized)
            {
                normalized = PathExtensions.NormalizeRelativePath(path, basePath);

                if (includes.Contains(normalized))
                    return false;
                
                includes.Add(normalized);
                return true;
            }
        }

        internal class ShaderSyntaxInclude : ShaderSyntaxTreeNode
        {
            private readonly ShaderPrecompiler currentCompiler;
            private readonly ShaderIncludeContext includeContext;
            private readonly string basePath;
            private readonly string path;

            public ShaderSyntaxInclude(ShaderPrecompiler currentCompiler, ShaderIncludeContext includeContext, string basePath, string path)
            {
                this.currentCompiler = currentCompiler;
                this.includeContext = includeContext;
                this.basePath = basePath;
                this.path = path;
            }

            public override string Precompile()
            {
                if(includeContext.TryAddInclude(path, basePath, out var normalizedPath))
                    return currentCompiler.Precompile(File.ReadAllText(normalizedPath), normalizedPath);

                return string.Empty;
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

            bool fixClipZ = (gd.BackendType == GraphicsBackend.OpenGL || gd.BackendType == GraphicsBackend.OpenGLES) && !gd.IsDepthRangeZeroToOne;
            bool invertY = false; //TODO: why no pass gd.IsClipSpaceYInverted? currently SpecializationConstant 100 has same functionallity?;

            return new CrossCompileOptions(fixClipZ, invertY, specializations);
        }

        public static SpecializationConstant[] GetSpecializations(GraphicsDevice gd)
        {
            bool glOrGles = gd.BackendType == GraphicsBackend.OpenGL || gd.BackendType == GraphicsBackend.OpenGLES;

            var specializations = new List<SpecializationConstant>
            {
                new SpecializationConstant(100, gd.IsClipSpaceYInverted),
                new SpecializationConstant(101, glOrGles), // TextureCoordinatesInvertedY
                new SpecializationConstant(102, gd.IsDepthRangeZeroToOne)
            };

            PixelFormat swapchainFormat = gd.MainSwapchain.Framebuffer.OutputDescription.ColorAttachments[0].Format;
            bool swapchainIsSrgb = swapchainFormat == PixelFormat.B8_G8_R8_A8_UNorm_SRgb
                || swapchainFormat == PixelFormat.R8_G8_B8_A8_UNorm_SRgb;
            specializations.Add(new SpecializationConstant(103, swapchainIsSrgb));

            return specializations.ToArray();
        }

        public static Shader CompileComputeShader(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Dictionary<string, bool> flags, Dictionary<string, string> values, string path, bool isDebug = false, string entryPoint = "main")
        {
            var precomipler = new ShaderPrecompiler(flags, values);
            var rawValue = File.ReadAllText(path);
            var precompiled = precomipler.Precompile(rawValue, path);

            try
            {
                return resourceFactory.CreateFromSpirv(new ShaderDescription(ShaderStages.Compute, Encoding.UTF8.GetBytes(precompiled), entryPoint, isDebug), GetOptions(graphicsDevice));
            }
            catch (Exception ex)
            {
                TryHandleCompilerError(ex, new [] { rawValue }, new [] { precompiled } );
                throw new Exception();
            }
        }

        public static (Shader VertexShader, Shader FragementShader) CompileVertexAndFragmentShaders(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Dictionary<string, bool> flags, Dictionary<string, string> values, string path, bool isDebug = false, string entryPoint = "main")
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
                var shaders = resourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc, GetOptions(graphicsDevice));
                return (shaders[0], shaders[1]);
            }
            catch (Exception exce)
            {
                TryHandleCompilerError(exce, new[] { rawVertShader, rawFragShader }, new[] { vertShader, fragShader });
                throw new Exception();
            }
        }

        private static void TryHandleCompilerError(Exception exce, string[] rawShaders, string[] precompiledShaders)
        {
            const string compileError = "Compilation failed: ";
            uint? rawLineNumber = null;
            string? errorType = null;
            if (exce.Message.StartsWith(compileError))
            {
                var endErrorTypeIndex = exce.Message.IndexOf(":", compileError.Length);
                var endLineIndex = exce.Message.IndexOf(":", endErrorTypeIndex + 1);

                errorType = exce.Message[compileError.Length..endErrorTypeIndex];

                var lineText = exce.Message.Substring(endErrorTypeIndex + 1, endLineIndex - endErrorTypeIndex - 1);
                rawLineNumber = uint.Parse(lineText);
            }

            var lines = rawLineNumber == null ? Array.Empty<string>() : precompiledShaders.Select(shader => GetPrecompiledLine(shader, rawLineNumber)).ToArray();
            var errorLineText = rawLineNumber != null ? Environment.NewLine + string.Join(Environment.NewLine, lines.Select(line => $"L{rawLineNumber}: {line}")) : string.Empty;
            throw new ShaderCompilationException("Compiling the fragment or vertex shader failed." + errorLineText, exce)
            {
                RawShaders = rawShaders,
                PreCompiledShaders = precompiledShaders,
                PreCompiledLines = lines,
                LineNumber = rawLineNumber,
                ErrorType = errorType
            };
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

        private static bool TryGetNotToken(List<ShaderSyntaxToken> tokens, ref int tokenIndex)
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

        private static List<ShaderSyntaxToken> GetIfContent(List<ShaderSyntaxToken> tokens, ref int tokenIndex)
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
                else if (type == ShaderSyntaxTokenType.EndIf)
                    depth--;

                items.Add(tokens[tokenIndex]);
            }
            return items;
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
                var match = tokenDefinitionMatch?.Match;

                if (match != null && match.Index != 0)
                {
                    parsedTokens.Add(new ShaderSyntaxToken(ShaderSyntaxTokenType.Text, tokenValue[..match.Index], lineNumber, filePath));
                }

                parsedTokens.Add(new ShaderSyntaxToken(tokenDefintion, match != null ? tokenValue.Substring(match.Index, match.Length) : tokenValue, lineNumber, filePath));

                if (match != null && match.Index + match.Length != tokenValue.Length)
                {
                    parsedTokens.Add(new ShaderSyntaxToken(ShaderSyntaxTokenType.Text, tokenValue.Substring(match.Index + match.Length, tokenValue.Length - match.Index - match.Length), lineNumber, filePath));
                }
            }

            if(tokens.Length > 1) 
            {
                parsedTokens.AddRange(ParseTokens(lineNumber, tokens[1..], filePath));
            }

            return parsedTokens;
        }

        private ShaderSyntaxIf ParseIf(List<ShaderSyntaxToken> tokens, ShaderIncludeContext includeContext, ref int tokenIndex)
        {
            tokenIndex++;

            var conditions = new List<ShaderSyntaxIf.IfCondition>();
            var not = TryGetNotToken(tokens, ref tokenIndex);
            var conditionValue = flags[tokens[tokenIndex++].Text];
            conditions.Add(new ShaderSyntaxIf.IfCondition(not ? !conditionValue : conditionValue, ParseSyntaxTree(GetIfContent(tokens, ref tokenIndex), includeContext).ToList()));

            while (tokenIndex < tokens.Count && tokens[tokenIndex].TokenType != ShaderSyntaxTokenType.EndIf)
            {
                if (tokens[tokenIndex].TokenType == ShaderSyntaxTokenType.Else)
                {
                    tokenIndex++;
                    conditions.Add(new ShaderSyntaxIf.IfCondition(true, ParseSyntaxTree(GetIfContent(tokens, ref tokenIndex), includeContext).ToList()));
                }
                else if (tokens[tokenIndex].TokenType == ShaderSyntaxTokenType.ElseIf)
                {
                    tokenIndex++;
                    not = TryGetNotToken(tokens, ref tokenIndex);
                    conditionValue = flags[tokens[tokenIndex++].Text];
                    conditions.Add(new ShaderSyntaxIf.IfCondition(not ? !conditionValue : conditionValue, ParseSyntaxTree(GetIfContent(tokens, ref tokenIndex), includeContext).ToList()));
                }
                else
                {
                    break;
                }
            }
            return new ShaderSyntaxIf(conditions);
        }

        private IEnumerable<ShaderSyntaxTreeNode> ParseSyntaxTree(List<ShaderSyntaxToken> tokens, ShaderIncludeContext includeContext)
        {
            for(var tokenIndex = 0; tokenIndex < tokens.Count; tokenIndex++)
            {
                if(tokens[tokenIndex].TokenType == ShaderSyntaxTokenType.If)
                {
                    yield return ParseIf(tokens, includeContext, ref tokenIndex);
                }
                else if (tokens[tokenIndex].TokenType == ShaderSyntaxTokenType.Variable)
                {
                    var text = tokens[tokenIndex].Text;
                    var variableName = text[2..^1];
                    yield return new ShaderSyntaxText((tokenIndex == 0 ? string.Empty : " ") + values[variableName]);
                }
                else if(tokens[tokenIndex].TokenType == ShaderSyntaxTokenType.Include)
                {
                    tokenIndex++;
                    if (tokens[tokenIndex].TokenType != ShaderSyntaxTokenType.Text)
                        throw new Exception($"[Line: {tokens[tokenIndex].LineNumber}, File] A file path must follow the include statement");

                    yield return new ShaderSyntaxInclude(this, includeContext, basePath: tokens[tokenIndex].FilePath, path: tokens[tokenIndex].Text);
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

            var includeContext = new ShaderIncludeContext();
            var syntaxTree = ParseSyntaxTree(tokens, includeContext);
            var text = new StringBuilder();
            foreach(var node in syntaxTree)
            {
                text.Append(node.Precompile());
            }

            return text.ToString();
        }
    }
}
