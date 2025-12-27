using System;
using System.Collections.Generic;

public static class BoardMapBuilder
{
    private const int ExpectedAttackers = 6;
    private const int ExpectedDefenders = 3;
    private const int ExpectedFlags = 2;
    private const int ExpectedReturns = 1;
    private const int ExpectedDefenderOnly = 12;

    public static void Build(
        BoardMapConfig cfg,
        BoardModel board,
        out List<(string id, HexCoord coord)> attackers,
        out List<(string id, HexCoord coord)> defenders,
        out List<(string id, HexCoord coord)> flags)
    {
        attackers = new List<(string, HexCoord)>();
        defenders = new List<(string, HexCoord)>();
        flags = new List<(string, HexCoord)>();

        if (cfg == null)
        {
            throw new ArgumentNullException(nameof(cfg));
        }

        if (board == null)
        {
            throw new ArgumentNullException(nameof(board));
        }

        if (string.IsNullOrWhiteSpace(cfg.boardMap))
        {
            throw new InvalidOperationException("BoardMapConfig.boardMap is empty.");
        }

        string[] lines = cfg.boardMap.Replace("\r", "")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        int a = 0;
        int d = 0;
        int f = 0;
        int rCount = 0;
        int yCount = 0;

        HashSet<string> usedCoords = new HashSet<string>();

        for (int row = 0; row < lines.Length; row++)
        {
            string line = lines[row].TrimEnd();

            // Parse tokens including '_' padding. Any amount of whitespace is allowed.
            List<char> tokens = ParseTokens(line);

            for (int col = 0; col < tokens.Count; col++)
            {
                char c = tokens[col];

                // '_' means: no hex exists here, but it still occupies a column for alignment.
                if (c == '_')
                {
                    continue;
                }

                int rr = row;

                // Use the same "col index" the player sees; '_' columns still affect alignment.
                int qq = cfg.oddR
                    ? col - ((row - (row & 1)) / 2)
                    : col - ((row + (row & 1)) / 2);

                HexCoord coord = new HexCoord(qq, rr);

                HexTag tag = c switch
                {
                    'Y' => HexTag.DefenderOnly,
                    'A' => HexTag.AttackerSpawn,
                    'D' => HexTag.DefenderSpawn,
                    'F' => HexTag.Flag,
                    'R' => HexTag.Return,
                    '.' => HexTag.Normal,
                    _ => HexTag.Normal
                };

                board.AddHex(new HexModel(coord, tag));

                if (c == 'A')
                {
                    a++;
                    attackers.Add(($"A{a}", coord));
                }
                else if (c == 'D')
                {
                    d++;
                    defenders.Add(($"D{d}", coord));
                }
                else if (c == 'F')
                {
                    f++;
                    flags.Add(($"F{f}", coord));
                }
                else if (c == 'R')
                {
                    rCount++;
                }
                else if (c == 'Y')
                {
                    yCount++;
                }
            }
        }


        ValidateCounts(a, d, f, rCount, yCount);
        ValidateAttackerConnectivity(board, attackers, flags);
    }

    private static void ValidateCounts(int a, int d, int f, int r, int y)
    {
        if (a != ExpectedAttackers)
        {
            throw new InvalidOperationException($"Board map invalid: expected A={ExpectedAttackers}, got A={a}.");
        }

        if (d != ExpectedDefenders)
        {
            throw new InvalidOperationException($"Board map invalid: expected D={ExpectedDefenders}, got D={d}.");
        }

        if (f != ExpectedFlags)
        {
            throw new InvalidOperationException($"Board map invalid: expected F={ExpectedFlags}, got F={f}.");
        }

        if (r != ExpectedReturns)
        {
            throw new InvalidOperationException($"Board map invalid: expected R={ExpectedReturns}, got R={r}.");
        }

        if (y != ExpectedDefenderOnly)
        {
            throw new InvalidOperationException($"Board map invalid: expected Y={ExpectedDefenderOnly}, got Y={y}.");
        }
    }

    private static void ValidateAttackerConnectivity(
        BoardModel board,
        List<(string id, HexCoord coord)> attackers,
        List<(string id, HexCoord coord)> flags)
    {
        HashSet<string> visited = new HashSet<string>();
        Queue<HexCoord> q = new Queue<HexCoord>();

        for (int i = 0; i < attackers.Count; i++)
        {
            string k = Key(attackers[i].coord);
            if (visited.Add(k))
            {
                q.Enqueue(attackers[i].coord);
            }
        }

        HashSet<string> flagSet = new HashSet<string>();
        for (int i = 0; i < flags.Count; i++)
        {
            flagSet.Add(Key(flags[i].coord));
        }

        while (q.Count > 0)
        {
            HexCoord cur = q.Dequeue();

            if (flagSet.Contains(Key(cur)))
            {
                return;
            }

            List<HexCoord> neighbors = board.GetNeighbors(cur);
            for (int i = 0; i < neighbors.Count; i++)
            {
                HexCoord next = neighbors[i];

                if (!board.TryGetHex(next, out HexModel nextHex))
                {
                    continue;
                }

                if (nextHex.tag == HexTag.DefenderOnly)
                {
                    continue;
                }

                string nk = Key(next);
                if (visited.Add(nk))
                {
                    q.Enqueue(next);
                }
            }
        }

        throw new InvalidOperationException("Board map invalid: attackers cannot reach any flag (F) from their spawn positions.");
    }

    private static string Key(HexCoord c)
    {
        return $"{c.q},{c.r}";
    }

    private static List<char> ParseTokens(string line)
    {
        List<char> tokens = new List<char>();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (char.IsWhiteSpace(c))
            {
                continue;
            }

            // Allowed tokens
            if (c == '_' || c == '.' || c == 'Y' || c == 'A' || c == 'D' || c == 'F' || c == 'R')
            {
                tokens.Add(c);
            }
        }

        return tokens;
    }

}
