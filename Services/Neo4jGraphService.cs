using Neo4j.Driver;

public class Neo4jGraphService : IAsyncDisposable
{
    private readonly IDriver _driver;

    public Neo4jGraphService(string uri, string user, string password)
    {
        _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
    }

    public async Task<(List<NodeDto> Nodes, List<EdgeDto> Edges)>
    GetGraphFromNode(string startLabel, string propertyName, string propertyValue, bool reverse = false)


    {
        var nodes = new Dictionary<string, NodeDto>();
        var edges = new List<EdgeDto>();

        var relationPattern = reverse ? "<-[r]-" : "-[r]->";
        var query = $@"
                MATCH (start:{startLabel})
                WHERE start.{propertyName} = $propertyValue
                OPTIONAL MATCH (start){relationPattern}(n)
                RETURN start, r, n
            ";

        var session = _driver.AsyncSession();
        try
        {
            var cursor = await session.RunAsync(query, new { propertyValue });
            while (await cursor.FetchAsync())
            {
                var record = cursor.Current;

                // Nodo di partenza
                var start = record["start"]?.As<INode>();
                if (start != null && !nodes.ContainsKey(start.Id.ToString()))
                    nodes[start.Id.ToString()] = new NodeDto
                    {
                        Id = start.Id.ToString(),
                        Label = start.Labels.FirstOrDefault() ?? "Nodo",
                        Properties = start.Properties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                    };

                // Nodo destinazione (può essere null se nessuna relazione)
                var n = record["n"]?.As<INode>();
                if (n != null && !nodes.ContainsKey(n.Id.ToString()))
                    nodes[n.Id.ToString()] = new NodeDto
                    {
                        Id = n.Id.ToString(),
                        Label = n.Labels.FirstOrDefault() ?? "Nodo",
                        Properties = n.Properties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                    };

                // Relazione (può essere null)
                var r = record["r"]?.As<IRelationship>();
                if (r != null && start != null && n != null)
                {
                    edges.Add(new EdgeDto
                    {
                        Source = start.Id.ToString(),
                        Target = n.Id.ToString(),
                        Type = r.Type
                    });
                }
            }
        }
        finally
        {
            await session.CloseAsync();
        }
        return (nodes.Values.ToList(), edges);
    }
    public async ValueTask DisposeAsync()
    {
        await _driver.CloseAsync();
        await _driver.DisposeAsync();
    }

    public async Task<List<NodeSearchResult>> SearchNodesByLabelAndPropertyAsync(string label, string property, string query, int maxResults = 10)
    {
        var results = new List<NodeSearchResult>();
        var session = _driver.AsyncSession();
        try
        {
            // Query Cypher con label e property dinamici (NO parametri per questi, vanno in stringa!)
            var cypher = $@"
            MATCH (n:{label})
            WHERE toLower(n.{property}) CONTAINS toLower($query)
            RETURN n.{property} AS testo, n
            ORDER BY n.{property}
            LIMIT $maxResults
        ";
            var cursor = await session.RunAsync(cypher, new { query, maxResults });
            while (await cursor.FetchAsync())
            {
                var record = cursor.Current;
                var node = record["n"].As<INode>();
                results.Add(new NodeSearchResult
                {
                    Text = record["testo"].As<string>(),
                    NodeId = node.Id.ToString(),
                    Properties = node.Properties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    Label = node.Labels.FirstOrDefault() ?? label
                });
            }
        }
        finally
        {
            await session.CloseAsync();
        }
        return results;
    }

    public class NodeSearchResult
    {
        public string Text { get; set; } = "";
        public string NodeId { get; set; } = "";
        public string Label { get; set; } = "";
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    // DTO semplici per JSON
    public class NodeDto
    {
        public string Id { get; set; } = "";
        public string Label { get; set; } = "";
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class EdgeDto
    {
        public string Source { get; set; } = "";
        public string Target { get; set; } = "";
        public string Type { get; set; } = "";
    }
}
