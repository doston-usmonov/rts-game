using UnityEngine;
using System.Collections.Generic;
using RTS.Units;

namespace RTS.Vision
{
    public class VisionSystem : MonoBehaviour
    {
        private static VisionSystem instance;
        public static VisionSystem Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<VisionSystem>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("VisionSystem");
                        instance = go.AddComponent<VisionSystem>();
                    }
                }
                return instance;
            }
        }

        private Dictionary<Unit, float> unitVisionRanges = new Dictionary<Unit, float>();
        private List<Vector3> revealedPositions = new List<Vector3>();

        public void RegisterUnit(Unit unit, float visionRange)
        {
            if (!unitVisionRanges.ContainsKey(unit))
            {
                unitVisionRanges.Add(unit, visionRange);
            }
        }

        public void UnregisterUnit(Unit unit)
        {
            if (unitVisionRanges.ContainsKey(unit))
            {
                unitVisionRanges.Remove(unit);
            }
        }

        public bool IsPositionVisible(Vector3 position)
        {
            foreach (var kvp in unitVisionRanges)
            {
                if (kvp.Key == null) continue;
                
                float distance = Vector3.Distance(kvp.Key.transform.position, position);
                if (distance <= kvp.Value)
                {
                    return true;
                }
            }

            foreach (var revealedPos in revealedPositions)
            {
                if (Vector3.Distance(revealedPos, position) < 1f)
                {
                    return true;
                }
            }

            return false;
        }

        public void RevealPosition(Vector3 position)
        {
            if (!revealedPositions.Contains(position))
            {
                revealedPositions.Add(position);
            }
        }

        public void ClearRevealedPositions()
        {
            revealedPositions.Clear();
        }
    }
}
