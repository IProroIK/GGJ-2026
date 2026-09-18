using System;
using System.Collections.Generic;
using Mask;
using Objectives;
using Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace GameUI
{
    // Gameplay adapter for MaskPopup. The view only emits selection and visibility events.
    public sealed class MaskPopupPresenter : IInitializable, IDisposable
    {
        private readonly MaskPopup _popup;
        private readonly MaskManager _maskManager;
        private readonly LevelManager _levelManager;
        private readonly List<MaskModel> _entries = new();
        private PlayerInputActions _input;
        private MasksData _data;
        private CursorLockMode _previousCursorLock;
        private bool _previousCursorVisible;
        private bool _ownsCursor;

        public MaskPopupPresenter(MaskPopup popup, MaskManager maskManager, LevelManager levelManager)
        {
            _popup = popup;
            _maskManager = maskManager;
            _levelManager = levelManager;
        }

        public void Initialize()
        {
            _data = Resources.Load<MasksData>("Data/MaskData");
            _popup.MaskSelected += OnMaskSelected;
            _popup.MaskPopupOpened += OnMaskPopupOpened;
            _maskManager.OnMaskUpdated += OnMasksUpdated;
            _levelManager.LevelChanged += OnLevelChanged;
            _levelManager.LevelRestarted += OnLevelChanged;
            OnMasksUpdated(_maskManager.AvailableMasks);
            _input = new PlayerInputActions();
            _input.Gameplay.OpenMaskPopup.started += OnOpen;
            _input.Gameplay.OpenMaskPopup.canceled += OnClose;
            _input.Gameplay.Navigate.performed += OnNavigate;
            _input.Gameplay.OpenMaskPopup.Enable();
            _input.Gameplay.Navigate.Enable();
        }

        private void OnMasksUpdated(IReadOnlyList<Enums.MaskType> masks)
        {
            _entries.Clear();
            if (_data != null && masks != null)
            {
                for (int i = 0; i < masks.Count; i++)
                {
                    Enums.MaskType type = masks[i];
                    MaskModel model = _data.Masks.Find(entry => entry.MaskType == type);
                    if (model != null)
                        _entries.Add(model);
                }
            }
            _popup.SetMasks(_entries);
        }

        private void OnOpen(InputAction.CallbackContext context) => _popup.Open(_entries);
        private void OnNavigate(InputAction.CallbackContext context)
        {
            float direction = context.ReadValue<float>();
            if (Mathf.Abs(direction) > 0.5f)
                _popup.Navigate(direction > 0f ? 1 : -1);
        }
        private void OnClose(InputAction.CallbackContext context)
        {
            if (_popup.IsOpen && _popup.HoveredMask != null)
            {
                OnMaskSelected(_popup.HoveredMask);
                return;
            }

            _popup.Close();
        }

        private void OnLevelChanged() => _popup.Close();

        private void OnMaskSelected(MaskModel model)
        {
            if (model != null && _maskManager.TrySetMask(model.MaskType))
                _popup.Close();
        }

        private void OnMaskPopupOpened(bool open)
        {
            if (open && !_ownsCursor)
            {
                _previousCursorLock = Cursor.lockState;
                _previousCursorVisible = Cursor.visible;
                _ownsCursor = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (!open && _ownsCursor)
            {
                _ownsCursor = false;
                Cursor.lockState = _previousCursorLock;
                Cursor.visible = _previousCursorVisible;
            }
        }

        public void Dispose()
        {
            _popup.MaskSelected -= OnMaskSelected;
            _popup.MaskPopupOpened -= OnMaskPopupOpened;
            _maskManager.OnMaskUpdated -= OnMasksUpdated;
            _levelManager.LevelChanged -= OnLevelChanged;
            _levelManager.LevelRestarted -= OnLevelChanged;
            if (_input != null)
            {
                _input.Gameplay.OpenMaskPopup.started -= OnOpen;
                _input.Gameplay.OpenMaskPopup.canceled -= OnClose;
                _input.Gameplay.Navigate.performed -= OnNavigate;
                _input.Disable();
                _input.Dispose();
            }
            OnMaskPopupOpened(false);
        }
    }
}
