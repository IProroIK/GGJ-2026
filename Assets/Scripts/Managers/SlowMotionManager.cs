using System;
using GameUI;
using UnityEngine;
using Zenject;

public sealed class SlowMotionManager : IInitializable, IDisposable
{
    private readonly MaskPopup _maskPopup;
    private readonly float _slowMotionMultiplier;
    private float _previousTimeScale;
    private float _previousFixedDeltaTime;
    private bool _isSlowMotionActive;

    public SlowMotionManager(MaskPopup maskPopup, GameConfig gameConfig)
    {
        _maskPopup = maskPopup;
        _slowMotionMultiplier = Mathf.Clamp(gameConfig.GamePlay.MaskPopupTimeScale, 0.01f, 1f);
    }

    public void Initialize()
    {
        _maskPopup.MaskPopupOpened += OnMaskPopupOpened;

        if (_maskPopup.IsOpen)
            EnableSlowMotion();
    }

    private void OnMaskPopupOpened(bool open)
    {
        if (open)
            EnableSlowMotion();
        else
            DisableSlowMotion();
    }

    private void EnableSlowMotion()
    {
        if (_isSlowMotionActive)
            return;

        _previousTimeScale = Time.timeScale;
        _previousFixedDeltaTime = Time.fixedDeltaTime;
        _isSlowMotionActive = true;

        Time.timeScale = _previousTimeScale * _slowMotionMultiplier;
        Time.fixedDeltaTime = _previousFixedDeltaTime * _slowMotionMultiplier;
    }

    private void DisableSlowMotion()
    {
        if (!_isSlowMotionActive)
            return;

        Time.timeScale = _previousTimeScale;
        Time.fixedDeltaTime = _previousFixedDeltaTime;
        _isSlowMotionActive = false;
    }

    public void Dispose()
    {
        _maskPopup.MaskPopupOpened -= OnMaskPopupOpened;
        DisableSlowMotion();
    }
}
