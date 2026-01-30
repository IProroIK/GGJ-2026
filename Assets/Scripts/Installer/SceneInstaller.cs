using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

public class SceneInstaller : MonoInstaller
{
    [SerializeField, ReadOnly] private Player.Player _player;
    
    private void OnValidate()
        => SetRefs();

    [Button]
    private void SetRefs()
    {
        _player = FindObjectOfType<Player.Player>(true);
    }

    public override void InstallBindings()
    {
        Container.Bind<Player.Player>().FromInstance(_player).AsSingle().NonLazy();
    }
}
