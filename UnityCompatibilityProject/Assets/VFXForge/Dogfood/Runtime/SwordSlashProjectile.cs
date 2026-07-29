using Kubonsang.VfxForge;
using UnityEngine;

namespace VfxForge.Dogfood
{
    [DisallowMultipleComponent]
    public sealed class SwordSlashProjectile : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float speed = 14f;
        [SerializeField, Min(0.01f)] private float lifetime = 0.45f;

        private float elapsed;

        public float Speed => speed;
        public float Lifetime => lifetime;

        public void Configure(float newSpeed, float newLifetime)
        {
            speed = Mathf.Max(0f, newSpeed);
            lifetime = Mathf.Max(0.01f, newLifetime);
        }

        private void OnEnable()
        {
            elapsed = 0f;
            if (!Application.isPlaying)
            {
                return;
            }

            VfxPlayer player = GetComponent<VfxPlayer>();
            if (player != null)
            {
                player.CacheEffects();
                player.Play();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            transform.position += transform.forward * (speed * deltaTime);
            elapsed += deltaTime;
            if (elapsed >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
