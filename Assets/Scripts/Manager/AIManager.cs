using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    public static AIManager instance;

    [Header("AI Settings")]
    public float delayTime = 1.0f; // Thời gian máy giả vờ "suy nghĩ" cho giống người

    void Awake()
    {
        instance = this;
    }

    // Hàm này sẽ được PawnManager gọi khi đến lượt của Player 2 (nếu bật chế độ Vs AI)
    public void TriggerAITurn()
    {
        // Chạy Coroutine để đợi 1 giây rồi mới đi (tránh việc máy đi quá nhanh chớp nhoáng)
        StartCoroutine(ThinkAndPlay());
    }

    IEnumerator ThinkAndPlay()
    {
        yield return new WaitForSeconds(delayTime);

        Debug.Log("🤖 AI đang suy nghĩ...");
        Pawn aiPawn = PawnManager.instance.GetPlayer2();

        // 1. KIỂM TRA TÚI ĐỒ VÀ XÀI KỸ NĂNG (NẾU CÓ)
        bool skillUsedTurn = TryUseAIPowerUp(aiPawn);

        // Nếu skill đã chiếm quyền điều khiển (như đang Dash hoặc vừa Phá Tường xong)
        // thì DỪNG CÁC HÀNH ĐỘNG BÊN DƯỚI LẠI
        if (skillUsedTurn) yield break;

        // 2. NẾU KHÔNG CÓ KỸ NĂNG, ĐI NHƯ BÌNH THƯỜNG
        PlayHardAI();
    }

    void PlayEasyAI()
    {
        Pawn aiPawn = PawnManager.instance.GetPlayer2();

        List<Vector2Int> possibleMoves = new List<Vector2Int>();

        // Quét toàn bộ 81 ô trên bàn cờ để tìm nước đi (Vừa an toàn tuyệt đối, vừa hỗ trợ luôn tính năng Nhảy qua đầu)
        int size = BoardManager.instance.boardSize;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (aiPawn.CanMoveTo(x, y))
                {
                    possibleMoves.Add(new Vector2Int(x, y));
                }
            }
        }

        // Chọn ngẫu nhiên 1 ô trong danh sách các ô hợp lệ để đi
        if (possibleMoves.Count > 0)
        {
            int randomIndex = Random.Range(0, possibleMoves.Count);
            Vector2Int chosenMove = possibleMoves[randomIndex];

            Debug.Log("🤖 AI quyết định đi tới ô: " + chosenMove);
            // THÊM DÒNG NÀY: Mở khóa bàn cờ trước khi AI bước đi
            GameManager.instance.currentMode = GameManager.GameMode.Move;
            // Ra lệnh di chuyển
            PawnManager.instance.MovePawn(chosenMove.x, chosenMove.y);
        }
        else
        {
            Debug.Log("🤖 AI bị kẹt, không có đường đi!");
            GameManager.instance.currentMode = GameManager.GameMode.Move;
            // Đáng lẽ ra phải đặt tường, nhưng Easy AI tạm thời bỏ qua lượt
            PawnManager.instance.EndTurn();
        }
    }

    // ==========================================
    //              MEDIUM AI LOGIC
    // ==========================================

    void PlayMediumAI()
    {
        Pawn aiPawn = PawnManager.instance.GetPlayer2();

        // Yêu cầu bộ não BFS tìm đường ngắn nhất về hàng y = 0
        Vector2Int nextStep = FindNextStepBFS(aiPawn.gridPosition, 0);

        // Nếu tìm thấy đường đi hợp lệ
        if (nextStep.x != -1)
        {
            Debug.Log("🧠 Medium AI tìm thấy đường! Bước tiếp theo: " + nextStep);
            GameManager.instance.currentMode = GameManager.GameMode.Move;
            PawnManager.instance.MovePawn(nextStep.x, nextStep.y);
        }
        else
        {
            // Nếu bị kẹt không thấy đường (hoặc lỗi), quay về đi bừa như Easy AI
            Debug.Log("🧠 Medium AI không tìm thấy đường ngắn nhất, chuyển sang đi bừa.");
            PlayEasyAI();
        }
    }

    Vector2Int FindNextStepBFS(Vector2Int start, int targetY)
    {
        Pawn aiPawn = PawnManager.instance.GetPlayer2();
        Vector2Int bestMove = new Vector2Int(-1, -1);
        int shortestDistance = 999;

        // Quét 4 hướng xung quanh AI (Những ô kề sát mà AI thực sự có thể bước lên)
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int dir in dirs)
        {
            Vector2Int clickPos = start + dir;

            // 1. Kiểm tra bước đi có hợp lệ không (nằm trong bàn cờ, không bị tường chắn)
            if (clickPos.x < 0 || clickPos.x >= 9 || clickPos.y < 0 || clickPos.y >= 9) continue;
            if (Pathfinding.IsBlocked(start, clickPos)) continue;

            // 2. MÔ PHỎNG ĐIỂM RƠI: Nếu AI quyết định bước vào ô này, kết quả cuối cùng nó bị ném tới đâu?
            Vector2Int finalPos = clickPos;
            bool isArena = GameManager.instance != null && GameManager.instance.isArenaMode;
            Pawn opponent = PawnManager.instance.GetPlayer1();

            // A. Mô phỏng luật nhảy qua đầu (Nếu có đối phương đứng chắn)
            if (finalPos == opponent.gridPosition)
            {
                Vector2Int jumpPos = finalPos + dir; // Thử nhảy thẳng qua đầu
                if (jumpPos.x >= 0 && jumpPos.x < 9 && jumpPos.y >= 0 && jumpPos.y < 9 && !Pathfinding.IsBlocked(finalPos, jumpPos))
                {
                    finalPos = jumpPos;
                    clickPos = jumpPos; // 🔥 THÊM DÒNG NÀY: Dạy AI click vào ô sau lưng đối phương thay vì click vào đầu họ!
                }
                else continue; // Không nhảy được (vướng tường/văng khỏi map) thì hướng này bỏ đi
            }

            // B. Mô phỏng trượt Băng
            if (isArena && BoardManager.instance.grid[finalPos.x, finalPos.y].type == Tile.TileType.Ice)
            {
                Vector2Int slideDir = new Vector2Int(Mathf.Clamp(dir.x, -1, 1), Mathf.Clamp(dir.y, -1, 1));
                while (true)
                {
                    Vector2Int nextSlide = finalPos + slideDir;
                    // Nếu đụng mép, đụng tường, đụng đối phương -> Dừng trượt
                    if (nextSlide.x < 0 || nextSlide.x >= 9 || nextSlide.y < 0 || nextSlide.y >= 9) break;
                    if (Pathfinding.IsBlocked(finalPos, nextSlide)) break;
                    if (nextSlide == opponent.gridPosition) break;

                    finalPos = nextSlide;
                    if (BoardManager.instance.grid[finalPos.x, finalPos.y].type != Tile.TileType.Ice) break;
                }
            }

            // C. Mô phỏng chui Portal
            if (isArena)
            {
                Tile landedTile = BoardManager.instance.grid[finalPos.x, finalPos.y];
                if (landedTile.type == Tile.TileType.Portal && landedTile.linkedPortal != null)
                {
                    Vector2Int exitPos = new Vector2Int(landedTile.linkedPortal.x, landedTile.linkedPortal.y);
                    // Chỉ chui nếu đầu ra không bị đối phương đứng đè lên
                    if (exitPos != opponent.gridPosition)
                    {
                        finalPos = exitPos;
                    }
                }
            }

            // 3. ĐÁNH GIÁ NƯỚC ĐI: Tính khoảng cách từ điểm rơi cuối cùng tới đích
            int penalty = 0;
            if (isArena && BoardManager.instance.grid[finalPos.x, finalPos.y].type == Tile.TileType.Lava)
            {
                penalty = 1; // Dẫm Lava mất lượt -> Cộng thêm phí phạt để AI né đi
            }

            // Dùng thuật toán đã tối ưu để đo quãng đường từ điểm rơi về đích
            int dist = GetShortestPathLength(finalPos, targetY) + penalty;

            // Nếu nước đi này giúp về đích nhanh hơn các nước đi đã thử, ghi nhớ lại ô cần bước vào
            if (dist < shortestDistance)
            {
                shortestDistance = dist;
                bestMove = clickPos;
            }

            // Nếu bước này giúp tới đích ngay lập tức, lấy luôn không cần nghĩ
            if (finalPos.y == targetY)
            {
                return clickPos;
            }
        }

        return bestMove;
    }

    // Hàm trả về số bước ngắn nhất để tới đích (Dùng BFS)
    int GetShortestPathLength(Vector2Int start, int targetY)
    {
        int size = BoardManager.instance.boardSize;
        bool[,] visited = new bool[size, size];

        // Đã sửa lại Queue để lưu thêm thông tin chi phí đi lại (cost)
        Queue<(Vector2Int pos, int cost)> queue = new Queue<(Vector2Int, int)>();

        queue.Enqueue((start, 0));
        visited[start.x, start.y] = true;

        Pawn aiPawn = PawnManager.instance.GetPlayer2();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.pos.y == targetY) return current.cost;

            // GỌI HÀM PATHFINDING MỚI ĐỂ LẤY CÁC NƯỚC ĐI HỢP LỆ (Bao gồm cả trượt băng, chui portal)
            List<Vector2Int> nextMoves = Pathfinding.GetArenaNeighbors(current.pos, aiPawn);

            foreach (Vector2Int next in nextMoves)
            {
                if (!visited[next.x, next.y])
                {
                    visited[next.x, next.y] = true;

                    int moveCost = 1;

                    // NẾU LÀ ARENA: ĐÁNH GIÁ ĐỘ NGUY HIỂM CỦA LAVA
                    if (GameManager.instance.isArenaMode)
                    {
                        if (BoardManager.instance.grid[next.x, next.y].type == Tile.TileType.Lava)
                        {
                            // Đi vào Lava bị phạt mất lượt = tốn thêm 1 turn -> Cost = 2
                            moveCost = 2;
                        }
                    }

                    queue.Enqueue((next, current.cost + moveCost));
                }
            }
        }
        return 999; // Không có đường đi
    }

    bool TryPlaceSmartWall(Pawn playerPawn)
    {
        // Nếu hết tường thì thôi không đặt nữa
        if (PawnManager.instance.p2WallCount <= 0) return false;

        int px = playerPawn.gridPosition.x;
        int py = playerPawn.gridPosition.y;

        // Vị trí lý tưởng: Đặt tường ngang ngay phía trên đầu người chơi (vì Player 1 đang đi lên y=8)
        int wallX = px;
        int wallY = py;

        // Chống lỗi văng khỏi bàn cờ: Nếu người chơi đi sát mép phải, dịch tường sang trái 1 ô
        if (wallX >= BoardManager.instance.boardSize - 1)
        {
            wallX = BoardManager.instance.boardSize - 2;
        }

        // Kiểm tra xem tọa độ tường có nằm an toàn trong bàn cờ không
        if (wallY >= 0 && wallY < BoardManager.instance.boardSize - 1)
        {
            // Kiểm tra xem chỗ này đã có tường ngang nào xây sẵn chưa
            if (!WallManager.instance.horizontalWalls[wallX, wallY] && !WallManager.instance.horizontalWalls[wallX + 1, wallY])
            {
                // 1. LÀM PHÉP ĐẶT TƯỜNG ẢO
                WallManager.instance.horizontalWalls[wallX, wallY] = true;
                WallManager.instance.horizontalWalls[wallX + 1, wallY] = true;

                // 2. KIỂM TRA LUẬT QUORIDOR: Xem tường này có bịt kín đường của ai không?
                int checkP1 = GetShortestPathLength(playerPawn.gridPosition, 8);
                int checkP2 = GetShortestPathLength(PawnManager.instance.GetPlayer2().gridPosition, 0);

                // Nếu số bước < 999 tức là vẫn còn khe hở để lách qua
                if (checkP1 < 999 && checkP2 < 999)
                {
                    // 3. HỢP LỆ! Gọi mô hình 3D thật ra
                    Debug.Log("🧱 AI vả tường chặn mặt bạn!");

                    // Đã trừ đi 0.5 ở trục X để bù lại độ lệch của Prefab khi xoay 90 độ
                    Vector3 wallPos = new Vector3(wallX - 4f, 0, wallY - 4f + 0.5f);

                    // NÂNG CẤP CHỖ NÀY: Ép bức tường xoay 90 độ theo trục Y để nó nằm ngang
                    Quaternion horizontalRotation = Quaternion.Euler(0, 90, 0);
                    GameObject newWall = Instantiate(WallManager.instance.wallPrefab, wallPos, horizontalRotation);

                    // Gắn thẻ tên cho bức tường (để dùng cho kỹ năng Phá Tường BreakWall)
                    WallData data = newWall.AddComponent<WallData>();
                    data.isHorizontal = true;
                    data.gx = wallX;
                    data.gy = wallY;

                    // Trừ số lượng tường của AI và kết thúc lượt
                    PawnManager.instance.p2WallCount--;
                    GameManager.instance.currentMode = GameManager.GameMode.Move;
                    PawnManager.instance.EndTurn();

                    return true; // Báo cáo đã đặt thành công
                }
                else
                {
                    // 4. BỊT KÍN RỒI: Phải rút lại màng ảo thuật ngay
                    WallManager.instance.horizontalWalls[wallX, wallY] = false;
                    WallManager.instance.horizontalWalls[wallX + 1, wallY] = false;
                }
            }
        }

        // Trả về false nếu không tìm được chỗ đặt (bị vướng tường cũ hoặc bịt kín đường)
        return false;
    }

    void PlayHardAI()
    {
        Pawn aiPawn = PawnManager.instance.GetPlayer2();
        Pawn playerPawn = PawnManager.instance.GetPlayer1();

        int aiDist = GetShortestPathLength(aiPawn.gridPosition, 0);
        int playerDist = GetShortestPathLength(playerPawn.gridPosition, 8);

        Debug.Log($"📊 Phân tích: AI còn {aiDist} bước, Bạn còn {playerDist} bước.");

        // NẾU AI THẤY NGUY HIỂM: Bạn chỉ còn cách đích <= 4 bước VÀ AI đang bị tụt lại phía sau hoặc ngang bằng
        if (playerDist <= 4 && playerDist <= aiDist && PawnManager.instance.p2WallCount > 0)
        {
            Debug.Log("⚠️ Cảnh báo! AI đang xuất chiêu...");

            // AI sẽ thử đặt tường
            bool placedWall = TryPlaceSmartWall(playerPawn);

            // Nếu đặt thành công, dừng hàm tại đây (vì đặt tường xong là qua lượt)
            if (placedWall)
            {
                return;
            }
            else
            {
                Debug.Log("Nhưng không tìm được chỗ đặt tường hợp lệ, AI đành phải bỏ chạy.");
            }
        }

        // Nếu tình hình vẫn ổn (hoặc hết tường), AI sẽ tiếp tục cắm đầu chạy về đích bằng Medium AI
        PlayMediumAI();
    }

    // Hàm này sẽ chạy đầu tiên mỗi khi đến lượt AI
    bool TryUseAIPowerUp(Pawn aiPawn)
    {
        // Nếu túi rỗng thì thôi, đi bình thường
        if (aiPawn.currentAbility == PowerUp.PowerUpType.None) return false;

        PowerUp.PowerUpType ability = aiPawn.currentAbility;
        aiPawn.currentAbility = PowerUp.PowerUpType.None; // Rút đồ ra xài

        if (ability == PowerUp.PowerUpType.ExtraWall)
        {
            PawnManager.instance.p2WallCount++;
            Debug.Log("🤖 AI xài Extra Wall! Hiện có: " + PawnManager.instance.p2WallCount + " tường.");
            return false; // Trả về false để AI tiếp tục đi nước cờ của nó trong lượt này
        }
        else if (ability == PowerUp.PowerUpType.Dash)
        {
            Debug.Log("🤖 AI kích hoạt Dash! Tăng tốc độ...");
            StartCoroutine(AIDashRoutine());
            return true; // Trả về true để báo GameManager là AI đang múa, đừng gọi hàm đi bình thường nữa
        }
        else if (ability == PowerUp.PowerUpType.BreakWall)
        {
            Debug.Log("🤖 AI kích hoạt Break Wall! Đang quét mục tiêu...");
            bool didBreak = AIBreakWall(aiPawn);

            // Nếu phá tường thành công thì nghỉ, qua lượt (giống luật người chơi)
            // Nếu không có tường để phá (didBreak = false), AI coi như phí skill và đi bộ bình thường
            return didBreak;
        }

        return false;
    }

    // --- COROUTINE CHO KỸ NĂNG DASH ---
    IEnumerator AIDashRoutine()
    {
        Pawn aiPawn = PawnManager.instance.GetPlayer2();

        // Bật cờ Dash trong PawnManager để nó không EndTurn sau bước 1
        PawnManager.instance.SendMessage("set_isDashing", true, SendMessageOptions.DontRequireReceiver);

        // --- BƯỚC 1 ---
        Vector2Int step1 = FindNextStepBFS(aiPawn.gridPosition, 0);

        if (step1.x != -1)
        {
            // 🔥 SỬA Ở ĐÂY: Mở khóa bàn cờ trước khi gọi lệnh đi
            GameManager.instance.currentMode = GameManager.GameMode.Move;
            PawnManager.instance.MovePawn(step1.x, step1.y);
        }
        else
        {
            // Nếu xui xẻo bị kẹt cứng không có đường đi, đành kết thúc lượt
            GameManager.instance.currentMode = GameManager.GameMode.Move;
            PawnManager.instance.EndTurn();
            yield break; // Dừng Coroutine luôn
        }

        // Đợi 0.5s cho hoạt ảnh nhịp 1 hoàn thành
        yield return new WaitForSeconds(0.5f);

        // --- BƯỚC 2 ---
        Vector2Int step2 = FindNextStepBFS(aiPawn.gridPosition, 0);

        if (step2.x != -1)
        {
            // 🔥 SỬA Ở ĐÂY: Mở khóa bàn cờ lần nữa cho nhịp 2
            GameManager.instance.currentMode = GameManager.GameMode.Move;
            PawnManager.instance.MovePawn(step2.x, step2.y);
        }
        else
        {
            // Nếu đi nhịp 1 xong mà bị kẹt, thì dừng và kết thúc lượt
            GameManager.instance.currentMode = GameManager.GameMode.Move;
            PawnManager.instance.EndTurn();
        }
    }

    // --- LOGIC PHÁ TƯỜNG THÔNG MINH ---
    bool AIBreakWall(Pawn aiPawn)
    {
        // Tìm tất cả các bức tường đang có trên sân
        WallData[] allWalls = FindObjectsOfType<WallData>();
        if (allWalls.Length == 0) return false; // Không có tường nào để đập

        WallData targetWall = null;
        int currentDist = GetShortestPathLength(aiPawn.gridPosition, 0);
        int bestDist = currentDist;

        // AI bắt đầu "giả vờ" đập từng bức tường xem đập cái nào thì đường về đích ngắn nhất
        foreach (WallData wall in allWalls)
        {
            // Tạm thời xóa tường khỏi lưới logic
            if (wall.isHorizontal)
            {
                WallManager.instance.horizontalWalls[wall.gx, wall.gy] = false;
                WallManager.instance.horizontalWalls[wall.gx + 1, wall.gy] = false;
            }
            else
            {
                WallManager.instance.verticalWalls[wall.gx, wall.gy] = false;
                WallManager.instance.verticalWalls[wall.gx, wall.gy + 1] = false;
            }

            // Đo lại khoảng cách
            int newDist = GetShortestPathLength(aiPawn.gridPosition, 0);

            // Phục hồi lại bức tường
            if (wall.isHorizontal)
            {
                WallManager.instance.horizontalWalls[wall.gx, wall.gy] = true;
                WallManager.instance.horizontalWalls[wall.gx + 1, wall.gy] = true;
            }
            else
            {
                WallManager.instance.verticalWalls[wall.gx, wall.gy] = true;
                WallManager.instance.verticalWalls[wall.gx, wall.gy + 1] = true;
            }

            // Nếu đập bức tường này giúp quãng đường ngắn đi, nhớ nó lại
            if (newDist < bestDist)
            {
                bestDist = newDist;
                targetWall = wall;
            }
        }

        // KẾT LUẬN: Bắt đầu đập thật
        if (targetWall != null)
        {
            Debug.Log($"💥 AI đã chọn phá bức tường tại ({targetWall.gx}, {targetWall.gy})");

            // Xóa logic
            if (targetWall.isHorizontal)
            {
                WallManager.instance.horizontalWalls[targetWall.gx, targetWall.gy] = false;
                WallManager.instance.horizontalWalls[targetWall.gx + 1, targetWall.gy] = false;
            }
            else
            {
                WallManager.instance.verticalWalls[targetWall.gx, targetWall.gy] = false;
                WallManager.instance.verticalWalls[targetWall.gx, targetWall.gy + 1] = false;
            }

            // Xóa object 3D
            Destroy(targetWall.gameObject);

            // Chuyển lại trạng thái và qua lượt
            GameManager.instance.currentMode = GameManager.GameMode.Move;
            PawnManager.instance.EndTurn();
            return true;
        }

        Debug.Log("🤔 AI thấy đập tường chẳng ích lợi gì nên giữ lại.");
        return false;
    }   
}