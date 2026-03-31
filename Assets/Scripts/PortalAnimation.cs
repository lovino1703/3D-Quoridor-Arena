using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalAnimation : MonoBehaviour
{
    [Header("Cài đặt Xoay")]
    public float rotationSpeed = 90f; // Tốc độ xoay (độ/giây)
    public Vector3 rotationAxis = Vector3.up; // Xoay quanh trục Y (trục dọc)

    [Header("Cài đặt Nhấp nhô")]
    public float floatSpeed = 2f; // Tốc độ nhấp nhô (nhanh/chậm)
    public float floatAmplitude = 0.1f; // Độ cao/thấp của nhịp nhấp nhô

    private Vector3 startPos;

    void Start()
    {
        // Lưu lại vị trí ban đầu để làm gốc cho việc nhấp nhô
        startPos = transform.localPosition;
    }

    void Update()
    {
        // 1. Xử lý Xoay tròn liên tục
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);

        // 2. Xử lý Nhấp nhô lên xuống (dùng hàm Sin trong toán học)
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
    }
}
