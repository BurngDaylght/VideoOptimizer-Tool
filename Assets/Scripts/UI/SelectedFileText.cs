using System;
using UnityEngine;
using TMPro;
using Zenject;

public class SelectedFileText : MonoBehaviour // TODO delete
{
    [Inject] private FileSelector _fileSelector;
    [SerializeField] private TextMeshProUGUI _text;

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

    public void SetText(string[] fileName)
    {
        _text.text = fileName[0];
    }
}
