using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class QualitySlider : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private TextMeshProUGUI _valueText;
    
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
    }

    private void OnDisable()
    {
        _fileProcessor.OnOptimizeStart -= DisableSlider;
        _fileProcessor.OnOptimizeEnd -= EnableSlider;
    }
    
    private void Start()
    {
        UpdateText(_slider.value);
        _slider.onValueChanged.AddListener(OnValueChanged);
    }
    
    private void OnValueChanged(float value)
    {
        UpdateText(_slider.value);
        int crf = Mathf.RoundToInt(value); 
        _fileProcessor.SetQuality(crf);
    }
    
    private void UpdateText(float value)
    {
        _valueText.text = Mathf.RoundToInt(value).ToString();
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

