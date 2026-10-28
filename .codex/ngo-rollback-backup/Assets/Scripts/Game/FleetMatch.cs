using System.Collections.Generic;
using UnityEngine;

// Server-only state. Full fleets are never included in outbound messages.
public sealed class FleetMatch
{
    readonly List<ulong> players = new List<ulong>(2);
    readonly Dictionary<ulong, HashSet<int>> fleets = new Dictionary<ulong, HashSet<int>>();
    readonly Dictionary<ulong, HashSet<int>> shots = new Dictionary<ulong, HashSet<int>>();
    readonly HashSet<ulong> pausedPlayers = new HashSet<ulong>();
    public int Revision { get; private set; }
    public int ShotSequence { get; private set; }
    public bool Started { get; private set; }
    public bool Finished { get; private set; }
    public bool Paused => pausedPlayers.Count > 0;
    public ulong Turn { get; private set; }
    public ulong Winner { get; private set; }
    public ulong LastShooter { get; private set; }
    public int LastX { get; private set; }
    public int LastY { get; private set; }
    public bool LastHit { get; private set; }

    public bool AddPlayer(ulong id)
    {
        if (players.Contains(id)) return true;
        if (players.Count == 2 || Started) return false;
        players.Add(id);
        shots[id] = new HashSet<int>();
        Revision++;
        return true;
    }

    public bool Ready(ulong id) => fleets.ContainsKey(id);
    public bool HasPlayer(ulong id) => players.Contains(id);
    public bool OwnPause(ulong id) => pausedPlayers.Contains(id);
    public bool OtherReady(ulong id) => players.Count == 2 && Ready(players[0] == id ? players[1] : players[0]);

    public bool SubmitFleet(ulong id, Vector2Int[] cells, out string error)
    {
        error = "Некорректная расстановка флота.";
        if (!players.Contains(id) || Started || Ready(id) || cells == null || cells.Length != 20) return false;
        var board = new CellState[10, 10];
        var occupied = new HashSet<int>();
        foreach (Vector2Int cell in cells)
        {
            if (cell.x < 0 || cell.x >= 10 || cell.y < 0 || cell.y >= 10 || !occupied.Add(cell.y * 10 + cell.x)) return false;
            board[cell.x, cell.y] = CellState.Ship;
        }
        if (!PlacementValidator.TryValidate(board, out _, out error)) return false;
        fleets.Add(id, occupied);
        if (players.Count == 2 && Ready(players[0]) && Ready(players[1]))
        {
            Started = true;
            Turn = players[0];
        }
        Revision++;
        return true;
    }

    public bool Shoot(ulong sender, int x, int y, int revision, out string error)
    {
        error = "Дождитесь своего хода.";
        if (!Started || Finished || Paused || sender != Turn || revision != Revision) return false;
        error = "Эта клетка уже обстреляна или находится вне поля.";
        if (x < 0 || x >= 10 || y < 0 || y >= 10 || !shots[sender].Add(y * 10 + x)) return false;
        ulong opponent = players[0] == sender ? players[1] : players[0];
        LastShooter = sender;
        LastX = x;
        LastY = y;
        LastHit = fleets[opponent].Contains(y * 10 + x);
        ShotSequence++;
        Finished = fleets[opponent].IsSubsetOf(shots[sender]);
        if (Finished) Winner = sender;
        else Turn = opponent; // Preserve the existing game's alternating-turn rule.
        Revision++;
        error = null;
        return true;
    }

    public bool SetPause(ulong sender, bool paused)
    {
        if (!players.Contains(sender) || Finished) return false;
        bool changed = paused ? pausedPlayers.Add(sender) : pausedPlayers.Remove(sender);
        if (changed) Revision++;
        return changed;
    }
}

public struct FleetSnapshot
{
    public int Revision, ShotSequence, X, Y;
    public bool MyReady, OtherReady, Started, Finished, Paused, OwnPause, Hit;
    public ulong Turn, Winner, Shooter;
}
