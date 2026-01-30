using UnityEngine;
using Zenject;

public class AppInstaller : MonoInstaller
{
    [SerializeField] private GameConfig _gameConfig;
    
    public override void InstallBindings()
    {
        Container.Bind<GameConfig>().FromInstance(_gameConfig).AsSingle().NonLazy();
    }
}
