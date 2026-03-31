using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum PowerUpType { None, Dash, ExtraWall, BreakWall }
    public PowerUpType type;
    public int x, y; // Vị trí trên lưới

    void Update()
    {
        // Làm hiệu ứng xoay tròn lơ lửng cho đẹp mắt
        transform.Rotate(0, 50 * Time.deltaTime, 0);
    }

    public void Init(PowerUpType t, int posX, int posY)
    {
        type = t;
        x = posX;
        y = posY;
        // Đặt vật phẩm cao lên một chút (y=1) để không bị chìm vào bàn cờ
        transform.position = new Vector3(x - 4, 1f, y - 4);

        // Đổi màu tạm thời để bạn dễ nhận biết (Sau này bạn có thể thay bằng model 3D)
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            if (type == PowerUpType.Dash) rend.material.color = Color.yellow; // Dash màu Vàng
            else if (type == PowerUpType.ExtraWall) rend.material.color = Color.cyan; // Tường màu Xanh nhạt
            else if (type == PowerUpType.BreakWall) rend.material.color = Color.red; // Phá tường màu Đỏ
        }
    }
}