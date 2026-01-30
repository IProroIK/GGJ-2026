using Mask;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

public class SceneInstaller : MonoInstaller
{
    [SerializeField, ReadOnly] private Player.Player _player;
    [SerializeField, ReadOnly] private MaskManager _maskManager;
    
    private void OnValidate()
        => SetRefs();

    [Button]
    private void SetRefs()
    {
        _player = FindObjectOfType<Player.Player>(true);
        _maskManager = FindObjectOfType<MaskManager>(true);
    }

    public override void InstallBindings()
    {
        Container.Bind<Player.Player>().FromInstance(_player).AsSingle().NonLazy();
        Container.Bind<MaskManager>().FromInstance(_maskManager).AsSingle().NonLazy();
    }
}
