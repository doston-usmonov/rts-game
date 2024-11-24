using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using RTS.Units;
using RTS.Units.Combat;
using RTS.Commands;
using RTS.Vision;
using RTS.Buildings;
using RTS;

namespace RTS.UI
{
    public class TacticalMapUI : MonoBehaviour, IPointerClickHandler, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Map Settings")]
        public RawImage mapTexture;
        public RectTransform mapRect;
        public float mapScale = 1f;
        public Color bunkerRangeColor = new Color(0.4f, 0.6f, 1f, 0.3f);
        public Color artilleryRangeColor = new Color(1f, 0.6f, 0.2f, 0.3f);
        public Color overlapColor = new Color(0.8f, 0.4f, 1f, 0.4f);

        [Header("Unit Indicators")]
        public GameObject bunkerIndicatorPrefab;
        public GameObject artilleryIndicatorPrefab;
        public float updateInterval = 0.25f;

        [Header("Zoom Settings")]
        public float minZoom = 0.5f;
        public float maxZoom = 2.0f;
        public float zoomSpeed = 0.15f;
        public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Filter Settings")]
        public Toggle lowAmmoFilterToggle;
        public Toggle underAttackFilterToggle;
        public Toggle fortifyModeFilterToggle;
        public Toggle synergiesFilterToggle;

        [Header("Keyboard Shortcuts")]
        public KeyCode lowAmmoKey = KeyCode.L;
        public KeyCode underAttackKey = KeyCode.U;
        public KeyCode fortifyKey = KeyCode.F;
        public KeyCode synergiesKey = KeyCode.S;
        public KeyCode customFilter1Key = KeyCode.Alpha1;
        public KeyCode customFilter2Key = KeyCode.Alpha2;
        public KeyCode customFilter3Key = KeyCode.Alpha3;

        [Header("Custom Filter UI")]
        public GameObject customFilterPanel;
        public Button createCustomFilterButton;
        public Transform customFilterContainer;
        public GameObject customFilterPrefab;

        [Header("Selection and Commands")]
        public Color selectionHighlightColor = new Color(0f, 1f, 0f, 0.5f);
        public GameObject commandPanelPrefab;
        public Transform commandPanelContainer;
        public float commandPanelOffset = 10f;

        [Header("Enemy Detection")]
        public GameObject enemyIndicatorPrefab;
        public Color enemyIndicatorColor = new Color(1f, 0.2f, 0.2f, 0.8f);
        public Color enemyStealthUnitColor = new Color(1f, 0.2f, 0.2f, 0.4f);
        public float visionUpdateInterval = 0.5f;

        [Header("Drag Selection")]
        public Color dragBoxColor = new Color(0.2f, 0.8f, 1f, 0.3f);
        public Color dragBoxBorderColor = new Color(0.2f, 0.8f, 1f, 0.8f);
        public float dragBoxBorderWidth = 2f;

        [Header("Unit Statistics")]
        public GameObject statsOverlayPrefab;
        public float statsUpdateInterval = 0.25f;
        public Color healthBarColor = Color.green;
        public Color ammoBarColor = Color.yellow;
        public Vector2 statsOffset = new Vector2(0, 30f);

        [Header("Unit Groups")]
        public int maxUnitGroups = 10;
        public KeyCode groupModifierKey = KeyCode.LeftControl;
        public Color groupNumberColor = new Color(1f, 1f, 1f, 0.8f);
        public GameObject groupNumberPrefab;

        [Header("Enemy Information")]
        public GameObject enemyInfoPanelPrefab;
        public float enemyInfoUpdateInterval = 0.25f;
        public Color enemyHealthBarColor = Color.red;
        public Color enemyAmmoBarColor = new Color(1f, 0.6f, 0f);
        public float enemyInfoDisplayRange = 20f;

        [Header("Minimap")]
        public RawImage minimapImage;
        public RectTransform minimapRect;
        public float minimapScale = 0.2f;
        public Color minimapBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        public Color friendlyUnitColor = new Color(0f, 0.7f, 1f, 1f);
        public Color enemyUnitColor = new Color(1f, 0.2f, 0.2f, 1f);
        public Color selectedUnitColor = new Color(0f, 1f, 0f, 1f);
        public Vector2 minimapSize = new Vector2(200f, 200f);

        [Header("Group Customization")]
        public List<Sprite> groupIcons;
        public List<Color> groupColors;
        public GameObject groupMarkerPrefab;
        public GameObject groupWaypointPrefab;
        public float waypointLineWidth = 2f;
        public Color waypointLineColor = new Color(1f, 1f, 1f, 0.5f);

        [Header("Advanced Filtering")]
        public GameObject filterPanelPrefab;
        public Transform filterPresetContainer;
        public int maxFilterPresets = 10;
        public Color activeFilterColor = new Color(0f, 1f, 0.5f, 1f);

        [Header("Formation Controls")]
        public List<FormationPreset> defaultFormations;
        public float formationSpacing = 2f;
        public float formationRotationSpeed = 120f;
        public GameObject formationPreviewPrefab;

        [Header("Tactical Overlay")]
        [SerializeField] private GroupTacticalOverlay tacticalOverlay;

        [Header("Formation System")]
        [SerializeField] private SpecializedFormation formationSystem;
        private Dictionary<int, SpecializedFormation.UnitType> groupUnitTypes = new Dictionary<int, SpecializedFormation.UnitType>();

        [Header("Terrain Analysis")]
        [SerializeField] private TerrainAnalyzer terrainAnalyzer;
        [SerializeField] private float terrainUpdateInterval = 0.5f;
        private float lastTerrainUpdateTime;

        [Header("UI References")]
        [SerializeField] private Camera tacticalCamera;
        [SerializeField] private RectTransform selectionBox;
        [SerializeField] private Image selectionBoxImage;
        [SerializeField] private Color selectionBoxColor = new Color(0f, 1f, 0f, 0.3f);

        [Header("Selection")]
        private Vector2 dragStartPosition;
        private bool isDragging;
        private List<Unit> selectedUnits = new List<Unit>();
        private Dictionary<Artillery, GameObject> artilleryIndicators = new Dictionary<Artillery, GameObject>();
        private Dictionary<HeavyDefenseBunker, GameObject> bunkerIndicators = new Dictionary<HeavyDefenseBunker, GameObject>();
        private Dictionary<EnemyUnit, GameObject> enemyIndicators = new Dictionary<EnemyUnit, GameObject>();
        private HashSet<MonoBehaviour> unitsUnderAttack = new HashSet<MonoBehaviour>();
        private HashSet<Artillery> lowAmmoUnits = new HashSet<Artillery>();
        private HashSet<HeavyDefenseBunker> fortifiedBunkers = new HashSet<HeavyDefenseBunker>();
        private HashSet<EnemyUnit> detectedEnemies = new HashSet<EnemyUnit>();

        [Header("Map")]
        private RenderTexture mapRenderTexture;
        private float nextUpdateTime;
        private float nextVisionUpdate;
        private float currentZoom = 1f;
        private Vector2 lastClickPosition;
        private GameObject activeCommandPanel;

        [Header("Groups")]
        private Dictionary<int, HashSet<MonoBehaviour>> unitGroups = new Dictionary<int, HashSet<MonoBehaviour>>();
        private Dictionary<MonoBehaviour, GameObject> statsOverlays = new Dictionary<MonoBehaviour, GameObject>();
        
        [Header("Filters")]
        private List<CustomFilterConfig> customFilters = new List<CustomFilterConfig>();
        private Dictionary<KeyCode, System.Action> keyboardShortcuts = new Dictionary<KeyCode, System.Action>();

        private void Start()
        {
            if (selectionBox != null && selectionBoxImage != null)
            {
                selectionBoxImage.color = selectionBoxColor;
                selectionBox.gameObject.SetActive(false);
            }

            InitializeTacticalMap();
            InitializeFilters();
            InitializeKeyboardShortcuts();
            InitializeCustomFilters();
            InitializeSelectionSystem();
            InitializeDragSelection();
            InitializeUnitGroups();
            InitializeMinimap();
            InitializeGroupCustomization();
            InitializeAdvancedFiltering();
            InitializeFormations();

            StartCoroutine(UpdateVisionRoutine());
            StartCoroutine(UpdateStatsRoutine());
            StartCoroutine(UpdateEnemyInfoRoutine());
        }

        private void InitializeTacticalMap()
        {
            // Create render texture for the tactical map
            mapRenderTexture = new RenderTexture(1024, 1024, 0);
            mapRenderTexture.name = "TacticalMapRT";

            // Set up tactical camera
            GameObject cameraObj = new GameObject("TacticalCamera");
            tacticalCamera = cameraObj.AddComponent<Camera>();
            tacticalCamera.orthographic = true;
            tacticalCamera.cullingMask = LayerMask.GetMask("TacticalMap");
            tacticalCamera.clearFlags = CameraClearFlags.SolidColor;
            tacticalCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            tacticalCamera.targetTexture = mapRenderTexture;

            // Configure map display
            if (mapTexture != null)
            {
                mapTexture.texture = mapRenderTexture;
            }
        }

        private void InitializeFilters()
        {
            if (lowAmmoFilterToggle != null)
                lowAmmoFilterToggle.onValueChanged.AddListener(OnLowAmmoFilterChanged);
            if (underAttackFilterToggle != null)
                underAttackFilterToggle.onValueChanged.AddListener(OnUnderAttackFilterChanged);
            if (fortifyModeFilterToggle != null)
                fortifyModeFilterToggle.onValueChanged.AddListener(OnFortifyFilterChanged);
            if (synergiesFilterToggle != null)
                synergiesFilterToggle.onValueChanged.AddListener(OnSynergiesFilterChanged);
        }

