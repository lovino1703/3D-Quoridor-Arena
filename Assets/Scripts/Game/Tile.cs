using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public enum TileType { Normal, Ice, Lava, Portal }
    public TileType type = TileType.Normal;
    public Tile linkedPortal;

    public int x;
    public int y;

    [Header("3D Models (Kéo object con vào đây)")]
    public GameObject normalModel;
    public GameObject iceModel;
    public GameObject lavaModel;
    public GameObject portalModel;

    [Header("Highlight")]
    public GameObject highlightIndicator; // Khối xanh lá mờ nổi lên trên

    public void Init(int xPos, int yPos)
    {
        x = xPos;
        y = yPos;
        SetType(TileType.Normal);
        ResetColor(); // Đảm bảo lúc mới sinh ra thì tắt Highlight
    }

    public void SetType(TileType newType)
    {
        type = newType;

        // 1. Tắt hết tất cả các model trước để dọn dẹp
        if (normalModel != null) normalModel.SetActive(false);
        if (iceModel != null) iceModel.SetActive(false);
        if (lavaModel != null) lavaModel.SetActive(false);
        if (portalModel != null) portalModel.SetActive(false);

        // 2. Bật đúng model của loại ô hiện tại
        if (type == TileType.Normal && normalModel != null) normalModel.SetActive(true);
        else if (type == TileType.Ice && iceModel != null) iceModel.SetActive(true);
        else if (type == TileType.Lava && lavaModel != null) lavaModel.SetActive(true);
        else if (type == TileType.Portal && portalModel != null) portalModel.SetActive(true);
    }

    public void Highlight()
    {
        if (highlightIndicator != null) highlightIndicator.SetActive(true);
    }

    public void ResetColor()
    {
        if (highlightIndicator != null) highlightIndicator.SetActive(false);
    }
}