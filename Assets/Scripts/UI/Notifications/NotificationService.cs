using UnityEngine;

public class NotificationService : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private CompressionNotification _compressionPrefab;
    [SerializeField] private NotificationBase _errorPrefab;
    [SerializeField] private NotificationBase _informationPrefab;

    [Header("Container")]
    [SerializeField] private Transform _container;

    [Header("Icons")]
    [SerializeField] private Sprite _successIcon;
    [SerializeField] private Sprite _errorIcon;
    [SerializeField] private Sprite _infoIcon;
    
    private bool _isSoundEnabled = true;

    public void ShowNotification(NotificationType type, string text1, string text2 = "")
    {
        switch (type)
        {
            case NotificationType.CompressionSuccess:
            {
                var notif = Instantiate(_compressionPrefab, _container);
                notif.Setup("Compression complete", text1, text2, _successIcon);
                notif.PlaySound(_isSoundEnabled); 
                break;
            }

            case NotificationType.CompressionError:
            {
                var notif = Instantiate(_errorPrefab, _container);
                notif.Setup("Compression error", text1, _errorIcon);
                notif.PlaySound(_isSoundEnabled);
                break;
            }

            case NotificationType.Information:
            {
                var notif = Instantiate(_informationPrefab, _container);
                notif.Setup("Information", text1, _infoIcon);
                notif.PlaySound(_isSoundEnabled);
                break;
            }
        }
    }
    
    public void EnableSound(bool enabled) => _isSoundEnabled = enabled;
}