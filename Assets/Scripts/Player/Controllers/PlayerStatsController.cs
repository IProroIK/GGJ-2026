using Mask;
using Player.Model;
using Settings;

namespace Player.Controllers
{
    public class PlayerStatsController : System.IDisposable
    {
        private readonly MaskManager _maskManager;
        private readonly Stats _playerStats;

        public PlayerStatsController(MaskManager maskManager, Stats playerStats)
        {
            _playerStats = playerStats;
            _maskManager = maskManager;

            _maskManager.OnMaskEquip += OnMaskEquipEventHandler;
            _maskManager.OnMaskUnequip += OnMaskUnequipped;
            OnMaskEquipEventHandler(_maskManager.CurrentMask);
        }

        public void Dispose()
        {
            _maskManager.OnMaskEquip -= OnMaskEquipEventHandler;
            _maskManager.OnMaskUnequip -= OnMaskUnequipped;
        }

        private void OnMaskUnequipped(Enums.MaskType type)
        {
            ResetToDefault();
        }

        private void OnMaskEquipEventHandler(Enums.MaskType type)
        {
            ResetToDefault();
            
            switch (type)
            {
                case Enums.MaskType.Strength:
                case Enums.MaskType.Mover:
                    _playerStats.GravityModifier = 2;
                    _playerStats.JumpForceModifier = 0.5f;
                    _playerStats.SpeedModifier = 0.6f;
                    break;
                case Enums.MaskType.Agility:
                    _playerStats.SpeedModifier = 1.15f;
                    _playerStats.JumpCountModifier = 2;
                    break;
            }
        }

        public void ResetToDefault()
        {
            _playerStats.GravityModifier = 1;
            _playerStats.JumpForceModifier = 1;
            _playerStats.SpeedModifier = 1;
            _playerStats.JumpCountModifier = 1;
        }
    }
}
