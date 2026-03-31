using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileClick : MonoBehaviour
{
    void OnMouseDown()
    {
        if (GameManager.instance.currentMode != GameManager.GameMode.Move)
            return;

        Tile tile = GetComponent<Tile>();

        if (tile == null)
            return;

        if (PawnManager.instance == null)
            return;

        PawnManager.instance.MovePawn(tile.x, tile.y);
    }
}
