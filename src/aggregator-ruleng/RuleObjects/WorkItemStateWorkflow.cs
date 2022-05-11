using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using QuikGraph;
using QuikGraph.Algorithms;

namespace aggregator.Engine;

internal class WorkItemStateWorkflow
{
    private readonly string workItemTypeName;
    public string Name { get; private set; }
    public string ReferenceName { get; private set; }
    private IList<string> States { get; } = new List<string>();
    private AdjacencyGraph<string, Edge<string>> Graph { get; set; }

    public WorkItemStateWorkflow(string workItemTypeName)
    {
        this.workItemTypeName = workItemTypeName;
    }

    public async Task<bool> LoadAsync(EngineContext context)
    {
        var workItemType = await context.Clients.WitClient.GetWorkItemTypeAsync(context.ProjectName, workItemTypeName);
        Name = workItemType.Name;
        ReferenceName = workItemType.ReferenceName;
        States.Clear();
        States.AddRange(workItemType.States.Select(s => s.Name));
        // BUILD GRAPH
        var edges = new List<Edge<string>>();
        workItemType.Transitions.ForEach(t =>
        {
            t.Value.ForEach(st =>
            {
                edges.Add(new Edge<string>(t.Key, st.To));
            });
        });
        Graph = edges.ToAdjacencyGraph<string, Edge<string>>();
        Graph.AddVertexRange(States);
        return true;
    }

    public bool HasState(string stateName)
    {
        return States.Contains(stateName);
    }

    internal IEnumerable<string> GetTransitionPath(string currentState, string targetState)
    {
        // Constant cost
        Func<Edge<string>, double> edgeCost = edge => 1;
        TryFunc<string, IEnumerable<Edge<string>>> tryGetPaths = Graph.ShortestPathsDijkstra(edgeCost, currentState);
        if (tryGetPaths(targetState, out var path))
        {
            foreach (Edge<string> edge in path)
            {
                yield return edge.Target;
            }
        }
    }
}
