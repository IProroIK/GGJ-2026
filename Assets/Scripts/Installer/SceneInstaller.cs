using System;
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
    {
#if UNITY_EDITOR
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
            return;
#endif
        ResolveMissingReferences();
    }

    [Button]
    private void SetRefs()
    {
        _player = FindFirstObjectByType<Player.Player>(FindObjectsInactive.Include);
        _maskManager = FindFirstObjectByType<MaskManager>(FindObjectsInactive.Include);
        _levelManager = FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
        _infoPopup = FindFirstObjectByType<InfoPopup>(FindObjectsInactive.Include);
        _maskPopup = FindFirstObjectByType<MaskPopup>(FindObjectsInactive.Include);
    }

    private void ResolveMissingReferences()
    {
        if (_player == null)
            _player = FindFirstObjectByType<Player.Player>(FindObjectsInactive.Include);
        if (_maskManager == null)
            _maskManager = FindFirstObjectByType<MaskManager>(FindObjectsInactive.Include);
        if (_levelManager == null)
            _levelManager = FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
        if (_infoPopup == null)
            _infoPopup = FindFirstObjectByType<InfoPopup>(FindObjectsInactive.Include);
        if (_maskPopup == null)
            _maskPopup = FindFirstObjectByType<MaskPopup>(FindObjectsInactive.Include);
    }

    public override void InstallBindings()
    {
        ResolveMissingReferences();
        if (_player == null || _maskManager == null || _levelManager == null || _infoPopup == null || _maskPopup == null)
            throw new InvalidOperationException($"{nameof(SceneInstaller)} could not resolve all required scene references.");

        Container.Bind<Player.Player>().FromInstance(_player).AsSingle().NonLazy();
        Container.Bind<MaskManager>().FromInstance(_maskManager).AsSingle().NonLazy();
        Container.Bind<LevelManager>().FromInstance(_levelManager).AsSingle().NonLazy();
        Container.Bind<InfoPopup>().FromInstance(_infoPopup).AsSingle().NonLazy();
        Container.Bind<MaskPopup>().FromInstance(_maskPopup).AsSingle().NonLazy();
        Container.BindInterfacesTo<MaskPopupPresenter>().AsSingle().NonLazy();
        Container.BindInterfacesTo<SlowMotionManager>().AsSingle().NonLazy();
    }
}
