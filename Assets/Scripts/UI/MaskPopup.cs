using System.Collections.Generic;
using Mask;
using Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace GameUI
{
    public class MaskPopup : MonoBehaviour
    {
        [Header("Views")] [SerializeField] private MaskItemView _left;
        [SerializeField] private MaskItemView _center;
        [SerializeField] private MaskItemView _right;

        [Header("Layout")] [SerializeField] private float _offsetX = 220f;
        [SerializeField] private float _animDuration = 0.35f;

        [Header("Input")] [SerializeField] private InputActionAsset _input;

        private InputActionMap _gameplay;
        private InputActionMap _ui;
        private InputAction _open;
        private InputAction _navigate;

        private MaskManager _maskManager;
        private MasksData _maskData;

        private readonly List<Enums.MaskType> _masks = new();
        private int _index;
        private bool _openState;

        [Inject]
        private void Construct(MaskManager maskManager)
        {
            _maskManager = maskManager;
        }

        private void Awake()
        {
            _maskData = Resources.Load<MasksData>("Data/MaskData");
            foreach (var m in _maskData.Masks)
                _masks.Add(m.MaskType);

            _gameplay = _input.FindActionMap("Gameplay", true);
            _ui = _input.FindActionMap("MaskScrollable", true);

            _open = _gameplay.FindAction("OpenMaskPopup", true);
            _navigate = _ui.FindAction("Navigate", true);

            _open.started += OnOpen;
            _open.canceled += OnClose;
            _navigate.performed += OnNavigate;

            _gameplay.Enable();
            _ui.Disable();

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _open.started -= OnOpen;
            _open.canceled -= OnClose;
            _navigate.performed -= OnNavigate;
        }

        // ---------------- OPEN / CLOSE ----------------

        private void Open()
        {
            if (_openState || _masks.Count == 0)
                return;

            _openState = true;

            _gameplay.Disable();   // 🔴 THIS WAS MISSING
            _ui.Enable();

            gameObject.SetActive(true);
            RefreshImmediate();
        }


        private void Close()
        {
            if (!_openState)
                return;

            _openState = false;

            _ui.Disable();
            _gameplay.Enable();    // 🔴 RESTORE GAMEPLAY

            gameObject.SetActive(false);
        }

        // ---------------- INPUT ----------------

        private void OnNavigate(InputAction.CallbackContext ctx)
        {
            if (_masks.Count < 2)
                return;

            float v = ctx.ReadValue<float>();
            if (v < 0) Scroll(-1);
            else if (v > 0) Scroll(1);
        }

        private void Scroll(int dir)
        {
            _index = Mod(_index + dir, _masks.Count);
            Animate();
        }

        // ---------------- VISUALS ----------------

        private void RefreshImmediate()
        {
            if (_masks.Count == 1)
            {
                _left.gameObject.SetActive(false);
                _right.gameObject.SetActive(false);

                _center.SetInstant(Vector2.zero, 1.4f);
                _center.SetData(GetSprite(_masks[0]));
                _maskManager.SetMask(_masks[0]);
                return;
            }

            _left.gameObject.SetActive(_masks.Count > 2);
            _right.gameObject.SetActive(true);

            Animate();
        }

        private void Animate()
        {
            var center = Get(0);
            var right = Get(1);

            _center.SetData(GetSprite(center));
            _center.Move(Vector2.zero, _animDuration);
            _center.SetMain(_animDuration);

            if (_masks.Count > 2)
            {
                var left = Get(-1);
                _left.SetData(GetSprite(left));
                _left.Move(new Vector2(-_offsetX, 0), _animDuration);
                _left.SetSecondary(_animDuration);
            }

            _right.SetData(GetSprite(right));
            _right.Move(new Vector2(_offsetX, 0), _animDuration);
            _right.SetSecondary(_animDuration);

            _maskManager.SetMask(center);
        }
        
        private void OnOpen(InputAction.CallbackContext _)
        {
            Open();
        }

        private void OnClose(InputAction.CallbackContext _)
        {
            Close();
        }

        // ---------------- HELPERS ----------------

        private Enums.MaskType Get(int offset)
        {
            return _masks[Mod(_index + offset, _masks.Count)];
        }

        private Sprite GetSprite(Enums.MaskType type)
        {
            return _maskData.Masks.Find(x => x.MaskType == type).MaskSprite;
        }

        private int Mod(int v, int m)
        {
            return (v % m + m) % m;
        }
    }
}