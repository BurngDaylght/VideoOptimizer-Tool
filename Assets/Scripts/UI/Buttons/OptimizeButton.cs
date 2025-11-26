using UnityEngine;
using Zenject;

public class OptimizeButton : BaseButton
{
    private FileProcessor _fileProcessor;
    private NotificationService _notificationService;
    
    [Inject]
    private void Construct(FileProcessor fileProcessor, NotificationService notificationService)
    {
        _fileProcessor = fileProcessor;
        _notificationService = notificationService;
    }
    private void OnEnable()
    {
        OnClickAnimationComplete += OptimizeFiles;

        _fileProcessor.OnOptimizeStart += DisableButton;
        _fileProcessor.OnOptimizeStart += HideInternal;
        
        _fileProcessor.OnOptimizeEnd += EnableButton;
        _fileProcessor.OnOptimizeEnd += ShowInternal;
        
        _fileProcessor.OnOptimizeStop += EnableButton;
        _fileProcessor.OnOptimizeStop += ShowInternal;
    }

    private void OnDisable()
    {
        OnClickAnimationComplete -= OptimizeFiles;
        
        _fileProcessor.OnOptimizeStart -= DisableButton;
        _fileProcessor.OnOptimizeStart -= HideInternal;
        
        _fileProcessor.OnOptimizeEnd -= EnableButton;
        _fileProcessor.OnOptimizeEnd -= ShowInternal;
        
        _fileProcessor.OnOptimizeStop -= EnableButton;
        _fileProcessor.OnOptimizeStop -= ShowInternal;
    }
    
    private void ShowInternal()
    {
        Show();
    }

    private void HideInternal()
    {
        Hide();
    }

    private void OptimizeFiles()
    {
        if (!_fileProcessor.IsFilesSelected()) 
            _notificationService.ShowNotification(NotificationType.Information, "Please select a video file");
            
        _fileProcessor.OptimizeFiles();
        Debug.Log("[OptimizeButton] Optimize file!");
    }
}
