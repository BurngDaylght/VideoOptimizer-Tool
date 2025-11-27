using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class QualitySlider : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private TextMeshProUGUI _valueText;
    [SerializeField] private Image _circle;
    [SerializeField] private float _punchDuration;
    [SerializeField] private float _punchForce;
    
    private Vector3 _originalTextScale;
    private Vector3 _originalCircleScale;
    private int _lastIntValue;
    
    private FileProcessor _fileProcessor;

    [Inject] 
    private void Construct(FileProcessor fileProcessor)
    {
        _fileProcessor = fileProcessor;
    }
    
    private void OnEnable()
    {
        _fileProcessor.OnOptimizeStart += DisableSlider;
        _fileProcessor.OnOptimizeEnd += EnableSlider;
        _fileProcessor.OnOptimizeStop += EnableSlider;
    }

    private void OnDisable()
    {
        _fileProcessor.OnOptimizeStart -= DisableSlider;
        _fileProcessor.OnOptimizeEnd -= EnableSlider;
        _fileProcessor.OnOptimizeStop -= EnableSlider;
    }
    
    private void Start()
    {
        _lastIntValue = Mathf.RoundToInt(_slider.value);
        
        _originalTextScale = _valueText.transform.localScale;
        _originalCircleScale = _circle.transform.localScale;

        UpdateText(_slider.value);
        _slider.onValueChanged.AddListener(OnValueChanged);
    }
    
    private void OnValueChanged(float value)
    {
        int intValue = Mathf.RoundToInt(value);
    
        if (intValue != _lastIntValue)
        {
            UpdateText(value);
            AnimateUI();
            _fileProcessor.SetQuality(intValue);

            _lastIntValue = intValue;
        }
    }
    
    private void UpdateText(float value)
    {
        _valueText.text = Mathf.RoundToInt(value).ToString();
    }

    private void AnimateUI()
    {
        _valueText.transform.DOKill(true);
        _valueText.transform.DOPunchScale(_originalTextScale * _punchForce, _punchDuration, 10, 1f);

        _circle.transform.DOKill(true);
        _circle.transform.DOPunchScale(_originalCircleScale * _punchForce, _punchDuration, 10, 1f);
    }

    private void DisableSlider()
    {
        _slider.interactable = false;
    }

    private void EnableSlider()
    {
        _slider.interactable = true;
    }
}
