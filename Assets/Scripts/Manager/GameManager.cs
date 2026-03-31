using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Time Settings")]
    public float p1Time = 300f; // 5 phút = 300 giây
    public float p2Time = 300f;

    [Header("Game Settings")]
    public bool isArenaMode;

    void Start()
    {
        // Lấy dữ liệu từ màn hình Menu truyền sang
        isArenaMode = GameSettings.isArenaMode;

        if (isArenaMode)
            Debug.Log("ĐANG CHƠI ARENA MODE: Bật tính năng Power-ups và Special Tiles!");
        else
            Debug.Log("ĐANG CHƠI CLASSIC MODE: Giữ nguyên luật gốc.");
    }

    public enum GameMode
    {
        Move,
        PlaceWall,
        GameOver,
        BreakWall // Thêm mode này cho kỹ năng phá tường sau này
    }

    public GameMode currentMode = GameMode.Move;

    void Awake()
    {
        instance = this;
    }

    void Update()
    {
        // Nếu game kết thúc thì không làm gì cả (dừng thời gian và khóa nút)
        if (currentMode == GameMode.GameOver)
            return;

        // Xử lý Input đổi chế độ
        if (Input.GetKeyDown(KeyCode.Alpha1))
            currentMode = GameMode.Move;

        if (Input.GetKeyDown(KeyCode.Alpha2))
            currentMode = GameMode.PlaceWall;

        // BẤM 3 ĐỂ XẢ KỸ NĂNG
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (PawnManager.instance != null)
            {
                PawnManager.instance.UseAbility();
            }
        }

        // Xử lý đếm ngược thời gian
        if (PawnManager.instance != null)
        {
            if (PawnManager.instance.IsPlayer1Turn)
            {
                p1Time -= Time.deltaTime; // Trừ thời gian theo frame
                if (p1Time <= 0)
                {
                    p1Time = 0;
                    TimeOutWin("PLAYER 2"); // P1 hết giờ thì P2 thắng
                }
            }
            else
            {
                p2Time -= Time.deltaTime;
                if (p2Time <= 0)
                {
                    p2Time = 0;
                    TimeOutWin("PLAYER 1"); // P2 hết giờ thì P1 thắng
                }
            }
        }
    }

    void TimeOutWin(string winner)
    {
        currentMode = GameMode.GameOver;

        if (UIManager.instance != null)
        {
            UIManager.instance.ShowWinScreen(winner + " THẮNG\n(Đối phương hết giờ)");
        }
    }
}