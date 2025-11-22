using System.IO;
using UnityEngine;
using TMPro;
using Zenject;

public class SelectedFileText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;
    
    private FileSelector _fileSelector;

    [Inject]
    private void Construct(FileSelector fileSelector)
    {
        _fileSelector = fileSelector;
    }
    
    private void OnValidate()
    {
        if (_text == null)
            _text = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable() => _fileSelector.OnFilesSelected += SetText;
    private void OnDisable() => _fileSelector.OnFilesSelected -= SetText;

    private void Awake()
    {
        if (_text == null)
            _text = GetComponent<TextMeshProUGUI>();
    }


    public void SetText(string[] fileNames)
    {
        if (fileNames == null || fileNames.Length == 0)
            return;

        string fileNameOnly = Path.GetFileName(fileNames[0]);
        _text.text = fileNameOnly;
    }
}
