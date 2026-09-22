using System;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Relationel_Database.Data;

namespace Relationel_Database.Analysis;

/// <summary>
/// Kører de samme 4 queries to gange: én gang uden indexes (Seq Scan)
/// og én gang med indexes (forventeligt Index Scan). EXPLAIN (ANALYZE, BUFFERS)
/// bliver printet rå til konsollen, så man kan se:
///   - Seq Scan vs. Index Scan
///   - "rows=" (estimeret) og "actual rows=" (faktisk fundet)
///   - "Rows Removed by Filter" -> hvor mange rækker der blev scannet forgæves
///   - "actual time=" -> reel køretid i ms
/// </summary>
public static class IndexBenchmark
{
    // Skift tallene ud, hvis I har genereret data med andre id'er/navne.
    private static readonly (string Label, string Sql)[] Queries =
    {
        ("Find karakter på navn (punktopslag)",
            "SELECT * FROM character WHERE char_name = 'Char_2500'"),

        ("Find alle karakterer på én realm (filter)",
            "SELECT * FROM character WHERE realm_id = 1"),

        ("Find alt inventory for én karakter (hyppigste 'hot path')",
            "SELECT * FROM inventory WHERE character_id = 2500"),

        ("Find konto på email (kontrolgruppe - er sandsynligvis allerede unik-indekseret)",
            "SELECT * FROM account WHERE email = 'user2500@example.com'")
    };

    public static void Run(AppDbContext db)
    {
        db.Database.OpenConnection();
        var conn = (NpgsqlConnection)db.Database.GetDbConnection();

        Console.WriteLine("################  FASE 1: UDEN INDEXES  ################\n");
        DropTestIndexes(conn);
        Analyze(conn);
        foreach (var (label, sql) in Queries)
            Explain(conn, label, sql);

        Console.WriteLine("################  OPRETTER INDEXES  ################\n");
        CreateTestIndexes(conn);
        Analyze(conn);

        Console.WriteLine("################  FASE 2: MED INDEXES  ################\n");
        foreach (var (label, sql) in Queries)
            Explain(conn, label, sql);
    }

    private static void Explain(NpgsqlConnection conn, string label, string sql)
    {
        Console.WriteLine($"--- {label} ---");
        Console.WriteLine($"SQL: {sql}");

        using var cmd = new NpgsqlCommand($"EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT) {sql}", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            Console.WriteLine(reader.GetString(0));

        Console.WriteLine();
    }

    private static void DropTestIndexes(NpgsqlConnection conn)
    {
        Exec(conn, "DROP INDEX IF EXISTS idx_character_char_name;");
        Exec(conn, "DROP INDEX IF EXISTS idx_character_realm_id;");
        Exec(conn, "DROP INDEX IF EXISTS idx_inventory_character_id;");
    }

    private static void CreateTestIndexes(NpgsqlConnection conn)
    {
        Exec(conn, "CREATE INDEX idx_character_char_name ON character(char_name);");
        Exec(conn, "CREATE INDEX idx_character_realm_id ON character(realm_id);");
        Exec(conn, "CREATE INDEX idx_inventory_character_id ON inventory(character_id);");
    }

    private static void Analyze(NpgsqlConnection conn)
    {
        // ANALYZE opdaterer Postgres' statistik, så planlæggeren regner
        // korrekt både før og efter vi laver indexes.
        Exec(conn, "ANALYZE account;");
        Exec(conn, "ANALYZE character;");
        Exec(conn, "ANALYZE inventory;");
    }

    private static void Exec(NpgsqlConnection conn, string sql)
    {
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }
}