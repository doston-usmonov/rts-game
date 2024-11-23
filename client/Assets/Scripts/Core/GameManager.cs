using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using RTS.Units;
using RTS.Buildings;

namespace RTS.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool showDebugInfo = false;

        [Header("Prefabs")]
        [SerializeField] private GameObject heavyTankPrefab;
        [SerializeField] private GameObject bunkerPrefab;

        [Header("UI")]
        [SerializeField] private GameObject gameUI;
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private GameObject debugPanel;

        private List<Unit> activeUnits = new List<Unit>();
        private List<Building> activeBuildings = new List<Building>();
        private bool isPaused = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeGame()
        {
            // Set target frame rate
            Application.targetFrameRate = targetFrameRate;

            // Initialize UI
            if (gameUI) gameUI.SetActive(true);
            if (pauseMenu) pauseMenu.SetActive(false);
            if (debugPanel) debugPanel.SetActive(showDebugInfo);

            // Load saved settings
            LoadGameSettings();
        }

        private void LoadGameSettings()
        {
            // Load any saved settings from PlayerPrefs
            showDebugInfo = PlayerPrefs.GetInt("ShowDebugInfo", 0) == 1;
            if (debugPanel) debugPanel.SetActive(showDebugInfo);
        }

        public void SpawnHeavyTank(Vector3 position)
        {
            if (heavyTankPrefab)
            {
                GameObject tank = Instantiate(heavyTankPrefab, position, Quaternion.identity);
                var unit = tank.GetComponent<HeavyUnit>();
                if (unit)
                {
                    activeUnits.Add(unit);
                }
            }
        }

        public void SpawnBunker(Vector3 position)
        {
            if (bunkerPrefab)
            {
                GameObject bunker = Instantiate(bunkerPrefab, position, Quaternion.identity);
                var building = bunker.GetComponent<Building>();
                if (building)
                {
                    activeBuildings.Add(building);
                }
            }
        }

        public void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0f;
            if (pauseMenu) pauseMenu.SetActive(true);
        }

        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;
            if (pauseMenu) pauseMenu.SetActive(false);
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitGame()
        {
            SaveGameSettings();
            Application.Quit();
        }

        private void SaveGameSettings()
        {
            PlayerPrefs.SetInt("ShowDebugInfo", showDebugInfo ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ToggleDebugInfo()
        {
            showDebugInfo = !showDebugInfo;
            if (debugPanel) debugPanel.SetActive(showDebugInfo);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SaveGameSettings();
            }
        }
    }
}
