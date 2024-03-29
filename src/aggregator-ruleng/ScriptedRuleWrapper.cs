﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
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
        private static readonly ConcurrentDictionary<string, Script<string>> _scriptCache = new();

        private Script<string> _roslynScript;
        private readonly IAggregatorLogger _logger;
        private readonly bool? _ruleFileParseSuccess;

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public bool ImpersonateExecution { get; set; }

        internal IPreprocessedRule RuleDirectives { get; private set; }

        public IRuleSettings Settings { get; private set; }
        public bool BypassRules { get; set; }

        private ScriptedRuleWrapper(string ruleName, IAggregatorLogger logger)
        {
            _logger = logger ?? new NullLogger();
            Name = ruleName;
        }

        /// <summary>
        /// Ctor for Unit Testing
        /// </summary>
        /// <param name="ruleName"></param>
        /// <param name="ruleCode"></param>
        internal ScriptedRuleWrapper(string ruleName, string[] ruleCode, IAggregatorLogger logger = null) : this(ruleName, logger)
        {
            (IPreprocessedRule preprocessedRule, bool parseSuccess) = RuleFileParser.Read(ruleCode);
            _ruleFileParseSuccess = parseSuccess;

            Initialize(preprocessedRule);
        }

        /// <summary>
        /// Standard constructor
        /// </summary>
        /// <param name="ruleName"></param>
        /// <param name="preprocessedRule"></param>
        public ScriptedRuleWrapper(string ruleName, IPreprocessedRule preprocessedRule) : this(ruleName, preprocessedRule, new NullLogger())
        {
        }

        /// <summary>
        /// Ctor for User testing
        /// </summary>
        /// <param name="ruleName"></param>
        /// <param name="preprocessedRule"></param>
        /// <param name="logger"></param>
        public ScriptedRuleWrapper(string ruleName, IPreprocessedRule preprocessedRule, IAggregatorLogger logger) : this(ruleName, logger)
        {
            Initialize(preprocessedRule);
        }

        private void Initialize(IPreprocessedRule preprocessedRule)
        {

            RuleDirectives = preprocessedRule;
            ImpersonateExecution = RuleDirectives.Impersonate;
            BypassRules = RuleDirectives.BypassRules;

            Settings = preprocessedRule.Settings;

            if (RuleDirectives.IsCSharp())
            {
                string ruleKey = string.Join('\n', RuleFileParser.Write(RuleDirectives));
#if DEBUG
                bool cached = _scriptCache.ContainsKey(ruleKey);                
                _logger.WriteVerbose(cached ? $"Rule {Name} found in cache": $"Rule {Name} was not in cache: compiling");
#endif
                _roslynScript = _scriptCache.GetOrAdd(ruleKey, CreateRoslynScript, RuleDirectives);
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

        private static Script<string> CreateRoslynScript(string key, IPreprocessedRule rule)
        {
            var references = new HashSet<Assembly>(DefaultAssemblyReferences().Concat(rule.LoadAssemblyReferences()));
            var imports = new HashSet<string>(DefaultImports().Concat(rule.Imports));

            var scriptOptions = ScriptOptions.Default
                .WithEmitDebugInformation(true)
                .WithReferences(references)
                // Add namespaces
                .WithImports(imports);

            var script = CSharpScript.Create<string>(
                code: rule.GetRuleCode(),
                options: scriptOptions,
                globalsType: typeof(RuleExecutionContext));
            script.Compile();

            return script;
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

