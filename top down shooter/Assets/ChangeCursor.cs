using UnityEngine;

public class ChangeCursor : MonoBehaviour
{
    [SerializeField]
    Texture2D cursorTexture;
    Vector2 cursorHotspot;

    // initialize mouse with a new texture with the
    // hotspot set to the middle of the texture
    // (don't forget to set the texture in the inspector
    // in the editor)
    void Start()
    {
        cursorHotspot = new Vector2(cursorTexture.width / 2, cursorTexture.height / 2);
        Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
    }
}
