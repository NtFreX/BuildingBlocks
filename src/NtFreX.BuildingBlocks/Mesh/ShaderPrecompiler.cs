using System.Text;
using System.Text.RegularExpressions;
using Veldrid;
using Veldrid.SPIRV;

namespace NtFreX.BuildingBlocks.Mesh
{
    public class ShaderPrecompiler
    {
        public class ShaderCompilationException : Exception
        {
            public string[] RawShaders { get; set; } = Array.Empty<string>();
            public string[] PreCompiledShaders { get; set; } = Array.Empty<string>();

            public ShaderCompilationException(string message, Exception innerException)
                : base(message, innerException) { }
        }

        internal class CompilerContext
        {
            public Stack<IfContext> IfContexts = new Stack<IfContext>();

            internal class IfContext 
            {
                public bool IsIfTrue { get; set; }
                public bool WasIfTrue { get; set; }
                public bool IsDead { get; set; }
            }
        }

        private readonly Dictionary<string, bool> flags;
        private readonly Dictionary<string, string> values;
        private readonly Regex valueRegex = new Regex(@"#{\w*}", RegexOptions.Compiled);

        public ShaderPrecompiler(Dictionary<string, bool> flags, Dictionary<string, string> values)
        {
            this.flags = flags;
            this.values = values;
        }

        public static Shader[] CompileVertexAndFragmentShaders(ResourceFactory resourceFactory, Dictionary<string, bool> flags, Dictionary<string, string> values, string path, bool isDebug = false, string entryPoint = "main")
        {
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
                return resourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
            }
            catch (Exception exce)
            {
                throw new ShaderCompilationException("Compiling the fragment or vertex shader failed", exce)
                {
                    RawShaders = new [] { rawVertShader, rawFragShader },
                    PreCompiledShaders = new [] { vertShader, fragShader }
                };
            }
        }

        private void ExecuteIfStatement(ref int lineNumber, in string filePath, in string[] tokens, in CompilerContext context, in StringBuilder text)
        {
            var ifContext = context.IfContexts.Peek();
            if (ifContext.WasIfTrue)
                return;

            ifContext.IsIfTrue = flags.TryGetValue(tokens[1], out var flagValue) && flagValue;
            TryExecuteInlineIf(ref lineNumber, filePath, context, in tokens, in text);
        }

        private void TryExecuteInlineIf(ref int lineNumber, in string filePath, in CompilerContext context, in string[] tokens, in StringBuilder text)
        {
            if (tokens.Length > 2)
            {
                var ifContext = context.IfContexts.Peek();
                if (ifContext.IsIfTrue)
                {
                    ExecuteTokens(ref lineNumber, filePath, tokens.Skip(2).ToArray(), new CompilerContext(), text);
                    ifContext.WasIfTrue = true;
                    ifContext.IsIfTrue = false;
                }
                ifContext.IsDead = true;
            }
        }

        private void ExecuteIncludeStatement(in string filePath, in string[] tokens, in StringBuilder text)
        {
            var path = string.Join(' ', tokens.Skip(1));
            var folder = Path.GetDirectoryName(filePath);
            if (!Path.IsPathRooted(path) && !string.IsNullOrEmpty(folder))
                path = Path.Combine(folder, path);

            var includeText = Precompile(File.ReadAllText(path), path);
            text.AppendLine(includeText);
        }

        private void ExecuteTokens(ref int lineNumber, in string filePath, in string[] tokens, in CompilerContext context, in StringBuilder text) 
        {
            if (tokens.Length > 0 && tokens[0] == "#if")
            {
                if (tokens.Length < 2)
                    throw new ArgumentException($"[Line: {lineNumber}, File: {filePath}] The #if statement must have at least two parts");

                context.IfContexts.Push(new CompilerContext.IfContext());
                ExecuteIfStatement(ref lineNumber, filePath, tokens, context, text);
            }
            else if (tokens.Length > 0 && tokens[0] == "#elseif")
            {
                if(!context.IfContexts.Any())
                    throw new ArgumentException($"[Line: {lineNumber}, File: {filePath}] No previous if statement was found");

                if (tokens.Length < 2)
                    throw new ArgumentException($"[Line: {lineNumber}, File: {filePath}] The #if statement must have at least two parts");

                var ifContext = context.IfContexts.Peek();
                if (ifContext.WasIfTrue)
                    ifContext.IsIfTrue = false;
                else
                    ExecuteIfStatement(ref lineNumber, filePath, tokens, context, text);
            }
            else if (tokens.Length > 0 && tokens[0] == "#else")
            {
                if (!context.IfContexts.Any())
                    throw new ArgumentException($"[Line: {lineNumber}, File: {filePath}] No previous if statement was found");

                var ifContext = context.IfContexts.Peek();
                ifContext.IsIfTrue = ifContext.WasIfTrue ? false : !ifContext.IsIfTrue;
                TryExecuteInlineIf(ref lineNumber, filePath, context, tokens, text);
            }
            else if (tokens.Length > 0 && tokens[0] == "#endif")
            {
                if (!context.IfContexts.Any())
                    throw new ArgumentException($"[Line: {lineNumber}, File: {filePath}] No previous if statement was found");

                context.IfContexts.Pop();
            }
            else if (!context.IfContexts.Any() || context.IfContexts.Peek().IsIfTrue)
            {
                if (tokens.Length > 0 && tokens[0] == "#include")
                {
                    if (tokens.Length < 2)
                        throw new ArgumentException($"[Line: {lineNumber}, File: {filePath}] The #include statement must have at least two parts");

                    ExecuteIncludeStatement(filePath, tokens, text);
                }
                else
                {
                    text.AppendLine(string.Join(' ', tokens));
                }
            }
        }

        public string Precompile(string shaderText, string filePath)
        {
            var text = new StringBuilder();
            var lineNumber = 1;
            var context = new CompilerContext();
            foreach(var line in shaderText.Split(Environment.NewLine))
            {
                var valueMatch = valueRegex.Matches(line);
                var currentIndex = 0;
                var realLineValue = string.Empty;
                foreach (var match in valueMatch.ToList())
                {
                    realLineValue += line.Substring(currentIndex, match.Index);
                    realLineValue += values[match.Value.Substring(2, match.Length - 3)];
                    currentIndex = match.Index + match.Length;
                }
                realLineValue += line.Substring(currentIndex);

                var tokens = realLineValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                ExecuteTokens(ref lineNumber, filePath, tokens, context, text);
                if (context.IfContexts.Any() && context.IfContexts.Peek().IsDead)
                    context.IfContexts.Pop();

                lineNumber++;
            }
            return text.ToString();
        }
    }
}
