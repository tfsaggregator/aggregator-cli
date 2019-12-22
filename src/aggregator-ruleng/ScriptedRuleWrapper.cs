using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using aggregator.Engine.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace aggregator.Engine
{
    /// <summary>
    /// CSharp Scripted Rule Facade
    /// </summary>
    public class ScriptedRuleWrapper : IRule
    {
        private Script<string> _roslynScript;
        private readonly IAggregatorLogger _logger;
        private readonly bool? _ruleFileParseSuccess;

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public bool ImpersonateExecution { get; set; }

        internal IPreprocessedRule RuleDirectives { get; set; }

        private ScriptedRuleWrapper(string ruleName, IAggregatorLogger logger)
        {
            _logger = logger;
            Name = ruleName;
        }

        internal ScriptedRuleWrapper(string ruleName, string[] ruleCode) : this(ruleName, new NullLogger())
        {
            (IPreprocessedRule preprocessedRule, bool parseSuccess) = RuleFileParser.Read(ruleCode);
            _ruleFileParseSuccess = parseSuccess;

            Initialize(preprocessedRule);
        }

        public ScriptedRuleWrapper(string ruleName, IPreprocessedRule preprocessedRule) : this(ruleName, preprocessedRule, new NullLogger())
        {
        }

        public ScriptedRuleWrapper(string ruleName, IPreprocessedRule preprocessedRule, IAggregatorLogger logger) : this(ruleName, logger)
        {
            Initialize(preprocessedRule);
        }

        private void Initialize(IPreprocessedRule preprocessedRule)
        {
            RuleDirectives = preprocessedRule;

            var references = new HashSet<Assembly>(DefaultAssemblyReferences().Concat(RuleDirectives.LoadAssemblyReferences()));
            var imports = new HashSet<string>(DefaultImports().Concat(RuleDirectives.Imports));

            var scriptOptions = ScriptOptions.Default
                .WithEmitDebugInformation(true)
                .WithReferences(references)
                // Add namespaces
                .WithImports(imports);

            if (RuleDirectives.IsCSharp())
            {
                _roslynScript = CSharpScript.Create<string>(
                    code: RuleDirectives.GetRuleCode(),
                    options: scriptOptions,
                    globalsType: typeof(RuleExecutionContext));
            }
            else
            {
                _logger.WriteError($"Cannot execute rule: language is not supported.");
            }
        }

        private static IEnumerable<Assembly> DefaultAssemblyReferences()
        {
            var types = new List<Type>()
            {
                typeof(object),
                typeof(System.Linq.Enumerable),
                typeof(System.Collections.Generic.CollectionExtensions),
                typeof(Microsoft.VisualStudio.Services.WebApi.IdentityRef),
                typeof(WorkItemWrapper)
            };

            return types.Select(t => t.Assembly);
        }

        private static IEnumerable<string> DefaultImports()
        {
            var imports = new List<string>
            {
                "System",
                "System.Linq",
                "System.Collections.Generic",
                "Microsoft.VisualStudio.Services.WebApi",
                "aggregator.Engine"
            };

            return imports;
        }

        /// <inheritdoc />
        public async Task<IRuleResult> ApplyAsync(RuleExecutionContext executionContext, CancellationToken cancellationToken)
        {
            var result = await _roslynScript.RunAsync(executionContext, cancellationToken);

            if (result.Exception != null)
            {
                _logger.WriteError($"Rule failed with {result.Exception}");
                return new RuleResult()
                {
                    Outcome = RuleExecutionOutcome.Error,
                    Value = result.Exception.ToString()
                };
            }

            _logger.WriteInfo($"Rule succeeded with {result.ReturnValue ?? "no return value"}");
            return new RuleResult()
            {
                Outcome = RuleExecutionOutcome.Success,
                Value = result.ReturnValue // ?? string.Empty
            };
        }

        /// <summary>
        /// Verify the script rule code by trying to compile and return compile errors
        /// if parsing rule code already fails, success will also be false
        /// </summary>
        /// <returns></returns>
        public (bool success, IReadOnlyList<Diagnostic> diagnostics) Verify()
        {
            if (_ruleFileParseSuccess.HasValue && !_ruleFileParseSuccess.Value)
            {
                return (false, ImmutableArray.Create<Diagnostic>());
            }

            // if parsing succeeded try to compile the script and look for errors
            var diagnostics = _roslynScript.Compile();
            var result = diagnostics.Any() ? (false, diagnostics) : (true, ImmutableArray.Create<Diagnostic>());

            return result;
        }

    }
}

