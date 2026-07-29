using Kubonsang.VfxForge;
using UnityEngine;

namespace VfxForge.Dogfood
{
    public sealed class TopDownSwordSlashDemoController : MonoBehaviour
    {
        [SerializeField] private GameObject slashPrefab;
        [SerializeField] private Vector3 spawnPosition = new Vector3(0f, 0f, -3f);
        [SerializeField, Min(0.1f)] private float spawnInterval = 0.9f;

        private float nextSpawnTime;

        public void Configure(GameObject prefab, Vector3 position, float interval)
        {
            slashPrefab = prefab;
            spawnPosition = position;
            spawnInterval = Mathf.Max(0.1f, interval);
        }

        private void Start()
        {
            Spawn();
            nextSpawnTime = Time.time + spawnInterval;
        }

        private void Update()
        {
            if (WasSpawnPressed() || Time.time >= nextSpawnTime)
            {
                Spawn();
                nextSpawnTime = Time.time + spawnInterval;
            }
        }

        private void Spawn()
        {
            if (slashPrefab == null)
            {
                return;
            }

            GameObject instance = Instantiate(
                slashPrefab,
                spawnPosition,
                Quaternion.identity);
            instance.name = "Top Down Crescent Sword Slash (Demo)";

            VfxPlayer player = instance.GetComponent<VfxPlayer>();
            if (player != null)
            {
                player.CacheEffects();
                player.Play();
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

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                normal = { textColor = Color.white }
            };
            GUI.Label(
                new Rect(24f, 20f, 760f, 30f),
                "VFX Forge DF-002 — Top Down Crescent Sword Slash",
                style);
            GUI.Label(
                new Rect(24f, 50f, 760f, 30f),
                "Top-down orthographic view | auto fire 0.9s | Space: fire now",
                style);
        }
    }
}
