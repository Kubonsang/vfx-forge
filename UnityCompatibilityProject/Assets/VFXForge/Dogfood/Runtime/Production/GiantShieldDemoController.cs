using UnityEngine;

namespace VfxForge.Dogfood
{
    public sealed class GiantShieldDemoController : MonoBehaviour
    {
        [SerializeField] private GameObject shieldPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField, Min(0.5f)] private float spawnInterval = 2.4f;

        private float nextSpawnTime;

        public void Configure(
            GameObject prefab,
            Transform origin,
            float interval)
        {
            shieldPrefab = prefab;
            spawnPoint = origin;
            spawnInterval = Mathf.Max(0.5f, interval);
        }

        private void Start()
        {
            SpawnShield();
            nextSpawnTime = Time.time + spawnInterval;
        }

        private void Update()
        {
            if (WasSpawnPressed() || Time.time >= nextSpawnTime)
            {
                SpawnShield();
                nextSpawnTime = Time.time + spawnInterval;
            }
        }

        private void SpawnShield()
        {
            if (shieldPrefab == null || spawnPoint == null)
            {
                return;
            }

            GameObject instance = Instantiate(
                shieldPrefab,
                spawnPoint.position,
                spawnPoint.rotation);
            instance.name = "Giant Shield Deployment (Demo)";
        }

        private static bool WasSpawnPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return UnityEngine.InputSystem.Keyboard.current
                ?.spaceKey.wasPressedThisFrame == true;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
        }

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                normal = { textColor = Color.white }
            };
            GUI.Label(
                new Rect(24f, 20f, 900f, 30f),
                "VFX Forge VF-013 — Giant Shield Deployment",
                style);
            GUI.Label(
                new Rect(24f, 50f, 900f, 30f),
                "Auto deploy 2.4s | Space: deploy now | VFX only",
                style);
        }
    }
}
