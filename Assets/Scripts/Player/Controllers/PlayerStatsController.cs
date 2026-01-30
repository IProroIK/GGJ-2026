using Mask;
using Player.Model;
using Settings;

namespace Player.Controllers
{
    public class PlayerStatsController
    {
        private MaskManager _maskManager;
        private Stats _playerStats;

        public PlayerStatsController(MaskManager maskManager, Stats playerStats)
        {
            _playerStats = playerStats;
            _maskManager = maskManager;

            _maskManager.OnMaskEquip += OnMaskEquipEventHandler;
        }

        private void OnMaskEquipEventHandler(Enums.MaskType type)
        {
            switch (type)
            {
                case Enums.MaskType.Strength:
                    _playerStats.gravityModifier = 2;
                    _playerStats.jumpForceModifier = 0.5f;
                    _playerStats.speedModifier = 0.6f;
                    break;
                default:
                    _playerStats.gravityModifier = 1;
                    _playerStats.jumpForceModifier = 1;
                    _playerStats.speedModifier = 1;
                    break;
                    
            }
        }
    }
}