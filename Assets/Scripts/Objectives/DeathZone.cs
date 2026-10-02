using UnityEngine;
using Zenject;

namespace Objectives
{
    [RequireComponent(typeof(Collider))]
    public class DeathZone : MonoBehaviour
    {
        private Player.Player _player;

        [Inject]
        private void Construct(Player.Player player)
        {
            _player = player;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (_player != null && other.transform.IsChildOf(_player.transform))
                _player.Health.Kill();
        }
    }
}
