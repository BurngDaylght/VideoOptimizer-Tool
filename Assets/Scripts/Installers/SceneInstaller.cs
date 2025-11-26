using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public class SceneInstaller : MonoInstaller
{
    [SerializeField] private Canvas _canvas;
    
    public override void InstallBindings()
    {
        Container.Bind<FileSelector>().AsSingle();
        Container.BindInterfacesAndSelfTo<FileProcessor>().AsSingle();
        
        Container.Bind<SelectedFileText>().FromComponentInHierarchy().AsSingle();
        Container.Bind<ProgressBar>().FromComponentInHierarchy().AsSingle();
        
        Container.Bind<WindowScript>().FromComponentInHierarchy().AsSingle();
        
        Container.Bind<NotificationService>().FromComponentInHierarchy().AsSingle();
        
        Container.Bind<Canvas>().FromInstance(_canvas).AsSingle();
    }
}