using LightSide;
using UnityEngine;

public class CompressionNotification : NotificationBase
{
    [SerializeField] private UniText _newSizeText;

    public void Setup(string titleText, string oldSize, string newSize, Sprite iconSprite = null)
    {
        base.Setup(titleText, oldSize, iconSprite);
        _newSizeText.Text = newSize;
        _newSizeText.gameObject.SetActive(true);
    }
}