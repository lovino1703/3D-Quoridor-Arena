using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Các nút Bước 1 (Chọn Luật)")]
    public GameObject classicBtn;
    public GameObject arenaBtn;
    public GameObject quitBtn;

    [Header("Các nút Bước 2 (Chọn Đối thủ)")]
    public GameObject pvpBtn;
    public GameObject pveBtn;
    public GameObject backBtn;

    [Header("Tên Scene Ván Cờ")]
    public string gameSceneName = "GameScene";

    void Start()
    {
        // Khi mới chạy game, đảm bảo hiện Bước 1, ẩn Bước 2
        ShowModeSelection();
    }

    // ==========================================
    // BƯỚC 1: BẤM VÀO NÚT CHỌN LUẬT CHƠI
    // ==========================================
    public void SelectClassicMode()
    {
        GameSettings.isArenaMode = false; // Lưu luật
        ShowOpponentSelection();          // Đổi sang màn hình nút Bước 2
    }

    public void SelectArenaMode()
    {
        GameSettings.isArenaMode = true;  // Lưu luật
        ShowOpponentSelection();          // Đổi sang màn hình nút Bước 2
    }

    // ==========================================
    // BƯỚC 2: BẤM VÀO NÚT CHỌN ĐỐI THỦ
    // ==========================================
    public void StartPvP()
    {
        GameSettings.isVsAI = false;
        SceneManager.LoadScene(gameSceneName);
    }

    public void StartPvE()
    {
        GameSettings.isVsAI = true;
        SceneManager.LoadScene(gameSceneName);
    }

    // ==========================================
    // CÁC HÀM ẨN/HIỆN NÚT
    // ==========================================
    public void ShowOpponentSelection()
    {
        // Giấu các nút cũ đi
        classicBtn.SetActive(false);
        arenaBtn.SetActive(false);
        quitBtn.SetActive(false);

        // Hiện các nút mới lên
        pvpBtn.SetActive(true);
        pveBtn.SetActive(true);
        backBtn.SetActive(true);
    }

    public void ShowModeSelection() // Gắn hàm này vào nút Quay Lại
    {
        // Hiện lại các nút cũ
        classicBtn.SetActive(true);
        arenaBtn.SetActive(true);
        quitBtn.SetActive(true);

        // Giấu các nút mới đi
        pvpBtn.SetActive(false);
        pveBtn.SetActive(false);
        backBtn.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Đã thoát game!");
    }
}