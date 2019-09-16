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
}