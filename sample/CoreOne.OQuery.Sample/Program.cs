using CoreOne.OQuery.Evaluators;
using CoreOne.OQuery.Extensions;

namespace CoreOne.OQuery.Sample;

public static class Program
{
    private static void Main()
    {
        var queries = new string[]
        {
            "status = \"open\" AND assignee = \"alice\"",
            "priority >= 3 OR (labels IN (\"urgent\", \"vip\"))",
            "user.address.city = \"Berlin\"",
            "contains(orders.items.product.name, \"keyboard\")",
            "limit 20 offset 40",
            "page 2 pageSize 50",
            "user.address.city = \"Paris\" AND (priority > 2 OR status = \"open\")",
            "NOT status = \"closed\""
        };

        foreach (var queryStr in queries)
        {
            Console.WriteLine($"Query: {queryStr}");
            try
            {
                var lexer = new Lexer.Lexer(queryStr);
                var tokens = lexer.Tokenize();
                var parser = new Parser.Parser(tokens);
                var ast = parser.Parse();

                Console.WriteLine("AST (JSON):");
                Console.WriteLine(Utility.Serialize(ast, true));

                var reSerialized = ast.Accept(new QuerySerializer());
                Console.WriteLine($"Re-serialized: {reSerialized}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            Console.WriteLine(new string('-', 40));
            Console.WriteLine();
            Console.WriteLine();
        }

        RunIQueryableDemo();
        RunSelectDemo();
    }

    // -----------------------------------------------------------------------
    // IQueryable<T> demo
    // -----------------------------------------------------------------------

    private record Ticket(string Status, string Assignee, int Priority, string Label);

    private static void RunIQueryableDemo()
    {
        Console.WriteLine("=== IQueryable<T> Demo ===");
        Console.WriteLine();

        var tickets = new List<Ticket>
        {
            new("open",    "alice", 5, "urgent"),
            new("open",    "bob",   2, "bug"),
            new("closed",  "alice", 3, "urgent"),
            new("pending", "carol", 4, "feature"),
            new("open",    "alice", 1, "bug"),
        }.AsQueryable();

        var demos = new (string query, string description, int count)[]
        {
            ("status = \"open\" AND assignee = \"alice\"",   "AND — filter by two fields",2),
            ("priority > 2",                                  "Numeric comparison",3),
            ("label IN (\"urgent\", \"bug\")",                "IN — match against a list",4),
            ("NOT status = \"closed\"",                       "NOT — logical negation",4),
            ("contains(assignee, \"li\")",                    "contains() built-in function",3),
            ("status = \"open\" LIMIT 2 OFFSET 1",           "Filter + LIMIT/OFFSET pagination",2),
            ("PAGE 1 PAGESIZE 3",                             "PAGE/PAGESIZE pagination only",3),
        };

        foreach (var (query, description, count) in demos)
        {
            Console.WriteLine($"Query    : {query}");
            Console.WriteLine($"Purpose  : ({count}) {description}");
            try
            {
                var lexer = new Lexer.Lexer(query);
                var tokens = lexer.Tokenize();
                var parser = new Parser.Parser(tokens);
                var ast = parser.Parse();

                var results = tickets.Apply(ast).ToList();
                Console.WriteLine($"Results  : ({results.Count})\r\n\t{string.Join("\r\n\t", results)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error    : {ex.Message}");
            }
            Console.WriteLine(new string('-', 60));
            Console.WriteLine();
        }
    }

    // -----------------------------------------------------------------------
    // SELECT projection demo
    // -----------------------------------------------------------------------

    private static void RunSelectDemo()
    {
        Console.WriteLine("=== SELECT Projection Demo ===");
        Console.WriteLine();

        var tickets = new List<Ticket>
        {
            new("open",    "alice", 5, "urgent"),
            new("open",    "bob",   2, "bug"),
            new("closed",  "alice", 3, "urgent"),
            new("pending", "carol", 4, "feature"),
            new("open",    "alice", 1, "bug"),
        }.AsQueryable();

        var demos = new (string query, string description)[]
        {
            ("SELECT status, assignee",                              "No filter — project two fields"),
            ("status = \"open\" SELECT assignee, priority",          "Filter + SELECT"),
            ("label IN (\"urgent\", \"bug\") SELECT label, status",  "IN filter + SELECT"),
            ("status = \"open\" SELECT assignee LIMIT 2 OFFSET 0",   "Filter + SELECT + pagination"),
            ("priority > 2",                                         "No SELECT — all fields projected"),
        };

        foreach (var (query, description) in demos)
        {
            Console.WriteLine($"Query    : {query}");
            Console.WriteLine($"Purpose  : {description}");
            try
            {
                var lexer = new Lexer.Lexer(query);
                var tokens = lexer.Tokenize();
                var parser = new Parser.Parser(tokens);
                var ast = parser.Parse();

                var reSerialized = ast.Accept(new QuerySerializer());
                Console.WriteLine($"AST      : {reSerialized}");

                var results = tickets.Project(ast).ToList();
                Console.WriteLine($"Results  : ({results.Count})");
                foreach (var row in results)
                    Console.WriteLine($"\t{{ {string.Join(", ", row.Select(kv => $"{kv.Key}: {kv.Value ?? "null"}"))} }}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error    : {ex.Message}");
            }
            Console.WriteLine(new string('-', 60));
            Console.WriteLine();
        }
    }
}