using GameUI;
using Mask;
using Objectives;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

public class SceneInstaller : MonoInstaller
{
    [SerializeField, ReadOnly] private Player.Player _player;
    [SerializeField, ReadOnly] private MaskManager _maskManager;
    [SerializeField, ReadOnly] private LevelManager _levelManager;
    [SerializeField, ReadOnly] private InfoPopup _infoPopup;
    [SerializeField, ReadOnly] private MaskPopup _maskPopup;

    private void OnValidate()
        => SetRefs();

    [Button]
    private void SetRefs()
    {
        _player = FindObjectOfType<Player.Player>(true);
        _maskManager = FindObjectOfType<MaskManager>(true);
        _levelManager = FindObjectOfType<LevelManager>(true);
        _infoPopup = FindObjectOfType<InfoPopup>(true);
        _maskPopup = FindObjectOfType<MaskPopup>(true);
    }

    public override void InstallBindings()
    {
        Container.Bind<Player.Player>().FromInstance(_player).AsSingle().NonLazy();
        Container.Bind<MaskManager>().FromInstance(_maskManager).AsSingle().NonLazy();
        Container.Bind<LevelManager>().FromInstance(_levelManager).AsSingle().NonLazy();
        Container.Bind<InfoPopup>().FromInstance(_infoPopup).AsSingle().NonLazy();
        Container.Bind<MaskPopup>().FromInstance(_maskPopup).AsSingle().NonLazy();
    }
}
