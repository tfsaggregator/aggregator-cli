using System.Threading;
using System.Threading.Tasks;


namespace aggregator.Engine {
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

        /// <summary>
        /// Apply the rule to executionContext
        /// </summary>
        /// <param name="executionContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IRuleResult> ApplyAsync(RuleExecutionContext executionContext, CancellationToken cancellationToken);
    }
}