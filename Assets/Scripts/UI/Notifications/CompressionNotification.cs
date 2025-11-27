using TMPro;
using UnityEngine;

public class CompressionNotification : NotificationBase
{
    [SerializeField] private TextMeshProUGUI _newSizeText;

    public void Setup(string titleText, string oldSize, string newSize, Sprite iconSprite = null)
    {
        base.Setup(titleText, oldSize, iconSprite);
        _newSizeText.text = newSize;
        _newSizeText.gameObject.SetActive(true);
    }
}