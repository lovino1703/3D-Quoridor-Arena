using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallManager : MonoBehaviour
{
    public static WallManager instance;

    public GameObject wallPrefab;

    private GameObject previewWall;
    private Renderer previewRenderer;

    private bool isHorizontal = false;

    public bool[,] verticalWalls = new bool[8, 9];
    public bool[,] horizontalWalls = new bool[9, 8];

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        previewWall = Instantiate(wallPrefab);

        var col = previewWall.GetComponentInChildren<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        previewRenderer = previewWall.GetComponentInChildren<Renderer>();
        previewRenderer.material.color = new Color(0, 1, 0, 0.4f);
    }

    void Update()
    {
        // 1. Tắt bóng mờ nếu không ở chế độ đặt tường
        if (GameManager.instance.currentMode != GameManager.GameMode.PlaceWall)
        {
            previewWall.SetActive(false);
        }

        // 2. NẾU ĐANG Ở CHẾ ĐỘ PHÁ TƯỜNG (BREAK WALL)
        if (GameManager.instance.currentMode == GameManager.GameMode.BreakWall)
        {
            if (Input.GetMouseButtonDown(0))
            {
                TryBreakWall();
            }
            return; // Xử lý xong thì thoát, không chạy code đặt tường bên dưới
        }

        // 3. NẾU ĐANG Ở CHẾ ĐỘ ĐẶT TƯỜNG (PLACE WALL)
        if (GameManager.instance.currentMode == GameManager.GameMode.PlaceWall)
        {
            previewWall.SetActive(true);
            HandlePreview();

            if (Input.GetMouseButtonDown(0))
            {
                PlaceWall();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                RotateWall();
            }
        }
    }

    void RotateWall()
    {
        isHorizontal = !isHorizontal;

        if (isHorizontal)
        {
            previewWall.transform.rotation = Quaternion.Euler(0, 90, 0);
        }
        else
        {
            previewWall.transform.rotation = Quaternion.identity;
        }
    }

    void HandlePreview()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 pos = hit.point;

            if (isHorizontal)
            {
                // wall ngang: kéo dài theo X
                float x = Mathf.Floor(pos.x) + 1f;   // 🔥 KEY
                float z = Mathf.Floor(pos.z) + 0.5f;

                previewWall.transform.position = new Vector3(x, 0.5f, z);
            }
            else
            {
                // wall dọc: kéo dài theo Z
                float x = Mathf.Floor(pos.x) + 0.5f;
                float z = Mathf.Floor(pos.z) + 1f;   // 🔥 KEY

                previewWall.transform.position = new Vector3(x, 0.5f, z);
            }
        }
    }

    void PlaceWall()
    {
        bool isP1Turn = PawnManager.instance.IsPlayer1Turn;

        if (isP1Turn && PawnManager.instance.p1WallCount <= 0)
        {
            Debug.Log("Player 1 đã hết tường!");
            return; // Hết tường thì chặn không cho đặt
        }
        else if (!isP1Turn && PawnManager.instance.p2WallCount <= 0)
        {
            Debug.Log("Player 2 đã hết tường!");
            return; // Hết tường thì chặn không cho đặt
        }
        Vector3 pos = previewWall.transform.position;

        int gx = Mathf.FloorToInt(pos.x + 4);
        int gy = Mathf.FloorToInt(pos.z + 4);

        Vector2Int p1 = PawnManager.instance.GetPlayer1().gridPosition;
        Vector2Int p2 = PawnManager.instance.GetPlayer2().gridPosition;

        if (isHorizontal)
        {
            // ===== VALIDATION =====
            if (gx < 0 || gx >= 8 || gy < 0 || gy >= 8) return;
            if (gx + 1 >= 9) return;

            // Check Existing Wall
            if (horizontalWalls[gx, gy] || horizontalWalls[gx + 1, gy]) return;

            // ===== PATHFINDING CHECK (SIMULATION) =====
            horizontalWalls[gx, gy] = true;
            horizontalWalls[gx + 1, gy] = true;

            bool valid = Pathfinding.HasPath(PawnManager.instance.GetPlayer1(), 8) &&
                         Pathfinding.HasPath(PawnManager.instance.GetPlayer2(), 0);

            if (!valid)
            {
                horizontalWalls[gx, gy] = false;
                horizontalWalls[gx + 1, gy] = false;
                return;
            }
        }
        else
        {
            // ===== VALIDATION =====
            if (gx < 0 || gx >= 8 || gy < 0 || gy >= 8) return;
            if (gy + 1 >= 9) return;

            // Check Existing Wall
            if (verticalWalls[gx, gy] || verticalWalls[gx, gy + 1]) return;

            // ===== PATHFINDING CHECK (SIMULATION) =====
            verticalWalls[gx, gy] = true;
            verticalWalls[gx, gy + 1] = true;

            bool valid = Pathfinding.HasPath(PawnManager.instance.GetPlayer1(), 8) &&
                         Pathfinding.HasPath(PawnManager.instance.GetPlayer2(), 0);

            if (!valid)
            {
                verticalWalls[gx, gy] = false;
                verticalWalls[gx, gy + 1] = false;
                return;
            }
        }

        // ===== EXECUTION =====
        GameObject newWall = Instantiate(
            wallPrefab,
            previewWall.transform.position,
            previewWall.transform.rotation
        );

        // THÊM: Tự động gắn thẻ tên (WallData) vào tường mới xây
        WallData data = newWall.AddComponent<WallData>();
        data.isHorizontal = isHorizontal;
        data.gx = gx;
        data.gy = gy;
        // ===== 3. TRỪ TƯỜNG SAU KHI ĐẶT THÀNH CÔNG (THÊM MỚI) =====
        if (isP1Turn)
        {
            PawnManager.instance.p1WallCount--;
            Debug.Log($"Player 1 đặt tường. Số tường còn lại: {PawnManager.instance.p1WallCount}");
        }
        else
        {
            PawnManager.instance.p2WallCount--;
            Debug.Log($"Player 2 đặt tường. Số tường còn lại: {PawnManager.instance.p2WallCount}");
        }

        PawnManager.instance.EndTurn();
    }
    void TryBreakWall()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Bắn tia Raycast từ chuột vào màn hình
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Kiểm tra xem vật thể bị click trúng có chứa WallData không
            // Dùng GetComponentInParent phòng trường hợp tia ray chạm vào mô hình con
            WallData wallToBreak = hit.collider.GetComponentInParent<WallData>();

            if (wallToBreak != null)
            {
                // 1. Xóa dữ liệu chặn đường trên lưới logic
                if (wallToBreak.isHorizontal)
                {
                    horizontalWalls[wallToBreak.gx, wallToBreak.gy] = false;
                    horizontalWalls[wallToBreak.gx + 1, wallToBreak.gy] = false;
                }
                else
                {
                    verticalWalls[wallToBreak.gx, wallToBreak.gy] = false;
                    verticalWalls[wallToBreak.gx, wallToBreak.gy + 1] = false;
                }

                // 2. Xóa mô hình 3D của bức tường khỏi bàn cờ
                Destroy(wallToBreak.gameObject);
                Debug.Log("💥 Đã phá hủy một bức tường!");

                // 3. Chuyển về chế độ đi bình thường và kết thúc lượt
                GameManager.instance.currentMode = GameManager.GameMode.Move;
                PawnManager.instance.EndTurn();
            }
        }
    }   

}