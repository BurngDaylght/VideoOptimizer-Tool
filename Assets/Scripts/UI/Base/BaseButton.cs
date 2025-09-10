using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BaseButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    public event Action OnClick;

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

    [Header("Optional References")]
    [SerializeField] private Image _image;
    [SerializeField] private Button _button;

    private Sequence _sequence;
    private bool _isInteractable = true;
    private Vector3 _defaultScale;

    #region Unity Lifecycle

    private void Awake()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        if (_image == null)
            _image = GetComponent<Image>();

        _defaultScale = transform.localScale;

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

        transform.DOPunchScale(Vector3.one * 0.08f, 0.18f, 10, 1f)
            .OnComplete(() => OnClick?.Invoke());
    }

    public void Show(bool immediate = false)
    {
        _sequence?.Kill();
        gameObject.SetActive(true);

        if (immediate)
        {
            transform.localScale = _defaultScale;
            SetInteractable(true);
            return;
        }

        transform.localScale = Vector3.zero;
        SetInteractable(false);
        _sequence = DOTween.Sequence();
        _sequence.Append(transform.DOScale(_defaultScale * 1.12f, _showDuration * 0.7f).SetEase(_showEase));
        _sequence.Append(transform.DOScale(_defaultScale, _showDuration * 0.4f).SetEase(Ease.OutBack));
        _sequence.OnComplete(() => SetInteractable(true));
    }

    public void Hide(bool immediate = false)
    {
        _sequence?.Kill();

        if (immediate)
        {
            SetInteractable(false);
            transform.localScale = Vector3.zero;
            gameObject.SetActive(false);
            return;
        }

        SetInteractable(false);
        _sequence = DOTween.Sequence();
        _sequence.Append(transform.DOScale(Vector3.zero, _hideDuration).SetEase(Ease.InBack));
        _sequence.OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }

    public void SetInteractable(bool flag)
    {
        _isInteractable = flag;
        if (_button != null)
            _button.interactable = flag;
    }

    #endregion

    #region Pointer Events

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_isInteractable) return;
        transform.DOKill();
        transform.DOScale(_defaultScale * _pressedScale, _pressTweenDuration).SetEase(Ease.OutSine);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isInteractable) return;
        transform.DOKill();
        transform.DOScale(_defaultScale, _pressTweenDuration).SetEase(Ease.OutSine);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isInteractable) return;
        transform.DOKill();
        transform.DOScale(_defaultScale * _hoverScale, _hoverDuration).SetEase(_hoverEase);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isInteractable) return;
        transform.DOKill();
        transform.DOScale(_defaultScale, _hoverDuration).SetEase(_hoverEase);
    }

    #endregion
}