        private void InitializeKeyboardShortcuts()
        {
            // Basic filters
            keyboardShortcuts[lowAmmoKey] = () => ToggleFilter(lowAmmoFilterToggle);
            keyboardShortcuts[underAttackKey] = () => ToggleFilter(underAttackFilterToggle);
            keyboardShortcuts[fortifyKey] = () => ToggleFilter(fortifyModeFilterToggle);
            keyboardShortcuts[synergiesKey] = () => ToggleFilter(synergiesFilterToggle);

            // Custom filters
            keyboardShortcuts[customFilter1Key] = () => ActivateCustomFilter(0);
            keyboardShortcuts[customFilter2Key] = () => ActivateCustomFilter(1);
            keyboardShortcuts[customFilter3Key] = () => ActivateCustomFilter(2);
        }

        private void InitializeCustomFilters()
        {
            if (createCustomFilterButton != null)
            {
                createCustomFilterButton.onClick.AddListener(ShowCreateCustomFilterPanel);
            }

            // Load saved custom filters
            var savedData = PlayerPrefs.GetString("CustomFilters", "");
            if (!string.IsNullOrEmpty(savedData))
            {
                customFilters = JsonUtility.FromJson<CustomFiltersData>(savedData).filters;

                foreach (var filter in customFilters)
                {
                    CreateCustomFilterUI(filter);
                }
            }
        }

        private void InitializeSelectionSystem()
        {
            // Create selection box for drag selection
            GameObject selectionBoxObj = new GameObject("SelectionBox");
            selectionBox = selectionBoxObj.AddComponent<RectTransform>();
            Image selectionImage = selectionBoxObj.AddComponent<Image>();
            selectionImage.color = new Color(0.2f, 0.8f, 1f, 0.3f);
            selectionBoxObj.transform.SetParent(transform, false);
            selectionBox.gameObject.SetActive(false);
        }

        private void InitializeDragSelection()
        {
            // Create drag selection box
            GameObject dragBoxObj = new GameObject("DragSelectionBox");
            RectTransform dragSelectionBox = dragBoxObj.AddComponent<RectTransform>();
            Image dragBoxImage = dragBoxObj.AddComponent<Image>();
            dragBoxImage.color = dragBoxColor;

            // Add border effect
            var border = new GameObject("Border").AddComponent<RectTransform>();
            border.transform.SetParent(dragSelectionBox, false);
            var borderImage = border.gameObject.AddComponent<Image>();
            borderImage.color = dragBoxBorderColor;

            dragSelectionBox.transform.SetParent(transform, false);
            dragSelectionBox.gameObject.SetActive(false);
        }

        private void InitializeUnitGroups()
        {
            for (int i = 0; i < maxUnitGroups; i++)
            {
                unitGroups[i] = new HashSet<MonoBehaviour>();
            }
        }

        private void InitializeMinimap()
        {
            // Create minimap render texture
            RenderTexture minimapRenderTexture = new RenderTexture(
                (int)minimapSize.x,
                (int)minimapSize.y,
                16,
                RenderTextureFormat.ARGB32
            );

            // Create and setup minimap camera
            GameObject minimapCameraObj = new GameObject("MinimapCamera");
            Camera minimapCamera = minimapCameraObj.AddComponent<Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.cullingMask = LayerMask.GetMask("Terrain", "Units", "Buildings");
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = minimapBackgroundColor;
            minimapCamera.targetTexture = minimapRenderTexture;
            minimapCamera.orthographicSize = 50f; // Adjust based on map size

            // Setup minimap UI
            if (minimapImage != null)
            {
                minimapImage.texture = minimapRenderTexture;
                minimapRect.sizeDelta = minimapSize;
            }

            // Position camera
            minimapCamera.transform.position = new Vector3(0, 100f, 0);
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void InitializeGroupCustomization()
        {
            // Load saved group data if exists
            var savedData = PlayerPrefs.GetString("GroupCustomization", "");
            if (!string.IsNullOrEmpty(savedData))
            {
                Dictionary<int, GroupData> groupData = JsonUtility.FromJson<Dictionary<int, GroupData>>(savedData);
            }
        }

        private void InitializeAdvancedFiltering()
        {
            // Load saved filter presets
            var savedPresets = PlayerPrefs.GetString("FilterPresets", "");
            if (!string.IsNullOrEmpty(savedPresets))
            {
                List<FilterPreset> filterPresets = JsonUtility.FromJson<List<FilterPreset>>(savedPresets);
            }

            CreateFilterPanel();
        }

        private void InitializeFormations()
        {
            // Initialize default formations
            defaultFormations = new List<FormationPreset>
            {
                new FormationPreset 
                { 
                    name = "Line",
                    type = FormationType.Line,
                    relativePositions = GenerateLineFormation(10)
                },
                new FormationPreset 
                { 
                    name = "Column",
                    type = FormationType.Column,
                    relativePositions = GenerateColumnFormation(10)
                },
                new FormationPreset 
                { 
                    name = "Wedge",
                    type = FormationType.Wedge,
                    relativePositions = GenerateWedgeFormation(10)
                },
                new FormationPreset 
                { 
                    name = "Square",
                    type = FormationType.Square,
                    relativePositions = GenerateSquareFormation(10)
                }
            };
        }

        private void CreateFilterPanel()
        {
            filterPanel = Instantiate(filterPanelPrefab, transform);
            var filterUI = filterPanel.GetComponent<FilterPanelUI>();
            if (filterUI != null)
            {
                filterUI.Initialize(this);
                filterPanel.SetActive(false);
            }
        }

        private void Update()
        {
            // Handle keyboard shortcuts
            foreach (var shortcut in keyboardShortcuts)
            {
                if (Input.GetKeyDown(shortcut.Key))
                {
                    shortcut.Value.Invoke();
                }
            }

            // Handle unit group hotkeys
            if (Input.GetKey(groupModifierKey))
            {
                for (int i = 0; i < maxUnitGroups; i++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                    {
                        AssignSelectedUnitsToGroup(i);
                    }
                }
            }
            else
            {
                for (int i = 0; i < maxUnitGroups; i++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                    {
                        SelectUnitGroup(i);
                    }
                }
            }
        }

        private void ToggleFilter(Toggle filterToggle)
        {
            if (filterToggle != null)
            {
                filterToggle.isOn = !filterToggle.isOn;
            }
        }

        private void ShowCreateCustomFilterPanel()
        {
            if (customFilterPanel != null)
            {
                customFilterPanel.SetActive(true);
            }
        }

        public void CreateCustomFilter(string filterName, FilterCriteria criteria)
        {
            var newFilter = new CustomFilterConfig
            {
                name = filterName,
                criteria = criteria
            };

            customFilters.Add(newFilter);
            CreateCustomFilterUI(newFilter);
            SaveCustomFilters();
        }

        private void CreateCustomFilterUI(CustomFilterConfig filter)
        {
            if (customFilterContainer == null || customFilterPrefab == null) return;

            GameObject filterObj = Instantiate(customFilterPrefab, customFilterContainer);
            var toggle = filterObj.GetComponent<Toggle>();
            var label = filterObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            if (toggle != null && label != null)
            {
                label.text = filter.name;
                toggle.onValueChanged.AddListener((isOn) => ApplyCustomFilter(filter, isOn));
            }
        }

        private void ActivateCustomFilter(int index)
        {
            if (index < 0 || index >= customFilters.Count) return;

            var filter = customFilters[index];
            var toggle = customFilterContainer.GetChild(index)?.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.isOn = !toggle.isOn;
            }
        }

        private void ApplyCustomFilter(CustomFilterConfig filter, bool isActive)
        {
            if (!isActive)
            {
                UpdateFilters();
                return;
            }

            foreach (var pair in artilleryIndicators)
            {
                if (pair.Key == null || pair.Value == null) continue;

                bool matchesCriteria = CheckFilterCriteria(pair.Key, filter.criteria);
                var indicator = pair.Value.GetComponent<ArtilleryMapIndicator>();
                if (indicator != null)
                {
                    indicator.SetHighlight(matchesCriteria);
                }
            }

            foreach (var pair in bunkerIndicators)
            {
                if (pair.Key == null || pair.Value == null) continue;

                bool matchesCriteria = CheckFilterCriteria(pair.Key, filter.criteria);
                var indicator = pair.Value.GetComponent<Image>();
                if (indicator != null)
                {
                    indicator.color = matchesCriteria ?
                        new Color(1f, 1f, 1f, 1f) :
                        new Color(0.4f, 0.6f, 1f, 0.7f);
                }
            }
        }

        private bool CheckFilterCriteria(MonoBehaviour unit, FilterCriteria criteria)
        {
            if (unit is Artillery artillery)
            {
                return (criteria.lowAmmo && artillery.IsLowOnAmmo()) ||
                       (criteria.underAttack && unitsUnderAttack.Contains(artillery)) ||
                       (criteria.deployed && artillery.IsDeployed);
            }
            else if (unit is HeavyDefenseBunker bunker)
            {
                return (criteria.fortifyMode && bunker.IsInFortifyMode) ||
                       (criteria.underAttack && unitsUnderAttack.Contains(bunker)) ||
                       (criteria.hasGarrison && bunker.HasGarrison);
            }

            return false;
        }

        private void SaveCustomFilters()
        {
            string json = JsonUtility.ToJson(new CustomFiltersData { filters = customFilters });
            PlayerPrefs.SetString("CustomFilters", json);
            PlayerPrefs.Save();
        }

