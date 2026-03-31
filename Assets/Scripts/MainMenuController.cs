using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Thư viện để chuyển Scene

public class MainMenuController : MonoBehaviour
{
    public void PlayClassicMode()
    {
        GameSettings.isArenaMode = false; // Đánh dấu là chơi Cổ điển
        SceneManager.LoadScene("GameScene"); // Tải màn chơi chính
    }

    public void PlayArenaMode()
    {
        GameSettings.isArenaMode = true; // Đánh dấu là chơi Arena (có Power-up, Tile đặc biệt)
        SceneManager.LoadScene("GameScene"); // Tải màn chơi chính
    }

    public void QuitGame()
    {
        Debug.Log("Thoát game!");
        Application.Quit(); // Lệnh này chỉ hoạt động khi đã Build ra file .exe, trong Editor sẽ không thấy tắt
    }
}