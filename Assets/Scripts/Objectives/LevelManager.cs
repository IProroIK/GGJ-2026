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

        private void Awake()
        {
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
            Destroy(CurrentLevel);
            CurrentLevelIndex++;
            CurrentLevelIndex = Mathf.Clamp(CurrentLevelIndex, 0, _levels.Count - 1);
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