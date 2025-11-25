using UnityEngine;

public class NotificationService : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private NotificationBase _baseNotificationPrefab;
    [SerializeField] private CompressionNotification _compressionPrefab;

    [Header("Container")]
    [SerializeField] private Transform _container;

    [Header("Icons")]
    [SerializeField] private Sprite _successIcon;
    [SerializeField] private Sprite _errorIcon;
    [SerializeField] private Sprite _infoIcon;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShowNotification(NotificationType.CompressionSuccess, "100 Gb", "20 Mb");
        }
    }

    public void ShowNotification(NotificationType type, string text1, string text2 = "")
    {
        switch (type)
        {
            case NotificationType.CompressionSuccess:
            {
                var notif = Instantiate(_compressionPrefab, _container);
                notif.Setup("Compression Complete", text1, text2, _successIcon);
                break;
            }

            case NotificationType.CompressionError:
            {
                var notif = Instantiate(_baseNotificationPrefab, _container);
                notif.Setup("Compression Error", text1, _errorIcon);
                break;
            }

            case NotificationType.Information:
            {
                var notif = Instantiate(_baseNotificationPrefab, _container);
                notif.Setup("Information", text1, _infoIcon);
                break;
            }
        }
    }
}