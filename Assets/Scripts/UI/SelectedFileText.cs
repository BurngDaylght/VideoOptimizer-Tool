using System.IO;
using UnityEngine;
using TMPro;
using Zenject;

public class SelectedFileText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;

    private Color _startColor;
    
    private FileSelector _fileSelector;
    private FileProcessor _fileProcessor;

    [Inject]
    private void Construct(FileSelector fileSelector, FileProcessor fileProcessor)
    {
        _fileSelector = fileSelector;
        _fileProcessor = fileProcessor;
    }
    
    private void OnValidate()
    {
        if (_text == null)
            _text = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        _fileSelector.OnFilesSelected += SetText;
        _fileSelector.OnFilesSelected += Show;
        
        _fileProcessor.OnOptimizeEnd += Hide;
    }
    private void OnDisable()
    {
        _fileSelector.OnFilesSelected -= SetText;
        _fileSelector.OnFilesSelected -= Show;
        
        _fileProcessor.OnOptimizeEnd += Hide;
    }

    private void Awake()
    {
        if (_text == null)
            _text = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        _startColor = _text.color;
        
        Hide();
    }
    
    public void SetText(string[] fileNames)
    {
        if (fileNames == null || fileNames.Length == 0)
            return;

        string fileNameOnly = Path.GetFileName(fileNames[0]);
        _text.text = fileNameOnly;
    }

    public void Hide()
    {
        _text.color = new Color(1f, 1f, 1f, 0f);
    }

    public void Show(string[] _)
    {
        _text.color = _startColor;
    }
}
