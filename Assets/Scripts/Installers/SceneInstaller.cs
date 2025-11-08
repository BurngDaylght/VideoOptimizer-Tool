using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public class SceneInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<FileSelector>().AsSingle();
        Container.BindInterfacesAndSelfTo<FileProcessor>().AsSingle();
        
        Container.Bind<SelectedFileText>().FromComponentInHierarchy().AsSingle();
    }
}