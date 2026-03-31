using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding
{
    public static bool HasPath(Pawn pawn, int goalRow)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(pawn.gridPosition);
        visited.Add(pawn.gridPosition);

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();

            // đạt goal
            if (cur.y == goalRow)
                return true;

            foreach (var next in GetNeighbors(cur))
            {
                if (!visited.Contains(next))
                {
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
        }

        return false;
    }

    static List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        TryAdd(pos, pos + Vector2Int.up, result);
        TryAdd(pos, pos + Vector2Int.down, result);
        TryAdd(pos, pos + Vector2Int.left, result);
        TryAdd(pos, pos + Vector2Int.right, result);

        return result;
    }

    static void TryAdd(Vector2Int from, Vector2Int to, List<Vector2Int> list)
    {
        if (to.x < 0 || to.x >= 9 || to.y < 0 || to.y >= 9)
            return;

        // check wall
        if (IsBlocked(from, to))
            return;

        list.Add(to);
    }

    static bool IsBlocked(Vector2Int from, Vector2Int to)
    {
        if (WallManager.instance == null)
            return false;

        int x = from.x;
        int y = from.y;

        // phải
        if (to.x == x + 1)
            return WallManager.instance.verticalWalls[x, y];

        // trái
        if (to.x == x - 1)
            return WallManager.instance.verticalWalls[to.x, y];

        // lên
        if (to.y == y + 1)
            return WallManager.instance.horizontalWalls[x, y];

        // xuống
        if (to.y == y - 1)
            return WallManager.instance.horizontalWalls[x, to.y];

        return false;
    }
}
