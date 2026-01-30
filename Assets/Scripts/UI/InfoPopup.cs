using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace GameUI
{
    public class InfoPopup : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Button _closeButton;

        private void Awake()
        {
            _root.SetActive(false);
            _closeButton.onClick.AddListener(OnCloseButton);
        }

        public void Show(string message, bool canClose = true)
        {
            _messageText.text = message;
            _root.SetActive(true);
            _closeButton.gameObject.SetActive(canClose);
        }

        public void Hide()
        {
            _root.SetActive(false);
        }

        private void OnCloseButton()
        {
            Hide();
        }
    }

}