        private void LoadCustomFilters()
        {
            if (PlayerPrefs.HasKey("CustomFilters"))
            {
                string json = PlayerPrefs.GetString("CustomFilters");
                var data = JsonUtility.FromJson<CustomFiltersData>(json);
                customFilters = data.filters;

                foreach (var filter in customFilters)
                {
                    CreateCustomFilterUI(filter);
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            lastClickPosition = eventData.position;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (eventData.clickCount == 2)
                {
                    HandleDoubleClick(eventData);
                }
                else
                {
                    HandleUnitSelection(eventData);
                }
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                HandleCommand(eventData);
            }
        }

        private void HandleUnitSelection(PointerEventData eventData)
        {
            Ray ray = tacticalCamera.ScreenPointToRay(eventData.position);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            if (!Input.GetKey(KeyCode.LeftShift))
            {
                ClearSelection();
            }

            if (hit.collider != null)
            {
                var unit = hit.collider.GetComponent<MonoBehaviour>();
                if (IsSelectableUnit(unit))
                {
                    SelectUnit(unit);
                    UpdateCommandPanel();
                }
            }
        }

        private void HandleCommand(PointerEventData eventData)
        {
            if (selectedUnits.Count == 0) return;

            Vector3 worldPoint = tacticalCamera.ScreenToWorldPoint(eventData.position);
            worldPoint.z = 0;

            // Show command ring animation
            ShowCommandRing(eventData.position);

            foreach (var unit in selectedUnits)
            {
                if (unit is Artillery artillery)
                {
                    IssueArtilleryCommand(artillery, worldPoint);
                }
                else if (unit is HeavyDefenseBunker bunker)
                {
                    IssueBunkerCommand(bunker, worldPoint);
                }
            }
        }

        private void ShowCommandRing(Vector2 position)
        {
            // Instantiate command ring effect
            GameObject ring = Instantiate(commandRingPrefab, position, Quaternion.identity, transform);
            StartCoroutine(AnimateCommandRing(ring));
        }

        private IEnumerator AnimateCommandRing(GameObject ring)
        {
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(0.5f, 2f, elapsed / duration);
                ring.transform.localScale = new Vector3(scale, scale, 1f);
                ring.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f - (elapsed / duration));
                yield return null;
            }

