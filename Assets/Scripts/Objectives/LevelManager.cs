using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Objectives
{
    public class LevelManager : MonoBehaviour
    {
        public event Action LevelChanged;
        public event Action LevelRestarted;
        public int CurrentLevelIndex { private set; get; }
        public GameObject CurrentLevel { private set; get; }
        
        [SerializeField] private List<LevelData> _levels;
        private Player.Player _player;
        private DiContainer _container;

        [Inject]
        private void Construct(Player.Player player, DiContainer container)
        {
            _container = container;
            _player = player;
        }

        private void Start()
        {
            Application.targetFrameRate = 120;
            LoadLevel(0);
        }
        
        private void LoadLevel(int index)
        {
            CurrentLevel = _container.InstantiatePrefab(_levels[index].Level);

            _player.SetPosition(_levels[index].PlayerPosition.position);
            
            LevelChanged?.Invoke();
        }

        public void LoadNextLevel()
        {
            if (_levels.Count == 0)
                return;

            if (CurrentLevel != null)
                Destroy(CurrentLevel);

            CurrentLevelIndex = (CurrentLevelIndex + 1) % _levels.Count;
            LoadLevel(CurrentLevelIndex);
        }
        public void RestartLevel()
        {
            Destroy(CurrentLevel);
            LoadLevel(CurrentLevelIndex);
            
            LevelRestarted?.Invoke();
        }
    }

    [Serializable]
    public class LevelData
    {
        public GameObject Level;
        public Transform PlayerPosition;
    }
}