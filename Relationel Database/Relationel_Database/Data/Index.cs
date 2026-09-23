using Microsoft.EntityFrameworkCore;
using Npgsql;
using Relationel_Database.Data;
using System.Globalization;

namespace Relationel_Database.Analysis;

public static class IndexBenchmark
{
    // ============================================================
    // QUERY DEFINITIONS
    // ============================================================

    private sealed record BenchmarkQuery(
        string Name,
        string Sql,
        string Purpose);

    private sealed record BenchmarkResult(
        string Configuration,
        string QueryName,
        string ScanType,
        double PlanningTimeMs,
        double ExecutionTimeMs,
        long SharedHitBlocks,
        long SharedReadBlocks);


    // ============================================================
    // QUERIES USED BY THE BENCHMARK
    // ============================================================

    private static readonly BenchmarkQuery[] Queries =
    {
        new(
            "Character name equality",
            "SELECT * FROM character WHERE char_name = 'Char_2500'",
            "Equality lookup"),

        new(
            "Characters by realm equality",
            "SELECT * FROM character WHERE realm_id = 1",
            "Equality filter"),

        new(
            "Inventory by character equality",
            "SELECT * FROM inventory WHERE character_id = 2500",
            "Frequent equality lookup"),

        new(
            "Characters by level range",
            "SELECT * FROM character WHERE level BETWEEN 50 AND 60",
            "Range query"),

        new(
            "Account by email control query",
            "SELECT * FROM account WHERE email = 'user2500@example.com'",
            "Control query")
    };


    // ============================================================
    // INDEX NAMES CREATED BY THIS BENCHMARK
    // ============================================================

    private static readonly string[] TestIndexNames =
    {
        // B-tree indexes
        "idx_character_char_name_btree",
        "idx_character_realm_id_btree",
        "idx_inventory_character_id_btree",
        "idx_character_level_btree",

        // Hash indexes
        "idx_character_char_name_hash",
        "idx_character_realm_id_hash",
        "idx_inventory_character_id_hash",
        "idx_character_level_hash"
    };


    // ============================================================
    // MAIN BENCHMARK
    // ============================================================

    public static void Run(AppDbContext db)
    {

        db.Database.OpenConnection();

        var connection =
            (NpgsqlConnection)db.Database.GetDbConnection();

        var adminConnectionString =
            Environment.GetEnvironmentVariable(
                "ADMIN_CONNECTION_STRING")
            ?? throw new InvalidOperationException(
                "ADMIN_CONNECTION_STRING environment variable is not set.");


        using var adminConnection =
            new NpgsqlConnection(adminConnectionString);

        adminConnection.Open();


        var allResults = new List<BenchmarkResult>();


        try
        {
            Console.WriteLine(
                "============================================================");

            Console.WriteLine(
                "INDEX BENCHMARK: NO INDEX vs B-TREE vs HASH");

            Console.WriteLine(
                "============================================================");

            Console.WriteLine();

            Console.WriteLine(
                "Application connection: game_server");

            Console.WriteLine(
                "Index administration connection: postgres");

            Console.WriteLine();


            // ====================================================
            // CONFIGURATION 1
            // NO TEST INDEXES
            // ====================================================

            Console.WriteLine(
                "Preparing baseline configuration...");

            DropTestIndexes(adminConnection);

            Analyze(connection);


            allResults.AddRange(
                RunConfiguration(
                    connection,
                    "No test indexes",
                    "Baseline without the additional benchmark indexes."
                ));


            // ====================================================
            // CONFIGURATION 2
            // B-TREE INDEXES
            // ====================================================

            DropTestIndexes(adminConnection);

            Console.WriteLine(
                "Creating B-tree indexes...");

            CreateBTreeIndexes(adminConnection);

            Analyze(connection);


            allResults.AddRange(
                RunConfiguration(
                    connection,
                    "B-tree indexes",
                    "B-tree is an ordered index structure."
                ));


            // ====================================================
            // CONFIGURATION 3
            // HASH INDEXES
            // ====================================================

            DropTestIndexes(adminConnection);

            Console.WriteLine(
                "Creating Hash indexes...");

            CreateHashIndexes(adminConnection);

            Analyze(connection);


            allResults.AddRange(
                RunConfiguration(
                    connection,
                    "Hash indexes",
                    "Hash indexes are primarily designed for equality lookups."
                ));


            // ====================================================
            // FINAL COMPARISON
            // ====================================================

            PrintComparison(allResults);
        }
        finally
        {
            // ----------------------------------------------------
            // Always remove the test indexes when the benchmark
            // finishes.
            //
            // This MUST use the administrator connection.
            // ----------------------------------------------------

            DropTestIndexes(adminConnection);
        }
    }


    // ============================================================
    // RUN ONE CONFIGURATION
    // ============================================================

