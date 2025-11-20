using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProgressBar : MonoBehaviour
{
    [SerializeField] private Image _fill;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private float _speed = 5f;
    
    private float _currentProgress;
    private float _targetProgress;
    
    private void Start()
    {
        _fill.fillAmount = 0;
        _text.text = "0%";
    }
    
    public void SetProgress(float value)
    {
        _targetProgress = Mathf.Clamp01(value);
    }

    private void Update()
    {
        _currentProgress = Mathf.Lerp(_currentProgress, _targetProgress, Time.deltaTime * _speed);
        _fill.fillAmount = _currentProgress;
        _text.text = $"{Mathf.RoundToInt(_currentProgress * 100)}%";
    }
}