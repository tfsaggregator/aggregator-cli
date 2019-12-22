using System.Threading.Tasks;

namespace aggregator.Engine
{
    public interface IRuleProvider
    {
        Task<IRule> GetRule(string name);
    }
}