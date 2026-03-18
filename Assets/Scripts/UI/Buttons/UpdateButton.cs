using UnityEngine;
using Zenject;

public class UpdateButton : BaseButton
{
    [SerializeField] private string _link;
    private UpdateChecker _updateChecker;

    [Inject]
    private void Construct(UpdateChecker updateChecker)
    {
        _updateChecker = updateChecker;
    }

    private void OnEnable()
    {
        OnClick += OpenDownloadPage;
        _updateChecker.OnUpdateAvailable += ShowInternal;
    }

    private void OnDisable()
    {
        OnClick -= OpenDownloadPage;
        _updateChecker.OnUpdateAvailable -= ShowInternal;
    }


    private void Start()
    {
        Hide(true);

        if (_updateChecker.HasUpdate)
            Show();
    }

    private void ShowInternal()
    {
        Show();
    }
    
    public void OpenDownloadPage()
    {
        Application.OpenURL(_link);
    }
}