    private static List<BenchmarkResult> RunConfiguration(
        NpgsqlConnection connection,
        string configuration,
        string description)
    {
        Console.WriteLine();

        Console.WriteLine(
            "------------------------------------------------------------");

        Console.WriteLine(
            $"CONFIGURATION: {configuration}");

        Console.WriteLine(
            description);

        Console.WriteLine(
            "------------------------------------------------------------");

        Console.WriteLine();


        var results = new List<BenchmarkResult>();


        foreach (var query in Queries)
        {
            var result =
                Explain(
                    connection,
                    configuration,
                    query);


            results.Add(result);


            Console.WriteLine(
                $"Query: {query.Name}");

            Console.WriteLine(
                $"Purpose: {query.Purpose}");

            Console.WriteLine(
                $"Scan type: {result.ScanType}");

            Console.WriteLine(
                $"Planning time: {result.PlanningTimeMs:F3} ms");

            Console.WriteLine(
                $"Execution time: {result.ExecutionTimeMs:F3} ms");

            Console.WriteLine(
                $"Shared hit blocks: {result.SharedHitBlocks}");

            Console.WriteLine(
                $"Shared read blocks: {result.SharedReadBlocks}");

            Console.WriteLine();
        }


        return results;
    }


    // ============================================================
    // EXPLAIN ANALYZE
    // ============================================================

    private static BenchmarkResult Explain(
        NpgsqlConnection connection,
        string configuration,
        BenchmarkQuery query)
    {
        var lines = new List<string>();


        using var command = new NpgsqlCommand(
            $"EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT) {query.Sql}",
            connection);


        using var reader =
            command.ExecuteReader();


        while (reader.Read())
        {
            lines.Add(
                reader.GetString(0));
        }


        var plan =
            string.Join(
                Environment.NewLine,
                lines);


        return new BenchmarkResult(
            configuration,
            query.Name,
            DetectScanType(plan),
            ExtractMilliseconds(
                plan,
                "Planning Time:"),
            ExtractMilliseconds(
                plan,
                "Execution Time:"),
            ExtractBufferValue(
                plan,
                "shared hit="),
            ExtractBufferValue(
                plan,
                "shared read="));
    }


    // ============================================================
    // DETECT SCAN TYPE
    // ============================================================

