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
        private InputAction _togglePopup;
        private InputAction _navigate;

        private MaskManager _maskManager;
        private MasksData _maskData;

        private List<Enums.MaskType> _masks = new();
        private int _index;
        private bool _openState;
        private Player.Player _player;
        private InfoPopup _infoPopup;

        [Inject]
        private void Construct(MaskManager maskManager, Player.Player player, InfoPopup infoPopup)
        {
            _infoPopup = infoPopup;
            _player = player;
            _maskManager = maskManager;
        }

        private void Awake()
        {
            _maskData = Resources.Load<MasksData>("Data/MaskData");
            
            _gameplay = _input.FindActionMap("Gameplay", true);

            _togglePopup = _gameplay.FindAction("OpenMaskPopup", true);
            _navigate = _gameplay.FindAction("Navigate", true);
            
            _togglePopup.started += OnOpen;
            _togglePopup.canceled += OnClose;
            _navigate.performed += OnNavigate;

            _gameplay.Enable();
            _navigate.Disable();
            
            gameObject.SetActive(false);
            _maskManager.OnMaskUpdated += OnMaskUpdatedEventHandler;

        }

        private void OnDestroy()
        {
            _togglePopup.started -= OnOpen;
            _togglePopup.canceled -= OnClose;
            _navigate.performed -= OnNavigate;
            _maskManager.OnMaskUpdated -= OnMaskUpdatedEventHandler;
        }

        // ---------------- EVENT HANDLERS ----------------

        private void OnMaskUpdatedEventHandler(List<Enums.MaskType> masks)
        {
            _masks = new List<Enums.MaskType>(masks);
            
            SyncIndexWithCurrentMask();
            
            if (_openState && gameObject.activeSelf)
            {
                if (_masks.Count == 0)
                {
                    Close();
                }
                else
                {
                    RefreshImmediate();
                }
            }
        }

        // ---------------- OPEN / CLOSE ----------------

        private void Open()
        {
            if (_openState || _masks.Count == 0)
                return;
            
            SyncIndexWithCurrentMask();
            
            _player.SwitchInputAsses(false);

            _openState = true;
            
            _navigate.Enable();

            gameObject.SetActive(true);
            RefreshImmediate();
        }

        private void Close()
        {
            if (!_openState)
                return;

            _player.SwitchInputAsses(true);

            _openState = false;

            _navigate.Disable();

            gameObject.SetActive(false);
            _infoPopup.Hide();
        }

        // ---------------- INPUT ----------------

        private void OnOpen(InputAction.CallbackContext ctx)
        {
            Open();
        }

        private void OnClose(InputAction.CallbackContext ctx)
        {
            Close();
        }

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
            if (_masks.Count == 0)
            {
                _left.gameObject.SetActive(false);
                _center.gameObject.SetActive(false);
                _right.gameObject.SetActive(false);
                return;
            }

            if (_masks.Count == 1)
            {
                _left.gameObject.SetActive(false);
                _right.gameObject.SetActive(false);
                _center.gameObject.SetActive(true);

                _center.SetInstant(Vector2.zero, 1.4f);
                _center.SetData(GetSprite(_masks[0]), false);
                return;
            }

            // Multiple masks case
            _left.gameObject.SetActive(_masks.Count > 2);
            _right.gameObject.SetActive(true);
            _center.gameObject.SetActive(true);

            var center = Get(0);
            var right = Get(1);

            _center.SetInstant(Vector2.zero, 1.4f);
            _center.SetData(GetSprite(center), false);

            if (_masks.Count > 2)
            {
                var left = Get(-1);
                _left.SetInstant(new Vector2(-_offsetX, 0), 0.85f);
                _left.SetData(GetSprite(left), false);
            }

            _right.SetInstant(new Vector2(_offsetX, 0), 0.85f);
            _right.SetData(GetSprite(right), false);
        }

        private void Animate()
        {
            if (_masks.Count == 0)
                return;

            var center = Get(0);
            var right = Get(1);

            _center.SetData(GetSprite(center), true);
            _center.Move(Vector2.zero, _animDuration);
            _center.SetMain(_animDuration);

            if (_masks.Count > 2)
            {
                var left = Get(-1);
                _left.SetData(GetSprite(left), true);
                _left.Move(new Vector2(-_offsetX, 0), _animDuration);
                _left.SetSecondary(_animDuration);
            }

            _right.SetData(GetSprite(right), true);
            _right.Move(new Vector2(_offsetX, 0), _animDuration);
            _right.SetSecondary(_animDuration);

            _maskManager.SetMask(center);
            var currentMaskModel = GetMaskModel();
            if(currentMaskModel.MaskType == Enums.MaskType.None)
                _infoPopup.Hide();
            else
                _infoPopup.Show($"{currentMaskModel.MaskName} \n\n {currentMaskModel.MaskDescription}", false);
        }

        // ---------------- HELPERS ----------------

        private void SyncIndexWithCurrentMask()
        {
            if (_masks.Count == 0)
            {
                _index = 0;
                return;
            }

            var currentMask = _maskManager.CurrentMask;
            int foundIndex = _masks.IndexOf(currentMask);
            
            if (foundIndex >= 0)
            {
                _index = foundIndex;
            }
            else
            {
                _index = 0;
            }
        }

        private Enums.MaskType Get(int offset)
        {
            return _masks[Mod(_index + offset, _masks.Count)];
        }

        private Sprite GetSprite(Enums.MaskType type)
        {
            return _maskData.Masks.Find(x => x.MaskType == type).MaskSprite;
        }

        private MaskModel GetMaskModel()
        {
            return _maskData.Masks.Find(x => x.MaskType == _maskManager.CurrentMask);
        }

        private int Mod(int v, int m)
        {
            return (v % m + m) % m;
        }
    }
}