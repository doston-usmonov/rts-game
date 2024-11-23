using UnityEngine;
using System.Collections.Generic;

namespace RTS.Effects
{
    public class ArtilleryShell : MonoBehaviour
    {
        public float rotationSpeed = 360f;
        public ParticleSystem trailEffect;
        public AudioSource whistleSound;
        public float lifetime = 5f;
        public float damage = 100f;
        public float splashRadius = 5f;
        public LayerMask damageLayer;

        private Vector3 targetPosition;
        private float startTime;
        private bool isActive = false;
        private static ObjectPool<ArtilleryShell> shellPool;
        private static ObjectPool<ParticleSystem> trailPool;
        private static ObjectPool<AudioSource> audioPool;

        private void Awake()
        {
            if (shellPool == null)
            {
                shellPool = new ObjectPool<ArtilleryShell>(CreateShellInstance, 10);
            }
            if (trailPool == null)
            {
                trailPool = new ObjectPool<ParticleSystem>(CreateTrailInstance, 10);
            }
            if (audioPool == null)
            {
                audioPool = new ObjectPool<AudioSource>(CreateAudioInstance, 5);
            }
        }

        public void Initialize(Vector3 target)
        {
            targetPosition = target;
            startTime = Time.time;
            isActive = true;
            gameObject.SetActive(true);

            // Get trail effect from pool
            var trail = trailPool.GetObject();
            trail.transform.parent = transform;
            trail.transform.localPosition = Vector3.zero;
            trail.Play();

            // Get audio from pool
            var audio = audioPool.GetObject();
            audio.transform.parent = transform;
            audio.transform.localPosition = Vector3.zero;
            audio.Play();
        }

        private void Update()
        {
            if (!isActive) return;

            // Rotate the shell for visual effect
            transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);

            // Check lifetime
            if (Time.time - startTime >= lifetime)
            {
                Impact();
            }
        }

        private void Impact()
        {
            // Apply damage
            Collider[] hits = Physics.OverlapSphere(transform.position, splashRadius, damageLayer);
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    float distance = Vector3.Distance(transform.position, hit.transform.position);
                    float damageMultiplier = 1f - (distance / splashRadius);
                    damageable.TakeDamage(damage * damageMultiplier);
                }
            }

            // Return to pool
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            isActive = false;
            gameObject.SetActive(false);
            shellPool.ReturnObject(this);

            // Return effects to their pools
            if (trailEffect != null)
            {
                trailEffect.Stop();
                trailPool.ReturnObject(trailEffect);
            }
            if (whistleSound != null)
            {
                whistleSound.Stop();
                audioPool.ReturnObject(whistleSound);
            }
        }

        private static ArtilleryShell CreateShellInstance()
        {
            var shell = Instantiate(Resources.Load<GameObject>("Prefabs/ArtilleryShell")).GetComponent<ArtilleryShell>();
            shell.gameObject.SetActive(false);
            return shell;
        }

        private static ParticleSystem CreateTrailInstance()
        {
            var trail = Instantiate(Resources.Load<ParticleSystem>("Prefabs/ShellTrail"));
            trail.gameObject.SetActive(false);
            return trail;
        }

        private static AudioSource CreateAudioInstance()
        {
            var audio = new GameObject("ShellAudio").AddComponent<AudioSource>();
            audio.clip = Resources.Load<AudioClip>("Sounds/ShellWhistle");
            audio.gameObject.SetActive(false);
            return audio;
        }

        public static ArtilleryShell Spawn(Vector3 position, Vector3 target)
        {
            var shell = shellPool.GetObject();
            shell.transform.position = position;
            shell.Initialize(target);
            return shell;
        }
    }
}
