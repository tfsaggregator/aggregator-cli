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
    public enum RuleExecutionOutcome
    {
        Unknown,
        Success,
        Error
    }

    public interface IRuleResult
    {
        /// <summary>
        /// Result Value Message
        /// </summary>
        string Value { get; }

        /// <summary>
        /// Execution Outcome
        /// </summary>
        RuleExecutionOutcome Outcome { get; }
    }


    public class RuleResult : IRuleResult
    {
        /// <inheritdoc />
        public string Value { get; set; }

        /// <inheritdoc />
        public RuleExecutionOutcome Outcome { get; set; }
    }

    public interface IRule
    {
        /// <summary>
        /// RuleName
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The history will show the changes made by person who triggered the event
        /// Assumes PAT or Account Permission is high enough
        /// </summary>
        bool ImpersonateExecution { get; set; }


        Task<IRuleResult> ApplyAsync(Globals executionContext, CancellationToken cancellationToken);
    }

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

        internal IRuleDirectives RuleDirectives { get; set; }

        private ScriptedRuleWrapper(string ruleName, IAggregatorLogger logger)
        {
            _logger = logger;
            Name = ruleName;
        }

        internal ScriptedRuleWrapper(string ruleName, string[] ruleCode) : this(ruleName, new NullLogger())
        {
            var parseResult = RuleFileParser.Read(ruleCode);
            _ruleFileParseSuccess = parseResult.parseSuccess;

            Initialize(parseResult.ruleDirectives);
        }

        public ScriptedRuleWrapper(string ruleName, IRuleDirectives ruleDirectives) : this(ruleName, ruleDirectives, new NullLogger())
        {
        }

        public ScriptedRuleWrapper(string ruleName, IRuleDirectives ruleDirectives, IAggregatorLogger logger) : this(ruleName, logger)
        {
            Initialize(ruleDirectives);
        }

        private void Initialize(IRuleDirectives ruleDirectives)
        {
            RuleDirectives = ruleDirectives;
            ImpersonateExecution = RuleDirectives.Impersonate;

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
                    globalsType: typeof(Globals));
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
        public async Task<IRuleResult> ApplyAsync(Globals executionContext, CancellationToken cancellationToken)
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

        public (bool success, ImmutableArray<Diagnostic> diagnostics) Verify()
        {
            if (_ruleFileParseSuccess.HasValue && !_ruleFileParseSuccess.Value)
            {
                return (false, ImmutableArray.Create<Diagnostic>());
            }

            // if parsing succeeded try to compile the script and look for errors
            ImmutableArray<Diagnostic> diagnostics = _roslynScript.Compile();
            (bool, ImmutableArray<Diagnostic>) result;
            if (diagnostics.Any())
            {
                result = (false, diagnostics);
            }
            else
            {
                result = (true, ImmutableArray.Create<Diagnostic>());
            }

            return result;
        }

    }
}

