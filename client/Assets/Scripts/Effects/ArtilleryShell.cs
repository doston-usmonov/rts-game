using UnityEngine;
using System;
using System.Collections;
using RTS.Core.Pooling;

namespace RTS.Effects
{
    public class ArtilleryShell : MonoBehaviour, IPoolable
    {
        public event Action<Vector3> OnExplode;

        private Vector3 startPosition;
        private Vector3 midPosition;
        private Vector3 targetPosition;
        private float timeOfFlight;
        private float elapsedTime;
        private float splashRadius;
        private float damage;
        private bool isActive;

        public void Initialize(Vector3 start, Vector3 mid, Vector3 target, float tof, float radius, float dmg)
        {
            startPosition = start;
            midPosition = mid;
            targetPosition = target;
            timeOfFlight = tof;
            splashRadius = radius;
            damage = dmg;
            elapsedTime = 0f;
            isActive = true;

            StartCoroutine(FlightRoutine());
        }

        private IEnumerator FlightRoutine()
        {
            while (elapsedTime < timeOfFlight && isActive)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / timeOfFlight;

                // Quadratic Bezier curve for smooth arc
                Vector3 a = Vector3.Lerp(startPosition, midPosition, t);
                Vector3 b = Vector3.Lerp(midPosition, targetPosition, t);
                transform.position = Vector3.Lerp(a, b, t);

                // Rotate shell to face direction of travel
                if (t < 1f)
                {
                    Vector3 nextPos = Vector3.Lerp(Vector3.Lerp(startPosition, midPosition, t + 0.01f),
                                                Vector3.Lerp(midPosition, targetPosition, t + 0.01f),
                                                t + 0.01f);
                    transform.rotation = Quaternion.LookRotation(nextPos - transform.position);
                }

                yield return null;
            }

            if (isActive)
            {
                OnExplode?.Invoke(targetPosition);
                ReturnToPool();
            }
        }

        public void OnSpawn()
        {
            gameObject.SetActive(true);
            isActive = true;
        }

        public void OnDespawn()
        {
            isActive = false;
            gameObject.SetActive(false);
        }

        public void ReturnToPool()
        {
            OnDespawn();
        }
    }
}
