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
        /// Apply the rule to executionContext
        /// </summary>
        /// <param name="executionContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IRuleResult> ApplyAsync(RuleExecutionContext executionContext, CancellationToken cancellationToken);
    }
}