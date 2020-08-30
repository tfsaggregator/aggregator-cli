using System.Threading;
using System.Threading.Tasks;


namespace aggregator.Engine
{
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
        /// <remarks>Setter public for CLI</remarks>
        bool ImpersonateExecution { get; set; }

        ///<summary>
        /// Configuration data picked by the directive parser that may influence a rule behaviour.
        ///</summary>
        IRuleSettings Settings { get; }

        /// <summary>
        /// Apply the rule to executionContext
        /// </summary>
        /// <param name="executionContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IRuleResult> ApplyAsync(RuleExecutionContext executionContext, CancellationToken cancellationToken);
    }
}
