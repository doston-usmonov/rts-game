using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace RTS.UI
{
    public class FilterPanelUI : MonoBehaviour
    {
        [Header("UI References")]
        public Transform conditionContainer;
        public Button addConditionButton;
        public Button savePresetButton;
        public TMP_InputField presetNameInput;
        public Transform presetContainer;
        public GameObject conditionPrefab;
        public GameObject presetPrefab;

        [Header("Dropdown Options")]
        public List<string> filterTypeOptions = new List<string>
        {
            "Unit Type",
            "Status",
            "Health",
            "Ammo",
            "Role",
            "Custom"
        };

        public List<string> operatorOptions = new List<string>
        {
            "Equals",
            "Not Equals",
            "Greater Than",
            "Less Than",
            "Contains"
        };

        private TacticalMapUI tacticalMap;
        private List<FilterCondition> currentConditions = new List<FilterCondition>();
        private List<GameObject> conditionUIElements = new List<GameObject>();

        public void Initialize(TacticalMapUI map)
        {
            tacticalMap = map;
            addConditionButton.onClick.AddListener(AddNewCondition);
            savePresetButton.onClick.AddListener(SaveCurrentPreset);
            LoadSavedPresets();
        }

        private void AddNewCondition()
        {
            var condition = new FilterCondition();
            currentConditions.Add(condition);
            CreateConditionUI(condition);
        }

        private void SaveCurrentPreset()
        {
            if (string.IsNullOrEmpty(presetNameInput.text)) return;

            tacticalMap.SaveFilterPreset(presetNameInput.text, new List<FilterCondition>(currentConditions));
            presetNameInput.text = "";
            ClearCurrentConditions();
            LoadSavedPresets();
        }

        private void CreateConditionUI(FilterCondition condition)
        {
            GameObject conditionObj = Instantiate(conditionPrefab, conditionContainer);
            conditionUIElements.Add(conditionObj);

            var typeDropdown = conditionObj.transform.Find("TypeDropdown").GetComponent<TMP_Dropdown>();
            var operatorDropdown = conditionObj.transform.Find("OperatorDropdown").GetComponent<TMP_Dropdown>();
            var valueInput = conditionObj.transform.Find("ValueInput").GetComponent<TMP_InputField>();
            var removeButton = conditionObj.transform.Find("RemoveButton").GetComponent<Button>();

            // Setup dropdowns
            typeDropdown.ClearOptions();
            typeDropdown.AddOptions(filterTypeOptions);
            typeDropdown.onValueChanged.AddListener((index) => UpdateConditionType(condition, index));

            operatorDropdown.ClearOptions();
            operatorDropdown.AddOptions(operatorOptions);
            operatorDropdown.onValueChanged.AddListener((index) => UpdateConditionOperator(condition, index));

            // Setup value input
            valueInput.onValueChanged.AddListener((value) => UpdateConditionValue(condition, value));

            // Setup remove button
            removeButton.onClick.AddListener(() => RemoveCondition(condition, conditionObj));
        }

        private void UpdateConditionType(FilterCondition condition, int index)
        {
            condition.type = (FilterCondition.FilterType)index;
        }

        private void UpdateConditionOperator(FilterCondition condition, int index)
        {
            condition.op = (FilterCondition.Operator)index;
        }

        private void UpdateConditionValue(FilterCondition condition, string value)
        {
            condition.value = value;
            if (float.TryParse(value, out float threshold))
            {
                condition.threshold = threshold;
            }
        }

        private void RemoveCondition(FilterCondition condition, GameObject conditionObj)
        {
            currentConditions.Remove(condition);
            conditionUIElements.Remove(conditionObj);
            Destroy(conditionObj);
        }

        private void ClearCurrentConditions()
        {
            currentConditions.Clear();
            foreach (var element in conditionUIElements)
            {
                Destroy(element);
            }
            conditionUIElements.Clear();
        }

        private void LoadSavedPresets()
        {
            // Clear existing preset UI
            foreach (Transform child in presetContainer)
            {
                Destroy(child.gameObject);
            }

            // Load and create UI for each preset
            var presets = tacticalMap.GetFilterPresets();
            foreach (var preset in presets)
            {
                CreatePresetUI(preset);
            }
        }

        private void CreatePresetUI(FilterPreset preset)
        {
            GameObject presetObj = Instantiate(presetPrefab, presetContainer);
            
            var nameText = presetObj.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
            var applyButton = presetObj.transform.Find("ApplyButton").GetComponent<Button>();
            var deleteButton = presetObj.transform.Find("DeleteButton").GetComponent<Button>();

            nameText.text = preset.name;
            applyButton.onClick.AddListener(() => ApplyPreset(preset));
            deleteButton.onClick.AddListener(() => DeletePreset(preset, presetObj));
        }

        private void ApplyPreset(FilterPreset preset)
        {
            tacticalMap.ApplyFilter(preset);
        }

        private void DeletePreset(FilterPreset preset, GameObject presetObj)
        {
            tacticalMap.DeleteFilterPreset(preset);
            Destroy(presetObj);
        }
    }
}
