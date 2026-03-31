using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding
{
    // Hàm cũ giữ nguyên (Dùng để check đặt tường hợp lệ)
    public static bool HasPath(Pawn pawn, int goalRow)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(pawn.gridPosition);
        visited.Add(pawn.gridPosition);

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();

            if (cur.y == goalRow)
                return true;

            // ĐÃ THAY ĐỔI: Sử dụng hàm GetArenaNeighbors mới
            foreach (var next in GetArenaNeighbors(cur, pawn))
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

    // --- HÀM MỚI: Xử lý toàn bộ logic Arena ---
    // --- HÀM MỚI: Xử lý toàn bộ logic Arena ---
    public static List<Vector2Int> GetArenaNeighbors(Vector2Int pos, Pawn currentPawn = null)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        // 1. Lấy 4 hướng đi cơ bản (có check tường)
        List<Vector2Int> basicMoves = new List<Vector2Int>();
        TryAdd(pos, pos + Vector2Int.up, basicMoves);
        TryAdd(pos, pos + Vector2Int.down, basicMoves);
        TryAdd(pos, pos + Vector2Int.left, basicMoves);
        TryAdd(pos, pos + Vector2Int.right, basicMoves);

        bool isArena = GameManager.instance != null && GameManager.instance.isArenaMode;
        Pawn opponent = null;
        if (currentPawn != null && PawnManager.instance != null)
        {
            opponent = (currentPawn == PawnManager.instance.GetPlayer1()) ? PawnManager.instance.GetPlayer2() : PawnManager.instance.GetPlayer1();
        }

        foreach (Vector2Int move in basicMoves)
        {
            Vector2Int finalPos = move;
            bool isValidMove = true; // Cờ đánh dấu bước đi này có được thêm vào kết quả không

            // TRƯỜNG HỢP 1: NHẢY QUA ĐẦU ĐỐI PHƯƠNG
            if (opponent != null && finalPos == opponent.gridPosition)
            {
                // Tính hướng đang đi
                Vector2Int dir = finalPos - pos;
                Vector2Int jumpPos = finalPos + dir;

                // Nếu ô nhảy tới hợp lệ và không có tường chặn sau lưng đối phương
                if (jumpPos.x >= 0 && jumpPos.x < 9 && jumpPos.y >= 0 && jumpPos.y < 9 && !IsBlocked(finalPos, jumpPos))
                {
                    finalPos = jumpPos;
                }
                else
                {
                    isValidMove = false; // Không nhảy thẳng được thì bỏ qua hướng này
                }
            }

            if (!isValidMove) continue; // Chuyển sang hướng khác nếu hướng này không đi được

            // TRƯỜNG HỢP 2: LUẬT ARENA (BĂNG)
            if (isArena && BoardManager.instance.grid[finalPos.x, finalPos.y].type == Tile.TileType.Ice)
            {
                Vector2Int dir = finalPos - pos;
                // Nếu vừa nhảy qua đầu đối phương, dir có độ dài là 2, cần normalize lại
                dir = new Vector2Int(Mathf.Clamp(dir.x, -1, 1), Mathf.Clamp(dir.y, -1, 1));

                Vector2Int slidePos = finalPos;

                // Trượt liên tục tới khi hết băng hoặc đụng tường/đối phương/mép bàn cờ
                while (true)
                {
                    Vector2Int nextSlide = slidePos + dir;

                    if (nextSlide.x < 0 || nextSlide.x >= 9 || nextSlide.y < 0 || nextSlide.y >= 9) break; // Đụng mép
                    if (IsBlocked(slidePos, nextSlide)) break; // Đụng tường
                    if (opponent != null && nextSlide == opponent.gridPosition) break; // Đụng người

                    slidePos = nextSlide;
                    if (BoardManager.instance.grid[slidePos.x, slidePos.y].type != Tile.TileType.Ice) break; // Hết băng
                }
                finalPos = slidePos;
            }

            // TRƯỜNG HỢP 3: DỊCH CHUYỂN PORTAL (TỨC THỜI)
            if (isArena)
            {
                Tile landedTile = BoardManager.instance.grid[finalPos.x, finalPos.y];
                if (landedTile.type == Tile.TileType.Portal && landedTile.linkedPortal != null)
                {
                    Vector2Int exitPos = new Vector2Int(landedTile.linkedPortal.x, landedTile.linkedPortal.y);
                    // Nếu đầu ra không bị đối phương đứng chặn, lập tức coi điểm đến là đầu ra của Portal
                    if (opponent == null || exitPos != opponent.gridPosition)
                    {
                        finalPos = exitPos;
                    }
                }
            }

            // Cuối cùng, thêm vị trí tính toán được vào danh sách kết quả (nếu chưa có)
            if (!result.Contains(finalPos))
            {
                result.Add(finalPos);
            }
        }

        return result;
    }

    static void TryAdd(Vector2Int from, Vector2Int to, List<Vector2Int> list)
    {
        if (to.x < 0 || to.x >= 9 || to.y < 0 || to.y >= 9) return;
        if (IsBlocked(from, to)) return;
        list.Add(to);
    }

    // Hàm IsBlocked giữ nguyên... (Để tiết kiệm không gian, tôi không chép lại hàm IsBlocked ở đây, bạn giữ nguyên hàm cũ nhé)
    public static bool IsBlocked(Vector2Int from, Vector2Int to)
    {
        if (WallManager.instance == null) return false;
        int x = from.x; int y = from.y;
        if (to.x == x + 1) return WallManager.instance.verticalWalls[x, y];
        if (to.x == x - 1) return WallManager.instance.verticalWalls[to.x, y];
        if (to.y == y + 1) return WallManager.instance.horizontalWalls[x, y];
        if (to.y == y - 1) return WallManager.instance.horizontalWalls[x, to.y];
        return false;
    }
}