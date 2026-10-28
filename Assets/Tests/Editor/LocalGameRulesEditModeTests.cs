using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class LocalGameRulesEditModeTests
{
    static Vector2Int[] Vertical(int x, int y, int length)
    {
        var cells = new Vector2Int[length];
        for (int i = 0; i < length; i++) cells[i] = new Vector2Int(x, y + i);
        return cells;
    }

    static Vector2Int[][] ValidFleet()
    {
        return new[]
        {
            Vertical(0, 0, 4),
            Vertical(2, 0, 3),
            Vertical(4, 0, 3),
            Vertical(6, 0, 2),
            Vertical(8, 0, 2),
            Vertical(0, 5, 2),
            new[] { new Vector2Int(2, 5) },
            new[] { new Vector2Int(4, 5) },
            new[] { new Vector2Int(6, 5) },
            new[] { new Vector2Int(8, 5) }
        };
    }

    static LocalBoard BuildBoard(Vector2Int[][] fleet)
    {
        var board = new LocalBoard();
        Assert.That(board.TryPlaceFleet(fleet, out string error), Is.True, error);
        return board;
    }

    static Vector2Int FindCell(LocalBoard board, bool occupied)
    {
        for (int x = 0; x < LocalBoard.Size; x++)
        for (int y = 0; y < LocalBoard.Size; y++)
        {
            var cell = new Vector2Int(x, y);
            if ((board.ShipAt(cell) != null) == occupied) return cell;
        }
        Assert.Fail("No matching test cell was found.");
        return Vector2Int.zero;
    }

    static Vector2Int FindUnshotEmptyCell(LocalBoard board)
    {
        for (int x = 0; x < LocalBoard.Size; x++)
        for (int y = 0; y < LocalBoard.Size; y++)
        {
            var cell = new Vector2Int(x, y);
            if (board.ShipAt(cell) == null && !board.WasShot(cell)) return cell;
        }
        Assert.Fail("No unshot empty cell remains.");
        return Vector2Int.zero;
    }

    [Test]
    public void GeneratedFleet_UsesClassicLengthsAndValidSpacing()
    {
        var fleet = LocalBoard.GenerateRandomFleet(new System.Random(42));
        var board = BuildBoard(fleet);

        var lengths = new List<int>();
        foreach (LocalShip ship in board.Ships) lengths.Add(ship.Length);
        lengths.Sort();

        CollectionAssert.AreEqual(new[] { 1, 1, 1, 1, 2, 2, 2, 3, 3, 4 }, lengths);
    }

    [Test]
    public void FleetMask_AcceptsClassicFleetAndRejectsDiagonalContact()
    {
        bool[,] mask = new bool[LocalBoard.Size, LocalBoard.Size];
        foreach (Vector2Int[] ship in ValidFleet())
        foreach (Vector2Int cell in ship) mask[cell.x, cell.y] = true;

        Assert.That(LocalBoard.TryBuildFleetFromMask(mask, out _, out string validError), Is.True, validError);

        mask[8, 5] = false;
        mask[1, 4] = true; // diagonal to the four-cell ship ending at (0,3)
        Assert.That(LocalBoard.TryBuildFleetFromMask(mask, out _, out string invalidError), Is.False);
        StringAssert.Contains("соприкасаться", invalidError);
    }

    [Test]
    public void Shoot_ReportsMissHitSunkAndRepeatedShot()
    {
        LocalBoard board = BuildBoard(ValidFleet());
        LocalShip single = null;
        foreach (LocalShip ship in board.Ships)
            if (ship.Length == 1) { single = ship; break; }

        Assert.That(single, Is.Not.Null);
        Vector2Int singleCell = Vector2Int.zero;
        foreach (Vector2Int cell in single.Cells) singleCell = cell;

        Assert.That(board.Shoot(singleCell), Is.EqualTo(LocalShotResult.Sunk));
        Assert.That(board.Shoot(singleCell), Is.EqualTo(LocalShotResult.AlreadyShot));
        Assert.That(board.Shoot(FindCell(board, false)), Is.EqualTo(LocalShotResult.Miss));
    }

    [Test]
    public void Match_HitKeepsTurnAndMissPassesTurn()
    {
        LocalBoard player = BuildBoard(ValidFleet());
        LocalBoard bot = BuildBoard(ValidFleet());
        var match = new LocalGameManager(player, bot, new BattleshipBot(new System.Random(12)));

        Assert.That(match.PlayerShoot(FindCell(bot, true)), Is.EqualTo(LocalShotResult.Hit));
        Assert.That(match.PlayerTurn, Is.True);

        Assert.That(match.PlayerShoot(FindCell(bot, false)), Is.EqualTo(LocalShotResult.Miss));
        Assert.That(match.PlayerTurn, Is.False);
        Assert.That(match.PlayerShoot(FindCell(bot, true)), Is.EqualTo(LocalShotResult.Invalid));

        LocalShotResult botResult = match.BotShoot();
        Assert.That(botResult, Is.Not.EqualTo(LocalShotResult.Invalid));
        Assert.That(botResult, Is.Not.EqualTo(LocalShotResult.AlreadyShot));
    }

    [Test]
    public void CompleteMatch_FinishesWithoutRepeatedShots()
    {
        var player = BuildBoard(LocalBoard.GenerateRandomFleet(new System.Random(23)));
        var botBoard = BuildBoard(LocalBoard.GenerateRandomFleet(new System.Random(24)));
        var match = new LocalGameManager(player, botBoard, new BattleshipBot(new System.Random(25)));

        int actions = 0;
        while (!match.Finished && actions < 250)
        {
            if (match.PlayerTurn)
            {
                Vector2Int target = FindUnshotEmptyCell(botBoard);
                LocalShotResult result = match.PlayerShoot(target);
                Assert.That(result, Is.Not.EqualTo(LocalShotResult.Invalid));
                Assert.That(result, Is.Not.EqualTo(LocalShotResult.AlreadyShot));
            }
            else
            {
                LocalShotResult result = match.BotShoot();
                Assert.That(result, Is.Not.EqualTo(LocalShotResult.Invalid));
                Assert.That(result, Is.Not.EqualTo(LocalShotResult.AlreadyShot));
            }
            actions++;
        }

        Assert.That(match.Finished, Is.True, "The game should resolve before exhausting the board.");
        Assert.That(match.PlayerWon, Is.EqualTo(botBoard.AllShipsSunk));
    }
}
