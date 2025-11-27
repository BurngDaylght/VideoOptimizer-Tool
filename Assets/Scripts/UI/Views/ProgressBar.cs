using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

public class ProgressBar : MonoBehaviour
{
    [SerializeField] private Image _fill;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private float _speed = 5f;
    [SerializeField] private Color _startColor = new Color32(0x12, 0x13, 0x12, 0xFF);
    [SerializeField] private Color _endColor = Color.white;
    [SerializeField] private float _changeColorDuration = 0.5f;
    [SerializeField] private float _changeColorThreshold = 0.5f;
    
    private Tween _colorTween;
    private float _currentProgress;
    private float _targetProgress;
    
    private FileProcessor _fileProcessor;

    [Inject]
    private void Construct(FileProcessor fileProcessor)
    {
        _fileProcessor = fileProcessor;
    }

    private void OnEnable()
    {
        _fileProcessor.OnOptimizeEnd += ResetProgress;
        _fileProcessor.OnOptimizeStop += ResetProgress;
    }

    private void OnDisable()
    {
        _fileProcessor.OnOptimizeEnd -= ResetProgress;
        _fileProcessor.OnOptimizeStop -= ResetProgress;
    }

    private void Start()
    {
        _fill.fillAmount = 0;
        
        _text.text = "0%";
        _text.color = _startColor;
    }

    private void Update()
    {
        _currentProgress = Mathf.Lerp(_currentProgress, _targetProgress, Time.deltaTime * _speed);
        _fill.fillAmount = _currentProgress;
        _text.text = $"{Mathf.RoundToInt(_currentProgress * 100)} %";
    }
    
    public void SetProgress(float value)
    {
        _targetProgress = Mathf.Clamp01(value);

        if (value >= _changeColorThreshold) 
            ChangeColor();
    }

    public void ChangeColor()
    {
        _colorTween?.Kill();
        _colorTween = _text.DOColor(_endColor, _changeColorDuration);
    }
    
    private void ResetProgress()
    {
        _currentProgress = 0;
        _targetProgress = 0;
        _fill.fillAmount = 0;
        _text.text = "0%";
        _text.color = _startColor;
    }
}