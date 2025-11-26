using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class NotificationBase : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] protected Image _icon;
    [SerializeField] protected TMP_Text _title;
    [SerializeField] protected TMP_Text _description;

    [Header("Animation Settings")]
    [SerializeField] private float _offsetY = 200f;
    [SerializeField] private float _showDuration = 0.6f;
    [SerializeField] private float _visibleDuration = 3f;
    [SerializeField] private float _hideDuration = 0.45f;

    [SerializeField] private Ease _showEase = Ease.OutCubic;
    [SerializeField] private Ease _hideEase = Ease.InCubic;
    
    [Header("Sound")]
    [SerializeField] protected AudioClip _audioClip;
    [SerializeField, Range(0f, 1f)] protected float _soundVolume = 1f;

    protected RectTransform _rectTransform;
    protected CanvasGroup _canvasGroup;
    protected AudioSource _audioSource;

    private Vector2 _initialPos;
    private Vector2 _visiblePos;
    private Vector2 _endPos;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _audioSource = GetComponent<AudioSource>();

        _canvasGroup.alpha = 0f;
        SetupPositionData();
    }

    private void SetupPositionData()
    {
        _initialPos = _rectTransform.anchoredPosition;
        _visiblePos = _initialPos;
        _initialPos.y += _offsetY;
        _visiblePos.y = _initialPos.y - _offsetY;

        _endPos = _initialPos;
    }

    public virtual void Setup(string titleText, string descriptionText, Sprite iconSprite = null)
    {
        _title.text = titleText;
        _description.text = descriptionText;

        if (iconSprite != null)
            _icon.sprite = iconSprite;

        Show();
    }

    private void Show()
    {
        _rectTransform.anchoredPosition = _initialPos;

        Sequence seq = DOTween.Sequence();

        seq.Append(_rectTransform
                .DOAnchorPos(_visiblePos, _showDuration)
                .SetEase(_showEase))
            .Join(_canvasGroup.DOFade(1f, _showDuration * 0.7f))
            .AppendInterval(_visibleDuration)
            .OnComplete(Hide);
    }

    private void Hide()
    {
        Sequence seq = DOTween.Sequence();

        seq.Append(_rectTransform
                .DOAnchorPos(_endPos, _hideDuration)
                .SetEase(_hideEase))
            .Join(_canvasGroup.DOFade(0f, _hideDuration))
            .OnComplete(() => Destroy(gameObject));
    }
    
    public void PlaySound(bool allowSound)
    {
        if (!allowSound) return;
        
        _audioSource.PlayOneShot(_audioClip, _soundVolume);
    }
}
