using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PawnManager : MonoBehaviour
{
    public static PawnManager instance;

    public GameObject player1Prefab;
    public GameObject player2Prefab;

    private Pawn player1;
    private Pawn player2;
    private bool player1Turn = true;

    public int totalTurns = 0;

    public int p1WallCount = 10;
    public int p2WallCount = 10;

    private bool isDashing = false; // Biến đánh dấu đang được đi 2 bước
    public bool IsPlayer1Turn => player1Turn;
    public Pawn GetPlayer1() => player1;
    public Pawn GetPlayer2() => player2;


    void Awake()
    {
        instance = this;
    }

    IEnumerator Start()
    {
        SpawnPawns();

        yield return null; // đợi 1 frame

        HighlightMoves(player1);
    }
    void SpawnPawns()
    {
        GameObject p1 = Instantiate(player1Prefab);
        player1 = p1.GetComponent<Pawn>();
        player1.SetPosition(4, 0);
        player1.transform.rotation = Quaternion.Euler(0, 0, 0); // P1 nhìn thẳng lên (+Z)

        GameObject p2 = Instantiate(player2Prefab);
        player2 = p2.GetComponent<Pawn>();
        player2.SetPosition(4, 8);
        player2.transform.rotation = Quaternion.Euler(0, 180, 0); // P2 nhìn ngược lại mặt P1 (-Z)
    }

    public void MovePawn(int x, int y)
    {
        if (GameManager.instance.currentMode == GameManager.GameMode.GameOver) return;

        Pawn currentPawn = player1Turn ? player1 : player2;
        Pawn opponentPawn = player1Turn ? player2 : player1;

        if (currentPawn.CanMoveTo(x, y))
        {
            // 1. Lưu lại vector hướng đi (dùng cho ô Ice trượt tới)
            int dirX = Mathf.Clamp(x - currentPawn.gridPosition.x, -1, 1);
            int dirY = Mathf.Clamp(y - currentPawn.gridPosition.y, -1, 1);

            // 2. Đi bước đầu tiên
            currentPawn.MoveTo(x, y);
            int finalX = x;
            int finalY = y;

            // 3. XỬ LÝ NGÓI ĐẶC BIỆT (CHỈ TRONG ARENA MODE)
            if (GameManager.instance.isArenaMode)
            {
                Tile landedTile = BoardManager.instance.grid[x, y];

                // --- HIỆU ỨNG PORTAL ---
                if (landedTile.type == Tile.TileType.Portal && landedTile.linkedPortal != null)
                {
                    Debug.Log("Dịch chuyển Portal!");
                    Tile outPortal = landedTile.linkedPortal;

                    // Dùng SetPosition để dịch chuyển tức thời (như teleport thật)
                    currentPawn.SetPosition(outPortal.x, outPortal.y);
                    finalX = outPortal.x;
                    finalY = outPortal.y;
                }
                // --- HIỆU ỨNG ICE ---
                else if (landedTile.type == Tile.TileType.Ice)
                {
                    Debug.Log("Bắt đầu trượt băng!");

                    // Dùng vòng lặp while để trượt liên hoàn chừng nào vẫn còn đứng trên Băng
                    while (BoardManager.instance.grid[finalX, finalY].type == Tile.TileType.Ice)
                    {
                        int nextX = finalX + dirX;
                        int nextY = finalY + dirY;

                        // Kiểm tra xem ô tiếp theo có an toàn để trượt tới không
                        if (nextX >= 0 && nextX < BoardManager.instance.boardSize &&
                            nextY >= 0 && nextY < BoardManager.instance.boardSize &&
                            !currentPawn.IsBlockedBetween(finalX, finalY, nextX, nextY) &&
                            !(opponentPawn.gridPosition.x == nextX && opponentPawn.gridPosition.y == nextY))
                        {
                            currentPawn.MoveTo(nextX, nextY); // Lướt thêm 1 nhịp nữa

                            // Cập nhật lại tọa độ hiện tại để vòng lặp check tiếp
                            finalX = nextX;
                            finalY = nextY;
                        }
                        else
                        {
                            // Nếu bị kẹt bởi mép bàn cờ, bị tường chặn, hoặc vướng đối phương -> Dừng trượt
                            break;
                        }
                    }

                    // --- TÍNH NĂNG CHUỖI HIỆU ỨNG (Tùy chọn) ---
                    // Kiểm tra xem sau khi trượt hết băng, có bị rớt vào Lava không
                    Tile endTile = BoardManager.instance.grid[finalX, finalY];
                    if (endTile.type == Tile.TileType.Lava)
                    {
                        Debug.Log("Trượt băng thẳng vào Lava! Mất lượt.");
                        currentPawn.skipNextTurn = true;
                    }
                    else if (endTile.type == Tile.TileType.Portal && endTile.linkedPortal != null)
                    {
                        Debug.Log("Trượt băng thẳng vào Portal! Dịch chuyển.");
                        Tile outPortal = endTile.linkedPortal;
                        currentPawn.SetPosition(outPortal.x, outPortal.y);
                        finalX = outPortal.x;
                        finalY = outPortal.y;
                    }
                }
                // --- HIỆU ỨNG LAVA ---
                else if (landedTile.type == Tile.TileType.Lava)
                {
                    Debug.Log("Dẫm Lava! Mất lượt tiếp theo.");
                    currentPawn.skipNextTurn = true;
                }
            }
            // --- THÊM PHẦN NHẶT POWER-UP ---
            if (GameManager.instance.isArenaMode)
            {
                PowerUp pickedUp = null;
                // Tìm xem ở ô vừa bước đến có Power-Up nào không
                foreach (var pu in BoardManager.instance.activePowerUps)
                {
                    if (pu.x == finalX && pu.y == finalY)
                    {
                        pickedUp = pu;
                        break;
                    }
                }

                // Nếu có thì nhặt cất vào túi và xóa cục đó đi
                if (pickedUp != null)
                {
                    currentPawn.currentAbility = pickedUp.type;
                    BoardManager.instance.activePowerUps.Remove(pickedUp);
                    Destroy(pickedUp.gameObject);
                    Debug.Log("🎒 " + (player1Turn ? "Player 1" : "Player 2") + " đã nhặt được: " + currentPawn.currentAbility);
                }
            }
            // 4. KIỂM TRA CHIẾN THẮNG THEO TỌA ĐỘ CUỐI CÙNG (finalY)
            if (player1Turn && finalY == 8)
            {
                UIManager.instance.ShowWinScreen("PLAYER 1");
                GameManager.instance.currentMode = GameManager.GameMode.GameOver;
                ResetBoard();
                return;
            }
            if (!player1Turn && finalY == 0)
            {
                UIManager.instance.ShowWinScreen("PLAYER 2");
                GameManager.instance.currentMode = GameManager.GameMode.GameOver;
                ResetBoard();
                return;
            }

            // XỬ LÝ DASH: Nếu đang kích hoạt Dash, cho đi thêm 1 bước, KHÔNG chuyển lượt
            if (isDashing)
            {
                isDashing = false; // Tắt buff để nhịp sau phải EndTurn
                Debug.Log("Dash! Mời đi thêm bước thứ 2!");
                ResetBoard();
                HighlightMoves(currentPawn); // Sáng ô cho đi tiếp
                return; // ⛔ Quan trọng: Dừng hàm tại đây, không cho chạy xuống EndTurn()
            }

            // 5. KẾT THÚC LƯỢT
            EndTurn();
        }
    }

    void HighlightMoves(Pawn pawn)
    {
        int size = BoardManager.instance.boardSize;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (pawn.CanMoveTo(x, y))
                {
                    BoardManager.instance.grid[x, y].Highlight();
                }
            }
        }
    }

    void TryHighlight(int x, int y, Pawn pawn)
    {
        if (BoardManager.instance == null)
            return;

        if (x < 0 || x >= BoardManager.instance.boardSize ||
            y < 0 || y >= BoardManager.instance.boardSize)
            return;

        if (BoardManager.instance.grid[x, y] == null)
            return;

        if (!pawn.CanMoveTo(x, y))
            return;

        BoardManager.instance.grid[x, y].Highlight();
    }

    void ResetBoard()
    {
        int size = BoardManager.instance.boardSize;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (BoardManager.instance.grid[x, y] != null)
                    BoardManager.instance.grid[x, y].ResetColor();
            }
        }
    }

    public void EndTurn()
    {
        player1Turn = !player1Turn; // Chuyển cho người kia
        Pawn currentPawn = player1Turn ? player1 : player2;
        totalTurns++;
        if (GameManager.instance.isArenaMode && totalTurns % 4 == 0)
        {
            BoardManager.instance.SpawnRandomPowerUp();
        }

        // KIỂM TRA MẤT LƯỢT (LAVA)
        if (currentPawn.skipNextTurn)
        {
            Debug.Log((player1Turn ? "Player 1" : "Player 2") + " BỊ BỎ LƯỢT!");
            currentPawn.skipNextTurn = false; // Xóa trạng thái bỏ lượt

            player1Turn = !player1Turn; // Ép chuyển ngược lại cho người vừa đi
            currentPawn = player1Turn ? player1 : player2;
        }

        ResetBoard();
        HighlightMoves(currentPawn);

        // --- GỌI AI NẾU ĐẾN LƯỢT PLAYER 2 ---
        if (GameSettings.isVsAI && !player1Turn)
        {
            // Tạm thời khóa không cho người chơi bấm lung tung khi máy đang nghĩ
            GameManager.instance.currentMode = GameManager.GameMode.GameOver;

            if (AIManager.instance != null)
            {
                AIManager.instance.TriggerAITurn();
            }
        }
        // Nếu chuyển lại về lượt Player 1 thì mở khóa điều khiển
        else if (GameSettings.isVsAI && player1Turn)
        {
            GameManager.instance.currentMode = GameManager.GameMode.Move;
        }
    }

    public void UseAbility()
    {
        Pawn currentPawn = player1Turn ? player1 : player2;

        // Nếu túi rỗng
        if (currentPawn.currentAbility == PowerUp.PowerUpType.None)
        {
            Debug.Log("Túi đồ rỗng! Không có kỹ năng để xài.");
            return;
        }

        // Lấy kỹ năng ra xài và làm rỗng túi
        PowerUp.PowerUpType ability = currentPawn.currentAbility;
        currentPawn.currentAbility = PowerUp.PowerUpType.None;

        if (ability == PowerUp.PowerUpType.ExtraWall)
        {
            if (player1Turn) p1WallCount++; else p2WallCount++;
            Debug.Log("Kích hoạt Extra Wall: Nhận thêm 1 tường!");
            // Xài xong vẫn tiếp tục lượt của mình như bình thường
        }
        else if (ability == PowerUp.PowerUpType.Dash)
        {
            isDashing = true;
            GameManager.instance.currentMode = GameManager.GameMode.Move;
            Debug.Log("Kích hoạt Dash: Bạn được đi 2 bước trong lượt này!");
        }
        else if (ability == PowerUp.PowerUpType.BreakWall)
        {
            GameManager.instance.currentMode = GameManager.GameMode.BreakWall;
            Debug.Log("Kích hoạt Break Wall: Hãy dùng chuột click vào bức tường muốn phá!");
        }
    }

    // Hàm hỗ trợ cho AI ép bật trạng thái Dash
    public void set_isDashing(bool state)
    {
        isDashing = state;
    }
}