    private static string DetectScanType(
        string plan)
    {
        if (plan.Contains(
                "Index Only Scan",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Index Only Scan";
        }


        if (plan.Contains(
                "Bitmap Heap Scan",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Bitmap Heap Scan";
        }


        if (plan.Contains(
                "Index Scan",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Index Scan";
        }


        if (plan.Contains(
                "Seq Scan",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Sequential Scan";
        }


        return "Other / unknown";
    }


    // ============================================================
    // EXTRACT PLANNING / EXECUTION TIME
    // ============================================================

    private static double ExtractMilliseconds(
        string plan,
        string label)
    {
        var line =
            plan
                .Split(Environment.NewLine)
                .FirstOrDefault(
                    line =>
                        line.TrimStart()
                            .StartsWith(
                                label,
                                StringComparison.OrdinalIgnoreCase));


        if (line is null)
        {
            return 0;
        }


        var colonIndex =
            line.IndexOf(':');


        if (colonIndex < 0)
        {
            return 0;
        }


        var value =
            line[(colonIndex + 1)..]
                .Trim()
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();


        return double.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var result)
            ? result
            : 0;
    }


    // ============================================================
    // EXTRACT BUFFER VALUE
    // ============================================================

    private static long ExtractBufferValue(
        string plan,
        string label)
    {
        var index =
            plan.IndexOf(
                label,
                StringComparison.OrdinalIgnoreCase);


        if (index < 0)
        {
            return 0;
        }


        index += label.Length;


        var digits =
            new string(
                plan[index..]
                    .TakeWhile(char.IsDigit)
                    .ToArray());


        return long.TryParse(
            digits,
            out var result)
            ? result
            : 0;
    }


    // ============================================================
    // CREATE B-TREE INDEXES
    // ============================================================

    private static void CreateBTreeIndexes(
        NpgsqlConnection adminConnection)
    {

        Execute(
            adminConnection,
            """
            CREATE INDEX idx_character_char_name_btree
            ON character USING BTREE (char_name);
            """);


        Execute(
            adminConnection,
            """
            CREATE INDEX idx_character_realm_id_btree
            ON character USING BTREE (realm_id);
            """);


        Execute(
            adminConnection,
            """
            CREATE INDEX idx_inventory_character_id_btree
            ON inventory USING BTREE (character_id);
            """);


        Execute(
            adminConnection,
            """
            CREATE INDEX idx_character_level_btree
            ON character USING BTREE (level);
            """);
    }


    // ============================================================
    // CREATE HASH INDEXES
    // ============================================================

    private static void CreateHashIndexes(
        NpgsqlConnection adminConnection)
    {

        Execute(
            adminConnection,
            """
            CREATE INDEX idx_character_char_name_hash
            ON character USING HASH (char_name);
            """);


        Execute(
            adminConnection,
            """
            CREATE INDEX idx_character_realm_id_hash
            ON character USING HASH (realm_id);
            """);


        Execute(
            adminConnection,
            """
            CREATE INDEX idx_inventory_character_id_hash
            ON inventory USING HASH (character_id);
            """);

        Execute(
            adminConnection,
            """
            CREATE INDEX idx_character_level_hash
            ON character USING HASH (level);
            """);
    }


    // ============================================================
    // DROP TEST INDEXES
    // ============================================================

    private static void DropTestIndexes(
        NpgsqlConnection adminConnection)
    {

        foreach (var indexName in TestIndexNames)
        {
            Execute(
                adminConnection,
                $"DROP INDEX IF EXISTS {indexName};");
        }
    }


    // ============================================================
    // UPDATE DATABASE STATISTICS
    // ============================================================

    private static void Analyze(
        NpgsqlConnection connection)
    {
        Execute(
            connection,
            "ANALYZE account;");

        Execute(
            connection,
            "ANALYZE character;");

        Execute(
            connection,
            "ANALYZE inventory;");
    }


    // ============================================================
    // EXECUTE SQL
    // ============================================================

    private static void Execute(
        NpgsqlConnection connection,
        string sql)
    {
        using var command =
            new NpgsqlCommand(
                sql,
                connection);


        command.ExecuteNonQuery();
    }


    // ============================================================
    // FINAL COMPARISON
    // ============================================================

    private static void PrintComparison(
        List<BenchmarkResult> results)
    {
        Console.WriteLine();

        Console.WriteLine(
            "============================================================");

        Console.WriteLine(
            "FINAL COMPARISON");

        Console.WriteLine(
            "============================================================");

        Console.WriteLine();


        Console.WriteLine(
            "{0,-34} {1,-20} {2,-19} {3,12} {4,12} {5,10}",
            "Query",
            "Configuration",
            "Scan type",
            "Planning ms",
            "Execution ms",
            "Hit blocks");


        Console.WriteLine(
            new string('-', 115));


        foreach (var result in results)
        {
            Console.WriteLine(
                "{0,-34} {1,-20} {2,-19} {3,12:F3} {4,12:F3} {5,10}",
                Shorten(
                    result.QueryName,
                    34),
                Shorten(
                    result.Configuration,
                    20),
                Shorten(
                    result.ScanType,
                    19),
                result.PlanningTimeMs,
                result.ExecutionTimeMs,
                result.SharedHitBlocks);
        }


        // ========================================================
        // FASTEST CONFIGURATION PER QUERY
        // ========================================================

        Console.WriteLine();

        Console.WriteLine(
            "------------------------------------------------------------");

        Console.WriteLine(
            "FASTEST CONFIGURATION PER QUERY");

        Console.WriteLine(
            "------------------------------------------------------------");


        foreach (var queryGroup in results.GroupBy(
                     result => result.QueryName))
        {
            var fastest =
                queryGroup
                    .OrderBy(
                        result => result.ExecutionTimeMs)
                    .First();


            Console.WriteLine(
                $"{queryGroup.Key}: " +
                $"{fastest.Configuration} " +
                $"({fastest.ExecutionTimeMs:F3} ms, " +
                $"{fastest.ScanType})");
        }


        // ========================================================
        // AVERAGE EXECUTION TIME
        // ========================================================

        Console.WriteLine();

        Console.WriteLine(
            "------------------------------------------------------------");

        Console.WriteLine(
            "AVERAGE EXECUTION TIME BY CONFIGURATION");

        Console.WriteLine(
            "------------------------------------------------------------");


        foreach (var configurationGroup in results.GroupBy(
                     result => result.Configuration))
        {
            var average =
                configurationGroup
                    .Average(
                        result => result.ExecutionTimeMs);


            Console.WriteLine(
                $"{configurationGroup.Key}: " +
                $"{average:F3} ms average execution time");
        }


        // ========================================================
        // AVERAGE BUFFER USAGE
        // ========================================================

        Console.WriteLine();

        Console.WriteLine(
            "------------------------------------------------------------");

        Console.WriteLine(
            "AVERAGE SHARED BUFFER HITS BY CONFIGURATION");

        Console.WriteLine(
            "------------------------------------------------------------");


        foreach (var configurationGroup in results.GroupBy(
                     result => result.Configuration))
        {
            var averageHits =
                configurationGroup
                    .Average(
                        result => result.SharedHitBlocks);


            Console.WriteLine(
                $"{configurationGroup.Key}: " +
                $"{averageHits:F2} average shared hit blocks");
            Console.WriteLine();
        }
    }


    // ============================================================
    // SHORTEN LONG OUTPUT
    // ============================================================

    private static string Shorten(
        string value,
        int maxLength)
    {
        return value.Length <= maxLength
            ? value
            : value[..(maxLength - 3)] + "...";
    }
}