            Destroy(ring);
        }

        private void IssueArtilleryCommand(Artillery artillery, Vector3 targetPosition)
        {
            if (artillery.IsDeployed)
            {
                // Issue attack command if deployed
                CommandManager.Instance.IssueCommand(new AttackCommand(artillery, targetPosition));
            }
            else
            {
                // Issue move command if not deployed
                CommandManager.Instance.IssueCommand(new MoveCommand(artillery, targetPosition));
            }
        }

        private void IssueBunkerCommand(HeavyDefenseBunker bunker, Vector3 targetPosition)
        {
            // Bunkers can't move, but can set rally points or targeting priority
            if (Vector3.Distance(bunker.transform.position, targetPosition) < bunker.Range)
            {
                CommandManager.Instance.IssueCommand(new SetPriorityTargetCommand(bunker, targetPosition));
            }
        }

        private void SelectUnit(MonoBehaviour unit)
        {
            if (!selectedUnits.Contains(unit))
            {
                selectedUnits.Add(unit);
                HighlightSelectedUnit(unit, true);
            }
        }

        private void ClearSelection()
        {
            foreach (var unit in selectedUnits)
            {
                HighlightSelectedUnit(unit, false);
            }
            selectedUnits.Clear();
            HideCommandPanel();
        }

        private void HighlightSelectedUnit(MonoBehaviour unit, bool selected)
        {
            GameObject indicator = null;
            if (unit is Artillery artillery && artilleryIndicators.TryGetValue(artillery, out indicator))
            {
                var mapIndicator = indicator.GetComponent<ArtilleryMapIndicator>();
                if (mapIndicator != null)
                {
                    mapIndicator.SetSelectionHighlight(selected);
                }
            }
            else if (unit is HeavyDefenseBunker bunker && bunkerIndicators.TryGetValue(bunker, out indicator))
            {
                var image = indicator.GetComponent<Image>();
                if (image != null)
                {
                    image.color = selected ? selectionHighlightColor : Color.white;
                }
            }
        }

        private void UpdateCommandPanel()
        {
            if (selectedUnits.Count == 0)
            {
                HideCommandPanel();
                return;
            }

            if (activeCommandPanel == null)
            {
                activeCommandPanel = Instantiate(commandPanelPrefab, commandPanelContainer);
            }

            var commandUI = activeCommandPanel.GetComponent<CommandPanelUI>();
            if (commandUI != null)
            {
                commandUI.UpdateForSelection(selectedUnits);
                PositionCommandPanel();
            }
        }

        private void PositionCommandPanel()
        {
            if (activeCommandPanel == null) return;

            Vector2 screenPoint = lastClickPosition + new Vector2(commandPanelOffset, commandPanelOffset);
            activeCommandPanel.transform.position = screenPoint;

            // Ensure panel stays within screen bounds
            var panelRect = activeCommandPanel.GetComponent<RectTransform>();
            Vector2 screenBounds = new Vector2(Screen.width, Screen.height);
            Vector2 panelSize = panelRect.sizeDelta;

            if (screenPoint.x + panelSize.x > screenBounds.x)
            {
                screenPoint.x = screenBounds.x - panelSize.x;
            }
            if (screenPoint.y + panelSize.y > screenBounds.y)
            {
                screenPoint.y = screenBounds.y - panelSize.y;
            }

            activeCommandPanel.transform.position = screenPoint;
        }

        private void HideCommandPanel()
        {
            if (activeCommandPanel != null)
            {
                Destroy(activeCommandPanel);
                activeCommandPanel = null;
            }
        }

        private bool IsSelectableUnit(MonoBehaviour unit)
        {
            return unit is Artillery || unit is HeavyDefenseBunker;
        }

        public void OnScroll(PointerEventData eventData)
        {
            float zoomDelta = eventData.scrollDelta.y * zoomSpeed;
            float newZoom = Mathf.Clamp(currentZoom + zoomDelta, minZoom, maxZoom);

            if (newZoom != currentZoom)
            {
                // Get mouse position before zoom
                Vector2 mousePos = eventData.position;
                Vector2 beforeZoomWorld = MapToWorldPosition(mousePos);

                currentZoom = newZoom;
                UpdateMapScale();

                // Get mouse position after zoom
                Vector2 afterZoomWorld = MapToWorldPosition(mousePos);
                Vector2 correction = afterZoomWorld - beforeZoomWorld;

                // Adjust map position to keep mouse over same world point
                mapRect.anchoredPosition -= correction * mapScale;
            }
        }

        private void UpdateMapScale()
        {
            float zoomFactor = zoomCurve.Evaluate(
                (currentZoom - minZoom) / (maxZoom - minZoom)
            );
            mapRect.localScale = Vector3.one * zoomFactor;
        }

        private void FocusOnPosition(Vector2 worldPosition)
        {
            Vector2 mapCenter = mapRect.rect.center;
            Vector2 targetPos = WorldToMapPosition(new Vector3(worldPosition.x, 0, worldPosition.y));
            mapRect.anchoredPosition = mapCenter - targetPos * mapScale;
        }

        private System.Collections.IEnumerator UpdateTacticalMapRoutine()
        {
            while (true)
            {
                if (Time.time >= nextUpdateTime)
                {
                    UpdateTacticalMap();
                    nextUpdateTime = Time.time + updateInterval;
                }
                yield return new WaitForSeconds(updateInterval);
            }
        }

        private void UpdateTacticalMap()
        {
            UpdateBunkers();
            UpdateArtillery();
            UpdateRangeOverlays();
            UpdateFilters();
        }

        private void UpdateBunkers()
        {
            var bunkers = FindObjectsOfType<HeavyDefenseBunker>();

            // Remove indicators for destroyed bunkers
            List<HeavyDefenseBunker> deadBunkers = new List<HeavyDefenseBunker>();
            foreach (var pair in bunkerIndicators)
            {
                if (pair.Key == null)
                {
                    Destroy(pair.Value);
                    deadBunkers.Add(pair.Key);
                }
            }
            foreach (var deadBunker in deadBunkers)
            {
                bunkerIndicators.Remove(deadBunker);
            }

            // Update or create indicators for active bunkers
            foreach (var bunker in bunkers)
            {
                if (!bunkerIndicators.ContainsKey(bunker))
                {
                    GameObject indicator = Instantiate(bunkerIndicatorPrefab, mapRect);
                    bunkerIndicators[bunker] = indicator;
                }

                // Update position and rotation
                Vector2 mapPosition = WorldToMapPosition(bunker.transform.position);
                bunkerIndicators[bunker].GetComponent<RectTransform>().anchoredPosition = mapPosition;
                bunkerIndicators[bunker].transform.rotation = Quaternion.Euler(0, 0, bunker.transform.eulerAngles.y);

                // Update range indicator
                var rangeIndicator = bunkerIndicators[bunker].GetComponent<Image>();
                if (rangeIndicator != null)
                {
                    float range = bunker.GetComponent<HeavyAssaultTactics>()?.artilleryShieldRadius ?? 20f;
                    rangeIndicator.transform.localScale = Vector3.one * (range * mapScale);
                    rangeIndicator.color = bunkerRangeColor;
                }
            }
        }

        private void UpdateArtillery()
        {
            var artilleryUnits = FindObjectsOfType<Artillery>();

            // Remove indicators for destroyed artillery
            List<Artillery> deadArtillery = new List<Artillery>();
            foreach (var pair in artilleryIndicators)
            {
                if (pair.Key == null)
                {
                    Destroy(pair.Value);
                    deadArtillery.Add(pair.Key);
                }
            }
            foreach (var deadArt in deadArtillery)
            {
                artilleryIndicators.Remove(deadArt);
            }

            // Update or create indicators for active artillery
            foreach (var artillery in artilleryUnits)
            {
                if (!artilleryIndicators.ContainsKey(artillery))
                {
                    GameObject indicator = Instantiate(artilleryIndicatorPrefab, mapRect);
                    artilleryIndicators[artillery] = indicator;
                }

                // Update position and rotation
                Vector2 mapPosition = WorldToMapPosition(artillery.transform.position);
                artilleryIndicators[artillery].GetComponent<RectTransform>().anchoredPosition = mapPosition;
                artilleryIndicators[artillery].transform.rotation = Quaternion.Euler(0, 0, artillery.transform.eulerAngles.y);

                // Update range and status
                var indicatorUI = artilleryIndicators[artillery].GetComponent<ArtilleryMapIndicator>();
                if (indicatorUI != null)
                {
                    indicatorUI.UpdateStatus(artillery.IsDeployed, artillery.GetAmmoPercentage());
                }

                // Update range indicator
                var rangeIndicator = artilleryIndicators[artillery].GetComponent<Image>();
                if (rangeIndicator != null)
                {
                    float range = artillery.IsDeployed ? artillery.GetAttackRange() : 0f;
                    rangeIndicator.transform.localScale = Vector3.one * (range * mapScale);
                    rangeIndicator.color = artilleryRangeColor;
                }
            }
        }

        private void UpdateRangeOverlays()
        {
            foreach (var bunkerPair in bunkerIndicators)
            {
                if (bunkerPair.Key == null) continue;

                foreach (var artilleryPair in artilleryIndicators)
                {
                    if (artilleryPair.Key == null) continue;

                    // Check if artillery is in bunker's range
                    float distance = Vector3.Distance(
                        bunkerPair.Key.transform.position,
                        artilleryPair.Key.transform.position
                    );

                    float bunkerRange = bunkerPair.Key.GetComponent<HeavyAssaultTactics>()?.artilleryShieldRadius ?? 20f;

                    if (distance <= bunkerRange && artilleryPair.Key.IsDeployed)
                    {
                        // Create or update synergy indicator
                        DrawSynergyLine(
                            WorldToMapPosition(bunkerPair.Key.transform.position),
                            WorldToMapPosition(artilleryPair.Key.transform.position),
                            overlapColor
                        );
                    }
                }
            }
        }

        private void UpdateFilters()
        {
            // Update low ammo units
            lowAmmoUnits.Clear();
            foreach (var pair in artilleryIndicators)
            {
                if (pair.Key != null && pair.Key.IsLowOnAmmo())
                {
                    lowAmmoUnits.Add(pair.Key);
                }
            }

            // Update fortified bunkers
            fortifiedBunkers.Clear();
            foreach (var pair in bunkerIndicators)
            {
                if (pair.Key != null && pair.Key.IsInFortifyMode)
                {
                    fortifiedBunkers.Add(pair.Key);
                }
            }

            // Apply visual filters
            foreach (var pair in artilleryIndicators)
            {
                if (pair.Key == null || pair.Value == null) continue;

                bool isHighlighted = false;
                if (lowAmmoFilterToggle != null && lowAmmoFilterToggle.isOn)
                    isHighlighted |= lowAmmoUnits.Contains(pair.Key);
                if (underAttackFilterToggle != null && underAttackFilterToggle.isOn)
                    isHighlighted |= unitsUnderAttack.Contains(pair.Key);

                // Apply highlight effect
                var indicator = pair.Value.GetComponent<ArtilleryMapIndicator>();
                if (indicator != null)
                {
                    indicator.SetHighlight(isHighlighted);
                }
            }

            foreach (var pair in bunkerIndicators)
            {
                if (pair.Key == null || pair.Value == null) continue;

                bool isHighlighted = false;
                if (fortifyModeFilterToggle != null && fortifyModeFilterToggle.isOn)
                    isHighlighted |= fortifiedBunkers.Contains(pair.Key);
                if (underAttackFilterToggle != null && underAttackFilterToggle.isOn)
                    isHighlighted |= unitsUnderAttack.Contains(pair.Key);

                // Apply highlight effect
                var indicator = pair.Value.GetComponent<Image>();
                if (indicator != null)
                {
                    indicator.color = isHighlighted ?
                        new Color(1f, 1f, 1f, 1f) :
                        new Color(0.4f, 0.6f, 1f, 0.7f);
                }
            }
        }

        private void OnLowAmmoFilterChanged(bool isOn)
        {
            UpdateFilters();
        }

        private void OnUnderAttackFilterChanged(bool isOn)
        {
            UpdateFilters();
        }

        private void OnFortifyFilterChanged(bool isOn)
        {
            UpdateFilters();
        }

        private void OnSynergiesFilterChanged(bool isOn)
        {
            UpdateFilters();
        }

        public void NotifyUnitUnderAttack(MonoBehaviour unit)
        {
            if (!unitsUnderAttack.Contains(unit))
            {
                unitsUnderAttack.Add(unit);
                StartCoroutine(ClearAttackStatus(unit));
            }
        }

        private System.Collections.IEnumerator ClearAttackStatus(MonoBehaviour unit)
        {
            yield return new WaitForSeconds(3f); // Clear "under attack" status after 3 seconds
            unitsUnderAttack.Remove(unit);
            UpdateFilters();
        }

        private Vector2 WorldToMapPosition(Vector3 worldPosition)
        {
            // Convert world position to map coordinates
            Vector2 mapPosition = new Vector2(
                worldPosition.x * mapScale,
                worldPosition.z * mapScale
            );

            return mapPosition;
        }

        private Vector2 MapToWorldPosition(Vector2 mapPosition)
        {
            // Convert map position to world coordinates
            Vector2 worldPosition = new Vector2(
                mapPosition.x / mapScale,
                mapPosition.y / mapScale
            );

            return worldPosition;
        }

        private void DrawSynergyLine(Vector2 start, Vector2 end, Color color)
        {
            // Create temporary line renderer for synergy visualization
            GameObject lineObj = new GameObject("SynergyLine");
            lineObj.transform.SetParent(mapRect);

            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = line.endColor = color;
            line.startWidth = line.endWidth = 2f;
            line.positionCount = 2;
            line.SetPosition(0, new Vector3(start.x, start.y, 0));
            line.SetPosition(1, new Vector3(end.x, end.y, 0));

            // Destroy line after brief display
            Destroy(lineObj, updateInterval);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!eventData.IsPointerOverGameObject()) return;

            isDragging = true;
            dragStartPosition = eventData.position;
            selectionBox.gameObject.SetActive(true);
            selectionBox.position = dragStartPosition;
            selectionBox.sizeDelta = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            Vector2 dragCurrentPosition = eventData.position;
            Vector2 selectionSize = dragCurrentPosition - dragStartPosition;

            selectionBox.sizeDelta = new Vector2(Mathf.Abs(selectionSize.x), Mathf.Abs(selectionSize.y));
            selectionBox.position = dragStartPosition + selectionSize * 0.5f;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            isDragging = false;
            selectionBox.gameObject.SetActive(false);

            // Convert screen coordinates to world coordinates
            Vector2 min = Vector2.Min(dragStartPosition, eventData.position);
            Vector2 max = Vector2.Max(dragStartPosition, eventData.position);

            // Select units within the rectangle
            SelectUnitsInRect(min, max);
        }

        private void SelectUnitsInRect(Vector2 min, Vector2 max)
        {
            if (!Input.GetKey(KeyCode.LeftShift))
            {
                ClearSelection();
            }

            var units = FindObjectsOfType<Unit>();
            foreach (var unit in units)
            {
                Vector2 screenPos = Camera.main.WorldToScreenPoint(unit.transform.position);
                if (screenPos.x >= min.x && screenPos.x <= max.x &&
                    screenPos.y >= min.y && screenPos.y <= max.y)
                {
                    SelectUnit(unit);
                }
            }
        }

        private System.Collections.IEnumerator UpdateVisionRoutine()
        {
            while (true)
            {
                UpdateEnemyDetection();
                yield return new WaitForSeconds(visionUpdateInterval);
            }
        }

        private void UpdateEnemyDetection()
        {
            // Get all vision providers (units with detection capability)
            var visionProviders = FindObjectsOfType<VisionProvider>();
            var newlyDetectedEnemies = new HashSet<EnemyUnit>();

            // Check each vision provider's detection range
            foreach (var provider in visionProviders)
            {
                var detectedUnits = provider.GetDetectedUnits();
                foreach (var enemy in detectedUnits)
                {
                    newlyDetectedEnemies.Add(enemy);
                    if (!detectedEnemies.Contains(enemy))
                    {
                        CreateEnemyIndicator(enemy);
                    }
                }
            }

            // Remove indicators for enemies no longer detected
            var undetectedEnemies = detectedEnemies.Except(newlyDetectedEnemies).ToList();
            foreach (var enemy in undetectedEnemies)
            {
                RemoveEnemyIndicator(enemy);
            }

            detectedEnemies = newlyDetectedEnemies;
            UpdateEnemyIndicators();
        }

        private void CreateEnemyIndicator(EnemyUnit enemy)
        {
            if (enemyIndicatorPrefab == null || enemy == null) return;

            GameObject indicator = Instantiate(enemyIndicatorPrefab, transform);
            enemyIndicators[enemy] = indicator;

            var enemyIndicator = indicator.GetComponent<EnemyMapIndicator>();
            if (enemyIndicator != null)
            {
                enemyIndicator.Initialize(enemy);
                UpdateEnemyIndicatorPosition(enemy, indicator);
            }
        }

        private void RemoveEnemyIndicator(EnemyUnit enemy)
        {
            if (enemyIndicators.TryGetValue(enemy, out GameObject indicator))
            {
                Destroy(indicator);
                enemyIndicators.Remove(enemy);
            }
        }

        private void UpdateEnemyIndicators()
        {
            foreach (var pair in enemyIndicators)
            {
                var enemy = pair.Key;
                var indicator = pair.Value;

                if (enemy == null || indicator == null)
                {
                    continue;
                }

                UpdateEnemyIndicatorPosition(enemy, indicator);
                UpdateEnemyIndicatorVisibility(enemy, indicator);
            }
        }

        private void UpdateEnemyIndicatorPosition(EnemyUnit enemy, GameObject indicator)
        {
            Vector3 screenPos = tacticalCamera.WorldToScreenPoint(enemy.transform.position);
            indicator.transform.position = screenPos;
        }

        private void UpdateEnemyIndicatorVisibility(EnemyUnit enemy, GameObject indicator)
        {
            var enemyIndicator = indicator.GetComponent<EnemyMapIndicator>();
            if (enemyIndicator != null)
            {
                enemyIndicator.UpdateVisibility(enemy.IsStealthed);
            }
        }

        private void AssignSelectedUnitsToGroup(int groupIndex)
        {
            if (selectedUnits.Count == 0) return;

            // Clear existing group numbers for these units
            foreach (var unit in selectedUnits)
            {
                RemoveUnitFromAllGroups(unit);
            }

            // Assign to new group
            unitGroups[groupIndex] = new HashSet<MonoBehaviour>(selectedUnits);

            // Update visual indicators
            foreach (var unit in selectedUnits)
            {
                CreateOrUpdateGroupNumber(unit, groupIndex);
            }

            SaveGroupCustomization();
        }

        private void SelectUnitGroup(int groupIndex)
        {
            if (!unitGroups.ContainsKey(groupIndex)) return;

            if (!Input.GetKey(KeyCode.LeftShift))
            {
                ClearSelection();
            }

            foreach (var unit in unitGroups[groupIndex])
            {
                if (unit != null)
                {
                    SelectUnit(unit);
                }
            }

            UpdateCommandPanel();
        }

        private void RemoveUnitFromAllGroups(MonoBehaviour unit)
        {
            foreach (var group in unitGroups.Values)
            {
                group.Remove(unit);
            }

            if (groupNumbers.TryGetValue(unit, out var numberText))
            {
                Destroy(numberText.gameObject);
                groupNumbers.Remove(unit);
            }
        }

        private void CreateOrUpdateGroupNumber(MonoBehaviour unit, int groupIndex)
        {
            if (!groupNumbers.TryGetValue(unit, out var numberText))
            {
                var numberObj = Instantiate(groupNumberPrefab, transform);
                numberText = numberObj.GetComponent<TextMeshProUGUI>();
                groupNumbers[unit] = numberText;
            }

            numberText.text = groupIndex.ToString();
            numberText.color = groupNumberColor;
            UpdateGroupNumberPosition(unit, numberText);
        }

        private void UpdateGroupNumberPosition(MonoBehaviour unit, TextMeshProUGUI numberText)
        {
            if (unit == null || numberText == null) return;

            Vector3 screenPos = tacticalCamera.WorldToScreenPoint(unit.transform.position);
            screenPos += new Vector3(-10f, 10f, 0); // Offset to top-left corner
            numberText.transform.position = screenPos;
        }

        private System.Collections.IEnumerator UpdateStatsRoutine()
        {
            while (true)
            {
                UpdateAllUnitStats();
                yield return new WaitForSeconds(statsUpdateInterval);
            }
        }

        private void UpdateAllUnitStats()
        {
            // Update stats for selected units
            foreach (var unit in selectedUnits)
            {
                UpdateUnitStats(unit);
            }

            // Remove stats overlays for deselected units
            var unitsToRemove = statsOverlays.Keys.Except(selectedUnits).ToList();
            foreach (var unit in unitsToRemove)
            {
                RemoveStatsOverlay(unit);
            }

            // Update group number positions
            foreach (var pair in groupNumbers)
            {
                UpdateGroupNumberPosition(pair.Key, pair.Value);
            }
        }

        private void UpdateUnitStats(MonoBehaviour unit)
        {
            if (unit == null) return;

            GameObject overlay;
            if (!statsOverlays.TryGetValue(unit, out overlay))
            {
                overlay = CreateStatsOverlay(unit);
                statsOverlays[unit] = overlay;
            }

            var statsUI = overlay.GetComponent<UnitStatsUI>();
            if (statsUI != null)
            {
                if (unit is Artillery artillery)
                {
                    UpdateArtilleryStats(artillery, statsUI);
                }
                else if (unit is HeavyDefenseBunker bunker)
                {
                    UpdateBunkerStats(bunker, statsUI);
                }

                // Update position
                Vector3 screenPos = tacticalCamera.WorldToScreenPoint(unit.transform.position);
                overlay.transform.position = screenPos + (Vector3)statsOffset;
            }
        }

        private GameObject CreateStatsOverlay(MonoBehaviour unit)
        {
            GameObject overlay = Instantiate(statsOverlayPrefab, transform);
            var statsUI = overlay.GetComponent<UnitStatsUI>();
            if (statsUI != null)
            {
                statsUI.Initialize(healthBarColor, ammoBarColor);
            }
            return overlay;
        }

        private void UpdateArtilleryStats(Artillery artillery, UnitStatsUI statsUI)
        {
            statsUI.UpdateStats(
                artillery.CurrentHealth / artillery.MaxHealth,
                artillery.CurrentAmmo / artillery.MaxAmmo,
                artillery.IsDeployed ? "Deployed" : "Mobile",
                artillery.GetStatusEffects()
            );
        }

        private void UpdateBunkerStats(HeavyDefenseBunker bunker, UnitStatsUI statsUI)
        {
            statsUI.UpdateStats(
                bunker.CurrentHealth / bunker.MaxHealth,
                bunker.HasGarrison ? 1f : 0f,
                bunker.IsInFortifyMode ? "Fortified" : "Normal",
                bunker.GetStatusEffects()
            );
        }

        private void RemoveStatsOverlay(MonoBehaviour unit)
        {
            if (statsOverlays.TryGetValue(unit, out GameObject overlay))
            {
                Destroy(overlay);
                statsOverlays.Remove(unit);
            }
        }

        private System.Collections.IEnumerator UpdateEnemyInfoRoutine()
        {
            while (true)
            {
                UpdateEnemyInformation();
                yield return new WaitForSeconds(enemyInfoUpdateInterval);
            }
        }

        private void UpdateEnemyInformation()
        {
            foreach (var enemy in detectedEnemies)
            {
                if (enemy == null) continue;

                // Check if enemy is in range of any friendly unit
                bool inRange = false;
                foreach (var unit in selectedUnits)
                {
                    if (Vector3.Distance(unit.transform.position, enemy.transform.position) <= enemyInfoDisplayRange)
                    {
                        inRange = true;
                        break;
                    }
                }

                if (inRange)
                {
                    UpdateOrCreateEnemyInfoPanel(enemy);
                }
                else
                {
                    RemoveEnemyInfoPanel(enemy);
                }
            }

            // Clean up panels for undetected enemies
            var panelsToRemove = enemyInfoPanels.Keys.Where(e => !detectedEnemies.Contains(e)).ToList();
            foreach (var enemy in panelsToRemove)
            {
                RemoveEnemyInfoPanel(enemy);
            }
        }

        private void UpdateOrCreateEnemyInfoPanel(EnemyUnit enemy)
        {
            GameObject panel;
            if (!enemyInfoPanels.TryGetValue(enemy, out panel))
            {
                panel = CreateEnemyInfoPanel(enemy);
                enemyInfoPanels[enemy] = panel;
            }

            var infoUI = panel.GetComponent<EnemyInfoUI>();
            if (infoUI != null)
            {
                UpdateEnemyInfo(enemy, infoUI);
                PositionEnemyInfoPanel(enemy, panel);
            }
        }

        private GameObject CreateEnemyInfoPanel(EnemyUnit enemy)
        {
            GameObject panel = Instantiate(enemyInfoPanelPrefab, transform);
            var infoUI = panel.GetComponent<EnemyInfoUI>();
            if (infoUI != null)
            {
                infoUI.Initialize(enemyHealthBarColor, enemyAmmoBarColor);
            }
            return panel;
        }

        private void UpdateEnemyInfo(EnemyUnit enemy, EnemyInfoUI infoUI)
        {
            infoUI.UpdateInfo(
                enemy.UnitType,
                enemy.CurrentHealth / enemy.MaxHealth,
                enemy.GetAmmoPercent(),
                enemy.IsStealthed,
                enemy.GetActiveBuffs(),
                enemy.GetActiveDebuffs()
            );
        }

        private void PositionEnemyInfoPanel(EnemyUnit enemy, GameObject panel)
        {
            Vector3 screenPos = tacticalCamera.WorldToScreenPoint(enemy.transform.position);
            screenPos += new Vector3(0, 50f, 0); // Offset above unit
            panel.transform.position = screenPos;
        }

        private void RemoveEnemyInfoPanel(EnemyUnit enemy)
        {
            if (enemyInfoPanels.TryGetValue(enemy, out GameObject panel))
            {
                Destroy(panel);
                enemyInfoPanels.Remove(enemy);
            }
        }

        private void UpdateMinimap()
        {
            if (minimapCamera == null) return;

            // Update minimap camera position to follow main camera
            Vector3 mainCamPos = tacticalCamera.transform.position;
            minimapCamera.transform.position = new Vector3(mainCamPos.x, 100f, mainCamPos.z);

            // Update unit icons on minimap
            UpdateMinimapIcons();
        }

        private void UpdateMinimapIcons()
        {
            // Update friendly unit icons
            foreach (var pair in artilleryIndicators)
            {
                UpdateMinimapIcon(pair.Key, friendlyUnitColor);
            }
            foreach (var pair in bunkerIndicators)
            {
                UpdateMinimapIcon(pair.Key, friendlyUnitColor);
            }

            // Update enemy unit icons
            foreach (var enemy in detectedEnemies)
            {
                UpdateMinimapIcon(enemy, enemyUnitColor);
            }

            // Highlight selected units
            foreach (var unit in selectedUnits)
            {
                if (minimapIcons.TryGetValue(unit, out GameObject icon))
                {
                    icon.GetComponent<Image>().color = selectedUnitColor;
                }
            }
        }

        private void UpdateMinimapIcon(MonoBehaviour unit, Color color)
        {
            if (unit == null) return;

            GameObject icon;
            if (!minimapIcons.TryGetValue(unit, out icon))
            {
                icon = CreateMinimapIcon(unit);
                minimapIcons[unit] = icon;
            }

            // Update position
            Vector3 worldPos = unit.transform.position;
            Vector2 minimapPos = WorldToMinimapPosition(worldPos);
            icon.transform.position = minimapPos;

            // Update color if not selected
            if (!selectedUnits.Contains(unit))
            {
                icon.GetComponent<Image>().color = color;
            }
        }

        private GameObject CreateMinimapIcon(MonoBehaviour unit)
        {
            GameObject icon = new GameObject("MinimapIcon");
            icon.transform.SetParent(minimapRect, false);

            Image image = icon.AddComponent<Image>();
            image.rectTransform.sizeDelta = new Vector2(4f, 4f);

            return icon;
        }

        private Vector2 WorldToMinimapPosition(Vector3 worldPos)
        {
            float minimapRatio = minimapSize.x / minimapSize.y;
            float worldX = (worldPos.x + 50f) / 100f; // Assuming 100x100 world size
            float worldZ = (worldPos.z + 50f) / 100f;

            return new Vector2(
                minimapRect.position.x + worldX * minimapSize.x,
                minimapRect.position.y + worldZ * minimapSize.y
            );
        }

        private void AssignGroupCustomization(int groupIndex, Sprite icon, Color color, string role)
        {
            if (!groupData.ContainsKey(groupIndex))
            {
                groupData[groupIndex] = new GroupData
                {
                    groupIndex = groupIndex,
                    units = unitGroups[groupIndex]
                };
            }

            var data = groupData[groupIndex];
            data.icon = icon;
            data.color = color;
            data.role = role;

            // Update visual elements for all units in group
            foreach (var unit in data.units)
            {
                UpdateGroupMarker(unit, data);
            }

            SaveGroupCustomization();
        }

        private void UpdateGroupMarker(MonoBehaviour unit, GroupData data)
        {
            if (unit == null) return;

            GameObject marker;
            if (!groupMarkers.TryGetValue(unit, out marker))
            {
                marker = CreateGroupMarker(unit);
                groupMarkers[unit] = marker;
            }

            var markerUI = marker.GetComponent<GroupMarkerUI>();
            if (markerUI != null)
            {
                markerUI.UpdateMarker(data.icon, data.color, data.role);
            }
        }

        private GameObject CreateGroupMarker(MonoBehaviour unit)
        {
            GameObject marker = Instantiate(groupMarkerPrefab, transform);
            marker.transform.position = GetUnitScreenPosition(unit);
            return marker;
        }

        private void UpdateGroupWaypoints(int groupIndex)
        {
            if (!groupData.ContainsKey(groupIndex)) return;

            var data = groupData[groupIndex];
            if (data.waypoints.Count < 2) return;

            // Create or update waypoint line
            LineRenderer line = GetWaypointLine(groupIndex);
            line.positionCount = data.waypoints.Count;
            line.SetPositions(data.waypoints.ToArray());
        }

        private LineRenderer GetWaypointLine(int groupIndex)
        {
            while (waypointLines.Count <= groupIndex)
            {
                GameObject lineObj = new GameObject($"WaypointLine_{waypointLines.Count}");
                lineObj.transform.SetParent(transform);
                LineRenderer line = lineObj.AddComponent<LineRenderer>();
                line.startWidth = waypointLineWidth;
                line.endWidth = waypointLineWidth;
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = waypointLineColor;
                line.endColor = waypointLineColor;
                waypointLines.Add(line);
            }
            return waypointLines[groupIndex];
        }

        public void AddWaypoint(int groupIndex, Vector3 position)
        {
            if (!groupData.ContainsKey(groupIndex)) return;

            var data = groupData[groupIndex];
            data.waypoints.Add(position);
            UpdateGroupWaypoints(groupIndex);
        }

        public void ClearWaypoints(int groupIndex)
        {
            if (!groupData.ContainsKey(groupIndex)) return;

            var data = groupData[groupIndex];
            data.waypoints.Clear();
            if (groupIndex < waypointLines.Count)
            {
                waypointLines[groupIndex].positionCount = 0;
            }
        }

        public void SaveFilterPreset(string name, List<FilterCondition> conditions)
        {
            var preset = new FilterPreset
            {
                name = name,
                conditions = conditions,
                isActive = false
            };

            filterPresets.Add(preset);
            SaveFilterPresets();
            UpdateFilterPresetUI();
        }

        public void ApplyFilter(FilterPreset preset)
        {
            var filteredUnits = new HashSet<MonoBehaviour>();

            foreach (var pair in artilleryIndicators)
            {
                if (pair.Key != null && CheckFilterConditions(pair.Key, preset.conditions))
                {
                    filteredUnits.Add(pair.Key);
                }
            }

            foreach (var pair in bunkerIndicators)
            {
                if (pair.Key != null && CheckFilterConditions(pair.Key, preset.conditions))
                {
                    filteredUnits.Add(pair.Key);
                }
            }

            // Update selection
            ClearSelection();
            foreach (var unit in filteredUnits)
            {
                SelectUnit(unit);
            }
            UpdateCommandPanel();
        }

        private bool CheckFilterConditions(MonoBehaviour unit, List<FilterCondition> conditions)
        {
            foreach (var condition in conditions)
            {
                if (!CheckSingleCondition(unit, condition))
                {
                    return false;
                }
            }
            return true;
        }

        private bool CheckSingleCondition(MonoBehaviour unit, FilterCondition condition)
        {
            switch (condition.type)
            {
                case FilterCondition.FilterType.UnitType:
                    return CheckUnitType(unit, condition);
                case FilterCondition.FilterType.Status:
                    return CheckUnitStatus(unit, condition);
                case FilterCondition.FilterType.Health:
                    return CheckUnitHealth(unit, condition);
                case FilterCondition.FilterType.Ammo:
                    return CheckUnitAmmo(unit, condition);
                case FilterCondition.FilterType.Role:
                    return CheckUnitRole(unit, condition);
                case FilterCondition.FilterType.Distance:
                    return CheckUnitDistance(unit, condition);
                case FilterCondition.FilterType.Speed:
                    return CheckUnitSpeed(unit, condition);
                case FilterCondition.FilterType.Morale:
                    return CheckUnitMorale(unit, condition);
                case FilterCondition.FilterType.Fatigue:
                    return CheckUnitFatigue(unit, condition);
                default:
                    return true;
            }
        }

        private bool CheckUnitType(MonoBehaviour unit, FilterCondition condition)
        {
            string unitType = unit.GetType().Name;
            return condition.op == FilterCondition.Operator.Equals ? 
                   unitType == condition.value :
                   unitType != condition.value;
        }

        private bool CheckUnitStatus(MonoBehaviour unit, FilterCondition condition)
        {
            if (unit is Artillery artillery)
            {
                bool isDeployed = artillery.IsDeployed;
                return condition.op == FilterCondition.Operator.Equals ?
                       isDeployed.ToString() == condition.value :
                       isDeployed.ToString() != condition.value;
            }
            else if (unit is HeavyDefenseBunker bunker)
            {
                bool isFortified = bunker.IsInFortifyMode;
                return condition.op == FilterCondition.Operator.Equals ?
                       isFortified.ToString() == condition.value :
                       isFortified.ToString() != condition.value;
            }
            return false;
        }

        private bool CheckUnitHealth(MonoBehaviour unit, FilterCondition condition)
        {
            float healthPercent = 0f;
            if (unit is Artillery artillery)
            {
                healthPercent = artillery.CurrentHealth / artillery.MaxHealth;
            }
            else if (unit is HeavyDefenseBunker bunker)
            {
                healthPercent = bunker.CurrentHealth / bunker.MaxHealth;
            }

            switch (condition.op)
            {
                case FilterCondition.Operator.GreaterThan:
                    return healthPercent > condition.threshold;
                case FilterCondition.Operator.LessThan:
                    return healthPercent < condition.threshold;
                default:
                    return false;
            }
        }

        private bool CheckUnitAmmo(MonoBehaviour unit, FilterCondition condition)
        {
            if (unit is Artillery artillery)
            {
                float ammoPercent = artillery.CurrentAmmo / artillery.MaxAmmo;
                switch (condition.op)
                {
                    case FilterCondition.Operator.GreaterThan:
                        return ammoPercent > condition.threshold;
                    case FilterCondition.Operator.LessThan:
                        return ammoPercent < condition.threshold;
                    default:
                        return false;
                }
            }
            return true; // Non-artillery units always pass ammo check
        }

        private bool CheckUnitRole(MonoBehaviour unit, FilterCondition condition)
        {
            foreach (var group in groupData.Values)
            {
                if (group.units.Contains(unit))
                {
                    return condition.op == FilterCondition.Operator.Equals ?
                           group.role == condition.value :
                           group.role != condition.value;
                }
            }
            return false;
        }

        private bool CheckUnitDistance(MonoBehaviour unit, FilterCondition condition)
        {
            float distance = Vector3.Distance(unit.transform.position, condition.referencePoint);
            
            switch (condition.op)
            {
                case FilterCondition.Operator.Within:
                    return distance <= condition.radius;
                case FilterCondition.Operator.Outside:
                    return distance > condition.radius;
                case FilterCondition.Operator.GreaterThan:
                    return distance > condition.threshold;
                case FilterCondition.Operator.LessThan:
                    return distance < condition.threshold;
                default:
                    return false;
            }
        }

        private bool CheckUnitSpeed(MonoBehaviour unit, FilterCondition condition)
        {
            float speed = 0f;
            if (unit is Artillery artillery)
            {
                speed = artillery.CurrentSpeed;
            }
            else if (unit is HeavyDefenseBunker bunker)
            {
                speed = bunker.CurrentSpeed;
            }

            switch (condition.op)
            {
                case FilterCondition.Operator.Equals:
                    return Mathf.Approximately(speed, condition.threshold);
                case FilterCondition.Operator.GreaterThan:
                    return speed > condition.threshold;
                case FilterCondition.Operator.LessThan:
                    return speed < condition.threshold;
                default:
                    return false;
            }
        }

        private bool CheckUnitMorale(MonoBehaviour unit, FilterCondition condition)
        {
            float morale = 1f; // Default morale value
            if (unit is Artillery artillery)
            {
                morale = artillery.CurrentMorale;
            }
            else if (unit is HeavyDefenseBunker bunker)
            {
                morale = bunker.CurrentMorale;
            }

            switch (condition.op)
            {
                case FilterCondition.Operator.GreaterThan:
                    return morale > condition.threshold;
                case FilterCondition.Operator.LessThan:
                    return morale < condition.threshold;
                default:
                    return false;
            }
        }

        private bool CheckUnitFatigue(MonoBehaviour unit, FilterCondition condition)
        {
            float fatigue = 0f; // Default fatigue value
            if (unit is Artillery artillery)
            {
                fatigue = artillery.CurrentFatigue;
            }
            else if (unit is HeavyDefenseBunker bunker)
            {
                fatigue = bunker.CurrentFatigue;
            }

            switch (condition.op)
            {
                case FilterCondition.Operator.GreaterThan:
                    return fatigue > condition.threshold;
                case FilterCondition.Operator.LessThan:
                    return fatigue < condition.threshold;
                default:
                    return false;
            }
        }

        private void SaveGroupCustomization()
        {
            string json = JsonUtility.ToJson(groupData);
            PlayerPrefs.SetString("GroupCustomization", json);
            PlayerPrefs.Save();
        }

        private void SaveFilterPresets()
        {
            string json = JsonUtility.ToJson(filterPresets);
            PlayerPrefs.SetString("FilterPresets", json);
            PlayerPrefs.Save();
        }

        private Vector3 GetUnitScreenPosition(MonoBehaviour unit)
        {
            return tacticalCamera.WorldToScreenPoint(unit.transform.position);
        }

        private void OnDestroy()
        {
            if (mapRenderTexture != null)
            {
                mapRenderTexture.Release();
                Destroy(mapRenderTexture);
            }

            foreach (var overlay in statsOverlays.Values)
            {
                Destroy(overlay);
            }
            foreach (var numberText in groupNumbers.Values)
            {
                Destroy(numberText.gameObject);
            }

            if (minimapRenderTexture != null)
            {
                minimapRenderTexture.Release();
            }
            foreach (var panel in enemyInfoPanels.Values)
            {
                Destroy(panel);
            }
            foreach (var icon in minimapIcons.Values)
            {
                Destroy(icon);
            }
            foreach (var marker in groupMarkers.Values)
            {
                Destroy(marker);
            }
            foreach (var line in waypointLines)
            {
                Destroy(line.gameObject);
            }
        }

        [System.Serializable]
        public class FormationPreset
        {
            public string name;
            public FormationType type;
            public Vector2[] relativePositions;
            public float spacing = 2f;
            public bool adaptToTerrain = true;
        }

        public enum FormationType
        {
            Line,
            Column,
            Wedge,
            Square,
            Circle,
            Custom
        }

        [System.Serializable]
        public class GroupFormationData
        {
            public FormationType currentFormation;
            public Vector2[] customPositions;
            public float rotation;
            public bool maintainFormation = true;
        }

        public void SetGroupFormation(int groupIndex, FormationType formationType)
        {
            if (!groupData.ContainsKey(groupIndex)) return;

            var units = groupData[groupIndex].units;
            if (units.Count == 0) return;

            // Create or get formation data
            if (!groupFormations.ContainsKey(groupIndex))
            {
                groupFormations[groupIndex] = new GroupFormationData();
            }
            var formationData = groupFormations[groupIndex];
            formationData.currentFormation = formationType;

            // Get formation positions
            Vector2[] positions;
            switch (formationType)
            {
                case FormationType.Line:
                    positions = GenerateLineFormation(units.Count);
                    break;
                case FormationType.Column:
                    positions = GenerateColumnFormation(units.Count);
                    break;
                case FormationType.Wedge:
                    positions = GenerateWedgeFormation(units.Count);
                    break;
                case FormationType.Square:
                    positions = GenerateSquareFormation(units.Count);
                    break;
                case FormationType.Custom:
                    positions = formationData.customPositions ?? GenerateLineFormation(units.Count);
                    break;
                default:
                    positions = GenerateLineFormation(units.Count);
                    break;
            }

            // Apply formation
            Vector3 groupCenter = GetGroupCenter(units);
            int index = 0;
            foreach (var unit in units)
            {
                if (index >= positions.Length) break;
                
                Vector3 targetPos = groupCenter + new Vector3(
                    positions[index].x * Mathf.Cos(formationData.rotation) - positions[index].y * Mathf.Sin(formationData.rotation),
                    0,
                    positions[index].x * Mathf.Sin(formationData.rotation) + positions[index].y * Mathf.Cos(formationData.rotation)
                );

                // Set unit's target position through movement system
                if (unit is Artillery artillery)
                {
                    artillery.SetDestination(targetPos);
                }
                else if (unit is HeavyDefenseBunker bunker)
                {
                    bunker.SetPosition(targetPos);
                }

                index++;
            }

            UpdateFormationPreview(groupIndex);
        }

        private Vector3 GetGroupCenter(HashSet<MonoBehaviour> units)
        {
            if (units.Count == 0) return Vector3.zero;

            Vector3 sum = Vector3.zero;
            foreach (var unit in units)
            {
                sum += unit.transform.position;
            }
            return sum / units.Count;
        }

        private void UpdateFormationPreview(int groupIndex)
        {
            if (!groupFormations.ContainsKey(groupIndex)) return;

            var formationData = groupFormations[groupIndex];
            var units = groupData[groupIndex].units;

            if (formationPreview == null)
            {
                formationPreview = Instantiate(formationPreviewPrefab, transform);
            }

            // Update preview visualization
            var previewUI = formationPreview.GetComponent<FormationPreviewUI>();
            if (previewUI != null)
            {
                previewUI.UpdatePreview(
                    GetGroupCenter(units),
                    formationData.currentFormation,
                    formationData.rotation,
                    units.Count
                );
            }
        }

        private Vector2[] GenerateLineFormation(int unitCount)
        {
            Vector2[] positions = new Vector2[unitCount];
            float startX = -(unitCount - 1) * formationSpacing / 2f;
            for (int i = 0; i < unitCount; i++)
            {
                positions[i] = new Vector2(startX + i * formationSpacing, 0);
            }
            return positions;
        }

        private Vector2[] GenerateColumnFormation(int unitCount)
        {
            Vector2[] positions = new Vector2[unitCount];
            float startZ = -(unitCount - 1) * formationSpacing / 2f;
            for (int i = 0; i < unitCount; i++)
            {
                positions[i] = new Vector2(0, startZ + i * formationSpacing);
            }
            return positions;
        }

        private Vector2[] GenerateWedgeFormation(int unitCount)
        {
            Vector2[] positions = new Vector2[unitCount];
            int rows = Mathf.CeilToInt(Mathf.Sqrt(unitCount));
            int currentUnit = 0;

            for (int row = 0; row < rows && currentUnit < unitCount; row++)
            {
                int unitsInRow = Mathf.Min(row * 2 + 1, unitCount - currentUnit);
                float startX = -(unitsInRow - 1) * formationSpacing / 2f;
                float z = -row * formationSpacing;

                for (int i = 0; i < unitsInRow && currentUnit < unitCount; i++)
                {
                    positions[currentUnit] = new Vector2(startX + i * formationSpacing, z);
                    currentUnit++;
                }
            }
            return positions;
        }

        private Vector2[] GenerateSquareFormation(int unitCount)
        {
            Vector2[] positions = new Vector2[unitCount];
            int side = Mathf.CeilToInt(Mathf.Sqrt(unitCount));
            int currentUnit = 0;

            for (int row = 0; row < side && currentUnit < unitCount; row++)
            {
                for (int col = 0; col < side && currentUnit < unitCount; col++)
                {
                    float x = (col - (side - 1) / 2f) * formationSpacing;
                    float z = (row - (side - 1) / 2f) * formationSpacing;
                    positions[currentUnit] = new Vector2(x, z);
                    currentUnit++;
                }
            }
            return positions;
        }

        private void UpdateGroupStatus(int groupIndex)
        {
            if (!groupData.TryGetValue(groupIndex, out var group)) return;

            // Update existing group status
            // ...

            // Update tactical overlay
            tacticalOverlay.UpdateGroupStatus(groupIndex, group.units);
            
            // Update engagement visualization if enemies are in range
            var nearbyEnemies = FindNearbyEnemies(group.units);
            if (nearbyEnemies.Count > 0)
            {
                tacticalOverlay.UpdateGroupEngagement(groupIndex, group.units, nearbyEnemies);
            }
        }

        private HashSet<MonoBehaviour> FindNearbyEnemies(HashSet<MonoBehaviour> units)
        {
            HashSet<MonoBehaviour> enemies = new HashSet<MonoBehaviour>();
            float maxRange = 50f; // Maximum detection range

            foreach (var unit in units)
            {
                Collider[] colliders = Physics.OverlapSphere(unit.transform.position, maxRange);
                foreach (var collider in colliders)
                {
                    if (collider.TryGetComponent<EnemyUnit>(out var enemy))
                    {
                        enemies.Add(enemy);
                    }
                }
            }

            return enemies;
        }

        private void OnGroupEffectApplied(int groupIndex, GroupEffect effect)
        {
            if (tacticalOverlay != null)
            {
                tacticalOverlay.AddGroupEffect(groupIndex, effect.icon, effect.duration);
            }
        }

        private void OnGroupDestroyed(int groupIndex)
        {
            // ...
            // Clean up tactical overlay
            if (tacticalOverlay != null)
            {
                tacticalOverlay.DestroyGroupOverlay(groupIndex);
            }
        }

        private void UpdateFormation(int groupIndex)
        {
            if (!groupData.TryGetValue(groupIndex, out var group) || formationSystem == null) return;

            // Determine primary unit type for the group
            SpecializedFormation.UnitType primaryType = DeterminePrimaryUnitType(group.units);
            groupUnitTypes[groupIndex] = primaryType;

            // Generate and apply formation positions
            Vector3[] positions = formationSystem.GenerateFormationPositions(
                group.units,
                group.center,
                group.facing,
                primaryType
            );

            // Apply positions to units
            int index = 0;
            foreach (var unit in group.units)
            {
                if (index >= positions.Length) break;
                
                // Update unit's target position
                if (unit is Artillery artillery)
                {
                    artillery.SetDestination(positions[index]);
                }
                else if (unit is HeavyDefenseBunker bunker)
                {
                    bunker.SetPosition(positions[index]);
                }

                index++;
            }

            // Update combat predictions with terrain advantage
            if (tacticalOverlay != null && combatPredictor != null)
            {
                var nearbyEnemies = FindNearbyEnemies(group.units);
                if (nearbyEnemies.Count > 0)
                {
                    float terrainAdvantage = formationSystem.GetTerrainAdvantage(
                        group.center,
                        nearbyEnemies.First().transform.position
                    );
                    combatPredictor.UpdateCombatPredictions(groupIndex, group.units, nearbyEnemies, terrainAdvantage);
                }
            }
        }

        private SpecializedFormation.UnitType DeterminePrimaryUnitType(HashSet<MonoBehaviour> units)
        {
            int artilleryCount = 0;
            int defenseCount = 0;
            int supportCount = 0;

            foreach (var unit in units)
            {
                if (unit is Artillery)
                    artilleryCount++;
                else if (unit is HeavyDefenseBunker)
                    defenseCount++;
                else
                    supportCount++;
            }

            // Determine primary type based on majority
            if (artilleryCount >= defenseCount && artilleryCount >= supportCount)
                return SpecializedFormation.UnitType.Artillery;
            else if (defenseCount >= artilleryCount && defenseCount >= supportCount)
                return SpecializedFormation.UnitType.HeavyDefense;
            else if (supportCount > 0)
                return SpecializedFormation.UnitType.Support;
            
            return SpecializedFormation.UnitType.Mixed;
        }

        private void OnGroupMoved(int groupIndex, Vector3 newPosition)
        {
            // ...
            UpdateFormation(groupIndex);
        }

        private void OnGroupRotated(int groupIndex, Vector3 newFacing)
        {
            // ...
            UpdateFormation(groupIndex);
        }

        private void OnUnitsAdded(int groupIndex, HashSet<MonoBehaviour> newUnits)
        {
            // ...
            // Recalculate formation when units are added
            UpdateFormation(groupIndex);
        }

        private void OnUnitsRemoved(int groupIndex, HashSet<MonoBehaviour> removedUnits)
        {
            // ...
            // Recalculate formation when units are removed
            UpdateFormation(groupIndex);
        }

        private async void UpdateTacticalOverlay()
        {
            if (Time.time - lastTerrainUpdateTime >= terrainUpdateInterval)
            {
                lastTerrainUpdateTime = Time.time;
                await UpdateTerrainAnalysis();
            }
        }

        private async Task UpdateTerrainAnalysis()
        {
            if (selectedUnits == null || selectedUnits.Count == 0) return;

            foreach (var unit in selectedUnits)
            {
                if (unit == null) continue;

                Vector3 unitPos = unit.transform.position;
                TerrainAnalyzer.TerrainCell terrainInfo = await terrainAnalyzer.AnalyzeTerrainAtPosition(unitPos);

                if (terrainInfo != null)
                {
                    // Update unit movement speed based on terrain
                    if (unit.TryGetComponent<UnitMovement>(out var movement))
                    {
                        movement.SetMovementModifier(terrainInfo.movementModifier);
                    }

                    // Update unit combat stats based on terrain advantages
                    if (unit.TryGetComponent<UnitCombat>(out var combat))
                    {
                        float heightAdvantage = terrainInfo.isHighGround ? 1.25f : 1f;
                        float coverBonus = terrainInfo.providesCover ? 1.5f : 1f;
                        combat.SetTerrainBonuses(heightAdvantage, coverBonus);
                    }

                    // Update formation positioning if needed
                    if (specializedFormation != null)
                    {
                        specializedFormation.UpdatePositioning(terrainInfo);
                    }
                }
            }
        }
    }

    public class ArtilleryMapIndicator : MonoBehaviour
    {
        public Image deploymentIndicator;
        public Image ammoIndicator;

        private Image highlightEffect;

        private void Awake()
        {
            // Create highlight effect
            var highlightObj = new GameObject("Highlight");
            highlightObj.transform.SetParent(transform);
            highlightEffect = highlightObj.AddComponent<Image>();
            highlightEffect.sprite = Resources.Load<Sprite>("UI/GlowCircle");
            highlightEffect.color = new Color(1f, 1f, 1f, 0f);

            var rectTransform = highlightEffect.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.localScale = Vector3.one * 1.2f;
        }

        public void UpdateStatus(bool isDeployed, float ammoPercentage)
        {
            if (deploymentIndicator != null)
            {
                deploymentIndicator.color = isDeployed ?
                    new Color(0.2f, 1f, 0.2f, 1f) :
                    new Color(1f, 0.6f, 0.2f, 1f);
            }

            if (ammoIndicator != null)
            {
                ammoIndicator.fillAmount = ammoPercentage / 100f;
                ammoIndicator.color = ammoPercentage <= 30f ?
                    new Color(1f, 0.3f, 0.3f, 1f) :
                    new Color(0.8f, 0.8f, 0.2f, 1f);
            }
        }

        public void SetHighlight(bool isHighlighted)
        {
            if (highlightEffect != null)
            {
                highlightEffect.color = isHighlighted ?
                    new Color(1f, 1f, 1f, 0.3f) :
                    new Color(1f, 1f, 1f, 0f);
            }
        }

        public void SetSelectionHighlight(bool isSelected)
        {
            if (highlightEffect != null)
            {
                highlightEffect.color = isSelected ?
                    new Color(0f, 1f, 0f, 0.5f) :
                    new Color(1f, 1f, 1f, 0f);
            }
        }
    }

    public class EnemyMapIndicator : MonoBehaviour
    {
        public Image iconImage;
        public Image rangeIndicator;

        private EnemyUnit enemyUnit;
        private Color normalColor;
        private Color stealthColor;

        public void Initialize(EnemyUnit enemy)
        {
            enemyUnit = enemy;
            normalColor = GetComponent<TacticalMapUI>().enemyIndicatorColor;
            stealthColor = GetComponent<TacticalMapUI>().enemyStealthUnitColor;

            if (iconImage != null)
            {
                iconImage.sprite = enemy.UnitIcon;
            }
        }

        public void UpdateVisibility(bool isStealthed)
        {
            if (iconImage != null)
            {
                iconImage.color = isStealthed ? stealthColor : normalColor;
            }
        }
    }

    public class UnitStatsUI : MonoBehaviour
    {
        public Image healthBar;
        public Image ammoBar;
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI effectsText;

        public void Initialize(Color healthColor, Color ammoColor)
        {
            healthBar.color = healthColor;
            ammoBar.color = ammoColor;
        }

        public void UpdateStats(float healthPercent, float ammoPercent, string status, string[] effects)
        {
            healthBar.fillAmount = healthPercent;
            ammoBar.fillAmount = ammoPercent;
            statusText.text = status;
            effectsText.text = string.Join(", ", effects);
        }
    }

    public class EnemyInfoUI : MonoBehaviour
    {
        public Image healthBar;
        public Image ammoBar;
        public TextMeshProUGUI unitTypeText;
        public TextMeshProUGUI stealthStatusText;
        public TextMeshProUGUI buffsText;
        public TextMeshProUGUI debuffsText;

        public void Initialize(Color healthColor, Color ammoColor)
        {
            healthBar.color = healthColor;
            ammoBar.color = ammoColor;
        }

        public void UpdateInfo(
            string unitType,
            float healthPercent,
            float ammoPercent,
            bool isStealthed,
            string[] buffs,
            string[] debuffs)
        {
            unitTypeText.text = unitType;
            healthBar.fillAmount = healthPercent;
            ammoBar.fillAmount = ammoPercent;
            stealthStatusText.text = isStealthed ? "Stealthed" : "";
            buffsText.text = string.Join(", ", buffs);
            debuffsText.text = string.Join(", ", debuffs);
        }
    }

    [System.Serializable]
    public class FilterCriteria
    {
        public bool lowAmmo;
        public bool underAttack;
        public bool deployed;
        public bool fortifyMode;
        public bool hasGarrison;
        public bool synergiesActive;
    }

    [System.Serializable]
    public class CustomFilterConfig
    {
        public string name;
        public FilterCriteria criteria;
    }

    [System.Serializable]
    public class CustomFiltersData
    {
        public List<CustomFilterConfig> filters;
    }

    [System.Serializable]
    public class GroupData
    {
        public int groupIndex;
        public Sprite icon;
        public Color color;
        public string role;
        public List<Vector3> waypoints = new List<Vector3>();
        public HashSet<MonoBehaviour> units = new HashSet<MonoBehaviour>();
    }

    [System.Serializable]
    public class FilterPreset
    {
        public string name;
        public List<FilterCondition> conditions = new List<FilterCondition>();
        public bool isActive;
    }

    [System.Serializable]
    public class FilterCondition
    {
        public enum FilterType
        {
            UnitType,
            Status,
            Health,
            Ammo,
            Role,
            Distance,
            Speed,
            Morale,
            Fatigue,
            Custom
        }

        public enum Operator
        {
            Equals,
            NotEquals,
            GreaterThan,
            LessThan,
            Contains,
            Within,
            Outside
        }

        public FilterType type;
        public Operator op;
        public string value;
        public float threshold;
        public Vector3 referencePoint;
        public float radius;
    }
}
