using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Thư viện để dùng TextMeshPro
using UnityEngine.SceneManagement; // Thư viện để load lại màn chơi

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("HUD Elements")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI modeText;
    public TextMeshProUGUI p1WallText;
    public TextMeshProUGUI p2WallText;
    public TextMeshProUGUI p1TimerText;
    public TextMeshProUGUI p2TimerText;
    public TextMeshProUGUI p1AbilityText;
    public TextMeshProUGUI p2AbilityText;

    [Header("Game Over Elements")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI winText;

    void Awake()
    {
        instance = this;
    }

    void Update()
    {
        // Liên tục cập nhật thông tin lên màn hình
        if (GameManager.instance == null || PawnManager.instance == null) return;

        // Nếu game kết thúc thì không cập nhật UI của màn chơi nữa
        if (GameManager.instance.currentMode == GameManager.GameMode.GameOver) return;

        // Hiển thị Lượt
        bool isP1 = PawnManager.instance.IsPlayer1Turn;
        turnText.text = isP1 ? "LƯỢT: PLAYER 1" : "LƯỢT: PLAYER 2";
        turnText.color = isP1 ? Color.blue : Color.red;

        // Hiển thị Chế độ (Mode)
        bool isMoveMode = GameManager.instance.currentMode == GameManager.GameMode.Move;
        modeText.text = isMoveMode ? "Chế độ: Di Chuyển (Phím 1)" : "Chế độ: Đặt Tường (Phím 2)";

        // Hiển thị số Tường
        p1WallText.text = "P1 Tường: " + PawnManager.instance.p1WallCount + "/10";
        p2WallText.text = "P2 Tường: " + PawnManager.instance.p2WallCount + "/10";

        // Hiển thị Thời gian
        p1TimerText.text = FormatTime(GameManager.instance.p1Time);
        p2TimerText.text = FormatTime(GameManager.instance.p2Time);

        // --- HIỂN THỊ KỸ NĂNG (CHỈ BẬT TRONG ARENA MODE) ---
        if (GameManager.instance.isArenaMode)
        {
            // Bật hiển thị UI
            p1AbilityText.gameObject.SetActive(true);
            p2AbilityText.gameObject.SetActive(true);

            // Cập nhật chữ
            p1AbilityText.text = "Item: " + PawnManager.instance.GetPlayer1().currentAbility.ToString();
            p2AbilityText.text = "Item: " + PawnManager.instance.GetPlayer2().currentAbility.ToString();
        }
        else
        {
            // Nếu là Classic Mode thì ẩn luôn 2 dòng text này đi
            p1AbilityText.gameObject.SetActive(false);
            p2AbilityText.gameObject.SetActive(false);
        }
    }

    // Hàm gọi khi có người thắng
    public void ShowWinScreen(string winnerName)
    {
        gameOverPanel.SetActive(true); // Bật panel lên
        winText.text = winnerName + " WINS!";
    }

    // Hàm gắn vào nút Restart
    public void RestartGame()
    {
        // Load lại chính màn chơi hiện tại
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Hàm phụ trợ biến số giây thành chữ định dạng MM:SS
    string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}