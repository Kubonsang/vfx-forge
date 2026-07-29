using Kubonsang.VfxForge;
using UnityEngine;
using UnityEngine.VFX;

namespace VfxForge.Dogfood
{
    public sealed class SwordSlashDemoController : MonoBehaviour
    {
        [SerializeField] private GameObject slashPrefab;
        [SerializeField, Min(0.1f)] private float spawnInterval = 0.8f;
        [SerializeField] private bool autoSpawn = true;

        private float nextSpawnTime;

        public void Configure(
            GameObject prefab,
            float interval,
            bool shouldAutoSpawn)
        {
            slashPrefab = prefab;
            spawnInterval = Mathf.Max(0.1f, interval);
            autoSpawn = shouldAutoSpawn;
        }

        private void Start()
        {
            Spawn();
            nextSpawnTime = Time.time + spawnInterval;
        }

        private void Update()
        {
            if (WasSpawnPressed())
            {
                Spawn();
                nextSpawnTime = Time.time + spawnInterval;
            }

            if (autoSpawn && Time.time >= nextSpawnTime)
            {
                Spawn();
                nextSpawnTime = Time.time + spawnInterval;
            }
        }

        private static bool WasSpawnPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return UnityEngine.InputSystem.Keyboard.current?.spaceKey
                .wasPressedThisFrame == true;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
        }

        private void Spawn()
        {
            if (slashPrefab == null)
            {
                return;
            }

            GameObject instance = Instantiate(
                slashPrefab,
                Vector3.zero,
                Quaternion.identity);
            instance.name = "Basic Sword Slash (Demo)";

            foreach (VisualEffect effect in
                instance.GetComponentsInChildren<VisualEffect>(true))
            {
                effect.gameObject.SetActive(true);
                effect.enabled = true;
            }

            VfxPlayer player =
                instance.GetComponent<VfxPlayer>()
                ?? instance.AddComponent<VfxPlayer>();
            player.Configure("OnPlay");

            SwordSlashProjectile projectile =
                instance.GetComponent<SwordSlashProjectile>()
                ?? instance.AddComponent<SwordSlashProjectile>();
            projectile.Configure(14f, 0.45f);
        }

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                normal = { textColor = Color.white }
            };
            GUI.Label(
                new Rect(24f, 20f, 720f, 30f),
                "VFX Forge DF-001 — Basic Sword Slash",
                style);
            GUI.Label(
                new Rect(24f, 50f, 720f, 30f),
                "Play: auto fire every 0.8s  |  Space: fire now",
                style);
        }
    }
}
