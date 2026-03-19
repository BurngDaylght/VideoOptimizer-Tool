using DG.Tweening;
using UnityEngine;
using Zenject;

public class DropZoneHighlight : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeDuration = 0.2f;

    private FileSelector _fileSelector;

    [Inject]
    private void Construct(FileSelector fileSelector)
    {
        _fileSelector = fileSelector;
    }

    private void Start()
    {
        _canvasGroup.alpha = 0f;
        _fileSelector.OnDragEnter += Show;
        _fileSelector.OnDragLeave += Hide;
    }

    private void OnDestroy()
    {
        _fileSelector.OnDragEnter -= Show;
        _fileSelector.OnDragLeave -= Hide;
    }

    private void Show() => _canvasGroup.DOFade(1f, _fadeDuration).SetEase(Ease.OutSine);
    private void Hide() => _canvasGroup.DOFade(0f, _fadeDuration).SetEase(Ease.OutSine);
}