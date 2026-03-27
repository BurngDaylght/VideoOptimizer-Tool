using System;
using DG.Tweening;
using LightSide;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BaseButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    public event Action OnClick;
    public event Action OnClickAnimationComplete;
    
    [Header("Text")]
    [SerializeField] private string _buttonText = "Click";

    [Header("Animation")]
    [SerializeField] private float _showDuration = 0.35f;
    [SerializeField] private float _hideDuration = 0.25f;
    [SerializeField] private Ease _showEase = Ease.OutBack;
    [SerializeField] private float _pressedScale = 0.92f;
    [SerializeField] private float _pressTweenDuration = 0.08f;

    [Header("Hover Animation")]
    [SerializeField] private float _hoverScale = 1.05f;
    [SerializeField] private float _hoverDuration = 0.15f;
    [SerializeField] private Ease _hoverEase = Ease.OutSine;

    [Header("References")]
    [SerializeField] protected UniText _text;
    [SerializeField] private Image _image;
    [SerializeField] private Button _button;

    private bool _isInteractable = true;
    private Sequence _sequence;
    private RectTransform _rectTransform;
    private Vector3 _initialScale;
    private float _initialFontSize;
    private Vector2 _initialSize;

    #region Unity Lifecycle

    private void OnValidate()
    {
        if (_text == null)
            _text = GetComponentInChildren<UniText>();
        
        _text.Text = _buttonText;
        
        if (_button == null)
            _button = GetComponent<Button>();
        
        if (_image == null) 
            _image = GetComponentInChildren<Image>();
    }

    private void Awake()
    {
        _initialScale = transform.localScale;
        _initialFontSize = _text.FontSize;
        _text.AutoSize = false;
        _rectTransform = GetComponent<RectTransform>();
        _initialSize = _rectTransform.sizeDelta;
        
        if (_button == null)
            _button = GetComponent<Button>();

        if (_image == null)
            _image = GetComponentInChildren<Image>();

        if (_button != null)
            _button.onClick.AddListener(InternalClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(InternalClick);

        _sequence?.Kill();
    }

    #endregion

    #region UI Methods

    private void InternalClick()
    {
        if (!_isInteractable) return;

        OnClick?.Invoke();
        
        _rectTransform.DOKill();
        DOTween.Kill(_text);

        _rectTransform.sizeDelta = _initialSize;
        _text.FontSize = _initialFontSize;

        Sequence seq = DOTween.Sequence();
        seq.Append(_rectTransform.DOSizeDelta(_initialSize * 1.08f, 0.08f));
        seq.Append(_rectTransform.DOSizeDelta(_initialSize, 0.1f));
        seq.OnComplete(() => OnClickAnimationComplete?.Invoke());
    }

    public void Show(bool immediate = false)
    {
        _sequence?.Kill();

        if (immediate)
        {
            transform.localScale = _initialScale;
            SetInteractable(true);
            return;
        }

        transform.localScale = Vector3.zero;
        SetInteractable(false);
        _sequence = DOTween.Sequence();
        _sequence.Append(transform.DOScale(_initialScale * 1.12f, _showDuration * 0.7f).SetEase(_showEase));
        _sequence.Append(transform.DOScale(_initialScale, _showDuration * 0.4f).SetEase(Ease.OutBack));
        _sequence.OnComplete(() => SetInteractable(true));
    }

    public void Hide(bool immediate = false)
    {
        _sequence?.Kill();

        if (immediate)
        {
            SetInteractable(false);
            transform.localScale = Vector3.zero;
            return;
        }

        SetInteractable(false);
        _sequence = DOTween.Sequence();
        _sequence.Append(transform.DOScale(Vector3.zero, _hideDuration).SetEase(Ease.InBack));
    }

    public void SetInteractable(bool flag)
    {
        _isInteractable = flag;
        if (_button != null)
            _button.interactable = flag;
    }

    public void DisableButton()
    {
        _isInteractable = false;
        if (_button != null)
            _button.interactable = false;
    }
    
    public void EnableButton()
    {
        _isInteractable = true;
        if (_button != null)
            _button.interactable = true;
    }

    #endregion

    #region Pointer Events

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_isInteractable) return;

        _rectTransform.DOKill();
        DOTween.Kill(_text);
        
        _rectTransform.sizeDelta = _initialSize;
        _text.FontSize = _initialFontSize;

        _rectTransform.DOSizeDelta(_initialSize * _pressedScale, _pressTweenDuration).SetEase(Ease.OutSine);

        DOTween.To(
            () => _text.FontSize,
            x => _text.FontSize = x,
            _initialFontSize * _pressedScale,
            _pressTweenDuration
        ).SetEase(Ease.OutSine);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isInteractable) return;

        _rectTransform.DOKill();
        DOTween.Kill(_text);

        _rectTransform.DOSizeDelta(_initialSize, _pressTweenDuration).SetEase(Ease.OutSine);

        DOTween.To(
            () => _text.FontSize,
            x => _text.FontSize = x,
            _initialFontSize,
            _pressTweenDuration
        ).SetEase(Ease.OutSine);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isInteractable) return;

        _rectTransform.DOKill();
        DOTween.Kill(_text);
        
        _rectTransform.sizeDelta = _initialSize;
        _text.FontSize = _initialFontSize;

        _rectTransform.DOSizeDelta(_initialSize * _hoverScale, _hoverDuration).SetEase(_hoverEase);

        DOTween.To(
            () => _text.FontSize,
            x => _text.FontSize = x,
            _initialFontSize * _hoverScale,
            _hoverDuration
        ).SetEase(_hoverEase);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isInteractable) return;

        _rectTransform.DOKill();
        DOTween.Kill(_text);

        _rectTransform.DOSizeDelta(_initialSize, _hoverDuration).SetEase(_hoverEase);

        DOTween.To(
            () => _text.FontSize,
            x => _text.FontSize = x,
            _initialFontSize,
            _hoverDuration
        ).SetEase(_hoverEase);
    }

    #endregion
}
