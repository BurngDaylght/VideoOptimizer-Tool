using UnityEngine;
using Zenject;

public class SceneInstaller : MonoInstaller
{
    [Header("UI Root")]
    [SerializeField] private Canvas _canvas;

    public override void InstallBindings()
    {
        BindServices();
        BindUI();
        BindExternal();
    }

    private void BindServices()
    {
        Container.Bind<FileSelector>().AsSingle();
        Container.BindInterfacesAndSelfTo<FileProcessor>().AsSingle();
    }

    private void BindUI()
    {
        Container.Bind<SelectedFileView>().FromComponentInHierarchy().AsSingle();
        Container.Bind<ProgressBar>().FromComponentInHierarchy().AsSingle();
        Container.Bind<WindowScript>().FromComponentInHierarchy().AsSingle();
        Container.Bind<NotificationService>().FromComponentInHierarchy().AsSingle();
    }

    private void BindExternal()
    {
        Container.Bind<Canvas>().FromInstance(_canvas).AsSingle();
    }
}