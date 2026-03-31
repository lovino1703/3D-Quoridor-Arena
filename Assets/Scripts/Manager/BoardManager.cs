using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager instance;

    public GameObject tilePrefab;
    public int boardSize = 9;

    public Tile[,] grid;

    public GameObject powerUpPrefab;
    public List<PowerUp> activePowerUps = new List<PowerUp>(); // Danh sách các item đang có trên sân

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        GenerateBoard();

        // Đợi 0.1 giây để đảm bảo GameManager đã đọc xong biến từ MainMenu
        Invoke("CheckAndGenerateArenaTiles", 0.1f);
    }

    void GenerateBoard()
    {
        grid = new Tile[boardSize, boardSize];

        for (int x = 0; x < boardSize; x++)
        {
            for (int y = 0; y < boardSize; y++)
            {
                Vector3 pos = new Vector3(x - 4, 0, y - 4);
                GameObject tileObj = Instantiate(tilePrefab, pos, Quaternion.identity);
                Tile tile = tileObj.GetComponent<Tile>();
                tile.Init(x, y);
                grid[x, y] = tile;
            }
        }
    }

    void CheckAndGenerateArenaTiles()
    {
        // Chỉ sinh ngói đặc biệt nếu đang ở Arena Mode
        if (GameManager.instance != null && GameManager.instance.isArenaMode)
        {
            GenerateSpecialTiles();
        }
    }

    void GenerateSpecialTiles()
    {
        // 1. Tạo 1 Cặp Portal (2 ô nối với nhau)
        Tile p1 = GetRandomSafeTile();
        Tile p2 = GetRandomSafeTile();
        while (p1 == p2) p2 = GetRandomSafeTile(); // Đảm bảo không trùng nhau

        p1.SetType(Tile.TileType.Portal);
        p2.SetType(Tile.TileType.Portal);
        p1.linkedPortal = p2;
        p2.linkedPortal = p1;

        // 2. Tạo ĐÚNG 3 ô Ice
        int iceCount = 0;
        while (iceCount < 3)
        {
            Tile t = GetRandomSafeTile();
            if (t.type == Tile.TileType.Normal)
            {
                t.SetType(Tile.TileType.Ice);
                iceCount++; // Chỉ đếm khi đã đổi màu thành công
            }
        }

        // 3. Tạo ĐÚNG 3 ô Lava
        int lavaCount = 0;
        while (lavaCount < 3)
        {
            Tile t = GetRandomSafeTile();
            if (t.type == Tile.TileType.Normal)
            {
                t.SetType(Tile.TileType.Lava);
                lavaCount++; // Chỉ đếm khi đã đổi màu thành công
            }
        }
    }

    // Hàm phụ trợ để lấy ngẫu nhiên 1 ô KHÔNG nằm quá gần vạch xuất phát/đích
    Tile GetRandomSafeTile()
    {
        int rx = Random.Range(0, boardSize);
        // Tránh hàng 0, 1 và 7, 8 theo đúng thiết kế để không cho win/thua quá nhanh
        int ry = Random.Range(2, boardSize - 2);
        return grid[rx, ry];
    }

    public void SpawnRandomPowerUp()
    {
        if (powerUpPrefab == null) return;

        int attempts = 0;
        while (attempts < 50) // Thử tìm ngẫu nhiên tối đa 50 lần để tránh vòng lặp vô tận
        {
            int rx = Random.Range(0, boardSize);
            int ry = Random.Range(0, boardSize);

            // 1. Chỉ rớt trên ô Normal (không rớt đè lên Băng, Dung Nham hay Portal)
            if (grid[rx, ry].type != Tile.TileType.Normal) { attempts++; continue; }

            // 2. Không rớt trúng đầu người chơi
            Vector2Int p1 = PawnManager.instance.GetPlayer1().gridPosition;
            Vector2Int p2 = PawnManager.instance.GetPlayer2().gridPosition;
            if ((rx == p1.x && ry == p1.y) || (rx == p2.x && ry == p2.y)) { attempts++; continue; }

            // 3. Không rớt đè lên PowerUp khác đã có sẵn
            bool hasPU = false;
            foreach (var pu in activePowerUps) { if (pu.x == rx && pu.y == ry) hasPU = true; }
            if (hasPU) { attempts++; continue; }

            // Đã tìm được vị trí hợp lệ -> Sinh ra!
            GameObject obj = Instantiate(powerUpPrefab);
            PowerUp powerUpScript = obj.GetComponent<PowerUp>();

            // Random ngẫu nhiên 1 trong 3 loại (1=Dash, 2=ExtraWall, 3=BreakWall)
            PowerUp.PowerUpType randomType = (PowerUp.PowerUpType)Random.Range(1, 4);
            powerUpScript.Init(randomType, rx, ry);

            activePowerUps.Add(powerUpScript);
            Debug.Log("🎲 Đã rơi Power-Up: " + randomType + " tại ô (" + rx + "," + ry + ")");
            break;
        }
    }
}