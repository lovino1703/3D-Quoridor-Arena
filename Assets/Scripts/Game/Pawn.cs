using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : MonoBehaviour
{
    public Vector2Int gridPosition;
    public float moveSpeed = 5f;
    public bool skipNextTurn = false;

    // THÊM BIẾN NÀY ĐỂ LƯU TRỮ HIỆU ỨNG ĐANG CHẠY
    private Coroutine currentMoveCoroutine;

    public PowerUp.PowerUpType currentAbility = PowerUp.PowerUpType.None;
    public void SetPosition(int x, int y)
    {
        // 1. Dừng ngay hiệu ứng lướt cũ (nếu có) trước khi dịch chuyển
        if (currentMoveCoroutine != null)
        {
            StopCoroutine(currentMoveCoroutine);
        }

        gridPosition = new Vector2Int(x, y);
        transform.position = new Vector3(x - 4, 0.5f, y - 4);
    }

    public void MoveTo(int x, int y)
    {
        int dx = x - gridPosition.x;
        int dy = y - gridPosition.y;
        Vector3 moveDirection = new Vector3(dx, 0, dy);

        if (moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }

        gridPosition = new Vector2Int(x, y);
        Vector3 targetPosition = new Vector3(x - 4, 0.5f, y - 4);

        // 2. Dừng hiệu ứng lướt cũ trước khi bắt đầu lướt tới điểm mới
        if (currentMoveCoroutine != null)
        {
            StopCoroutine(currentMoveCoroutine);
        }
        currentMoveCoroutine = StartCoroutine(SmoothMove(targetPosition));
    }

    // Hàm Coroutine xử lý việc lướt đi mượt mà
    IEnumerator SmoothMove(Vector3 targetPos)
    {
        // Vòng lặp chạy liên tục cho đến khi Pawn gần như chạm đến đích
        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            // Dịch chuyển dần dần vị trí hiện tại về phía đích
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

            // Tạm dừng Coroutine và đợi đến frame tiếp theo mới chạy tiếp vòng lặp
            yield return null;
        }

        // Khi đã đến rất gần, ép vị trí khớp hoàn toàn với đích để tránh sai số
        transform.position = targetPos;
    }

    public bool CanMoveTo(int x, int y)
    {
        int cx = gridPosition.x;
        int cy = gridPosition.y;

        int dx = x - cx;
        int dy = y - cy;

        Pawn opponent = (this == PawnManager.instance.GetPlayer1())
            ? PawnManager.instance.GetPlayer2()
            : PawnManager.instance.GetPlayer1();

        Vector2Int opp = opponent.gridPosition;

        // ===== KHÔNG ĐƯỢC ĐỨNG CHỒNG =====
        if (x == opp.x && y == opp.y)
            return false;

        // ===== MOVE 1 Ô =====
        if (Mathf.Abs(dx) + Mathf.Abs(dy) == 1)
        {
            // nếu có opponent ở ô đó → KHÔNG đi được
            if (opp.x == x && opp.y == y)
                return false;

            return !IsBlockedBetween(cx, cy, x, y);
        }

        // ===== JUMP (NHẢY THẲNG) =====
        // FIX 1: Thêm điều kiện (dx == 0 || dy == 0) để đảm bảo đây là bước nhảy thẳng, 
        // ngăn chặn việc bắt nhầm bước đi chéo.
        if (Mathf.Abs(dx) + Mathf.Abs(dy) == 2 && (dx == 0 || dy == 0))
        {
            int midX = (cx + x) / 2;
            int midY = (cy + y) / 2;

            // phải có opponent ở giữa
            if (opp.x == midX && opp.y == midY)
            {
                if (!IsBlockedBetween(cx, cy, midX, midY) &&
                    !IsBlockedBetween(midX, midY, x, y))
                    return true;
            }
        }

        // ===== DIAGONAL (NHẢY CHÉO) =====
        if (Mathf.Abs(dx) == 1 && Mathf.Abs(dy) == 1)
        {
            // opponent phải đứng cạnh
            if (Mathf.Abs(opp.x - cx) + Mathf.Abs(opp.y - cy) == 1)
            {
                // FIX 2: Đảm bảo ô đích phải nằm ngang hoặc dọc so với opponent. 
                // Điều này ép buộc pawn phải nhảy "vòng qua" opponent, ngăn chặn việc nhảy chéo lùi về sau.
                if (x == opp.x || y == opp.y)
                {
                    int dirX = opp.x - cx;
                    int dirY = opp.y - cy;

                    int behindX = opp.x + dirX;
                    int behindY = opp.y + dirY;

                    bool blockedBehind = false;

                    // nếu ra ngoài board → coi như bị chặn
                    if (behindX < 0 || behindX >= 9 || behindY < 0 || behindY >= 9)
                    {
                        blockedBehind = true;
                    }
                    else
                    {
                        blockedBehind = IsBlockedBetween(opp.x, opp.y, behindX, behindY);
                    }

                    // chỉ cho diagonal khi bị chặn phía sau
                    if (blockedBehind)
                    {
                        // FIX 3: Check tường bằng đường ziczac vì IsBlockedBetween chỉ chạy đúng trên đường thẳng.
                        // Yêu cầu: Không có tường giữa mình & opponent, VÀ không có tường giữa opponent & đích.
                        if (!IsBlockedBetween(cx, cy, opp.x, opp.y) &&
                            !IsBlockedBetween(opp.x, opp.y, x, y))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    // ===== HÀM QUAN TRỌNG NHẤT =====
    public bool IsBlockedBetween(int x1, int y1, int x2, int y2)
    {
        if (WallManager.instance == null)
            return false;

        // đi ngang
        if (x2 > x1)
            return WallManager.instance.verticalWalls[x1, y1];

        if (x2 < x1)
            return WallManager.instance.verticalWalls[x2, y1];

        // đi dọc
        if (y2 > y1)
            return WallManager.instance.horizontalWalls[x1, y1];

        if (y2 < y1)
            return WallManager.instance.horizontalWalls[x1, y2];

        return false;
    }
}