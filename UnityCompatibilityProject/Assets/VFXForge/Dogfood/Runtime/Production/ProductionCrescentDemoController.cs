using System.Collections.Generic;
using UnityEngine;

namespace VfxForge.Dogfood
{
    public sealed class ProductionCrescentDemoController : MonoBehaviour
    {
        [SerializeField] private GameObject slashPrefab;
        [SerializeField] private Vector3[] spawnPositions = System.Array.Empty<Vector3>();
        [SerializeField, Min(0.2f)] private float spawnInterval = 1.1f;

        private float nextSpawnTime;

        public void Configure(GameObject prefab, IEnumerable<Vector3> positions, float interval)
        {
            slashPrefab = prefab;
            spawnPositions = positions == null
                ? System.Array.Empty<Vector3>()
                : new List<Vector3>(positions).ToArray();
            spawnInterval = Mathf.Max(0.2f, interval);
        }

        private void Start()
        {
            SpawnWave();
            nextSpawnTime = Time.time + spawnInterval;
        }

        private void Update()
        {
            if (WasSpawnPressed() || Time.time >= nextSpawnTime)
            {
                SpawnWave();
                nextSpawnTime = Time.time + spawnInterval;
            }
        }

        private void SpawnWave()
        {
            if (slashPrefab == null)
            {
                return;
            }

            foreach (Vector3 position in spawnPositions)
            {
                GameObject instance = Instantiate(slashPrefab, position, Quaternion.identity);
                instance.name = "Production Crescent Slash (Demo)";
            }
        }

        private static bool WasSpawnPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return UnityEngine.InputSystem.Keyboard.current?.spaceKey.wasPressedThisFrame == true;
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
            GUI.Label(new Rect(24f, 20f, 850f, 30f),
                "VFX Forge VF-012 — Production Crescent Slash", style);
            GUI.Label(new Rect(24f, 50f, 850f, 30f),
                "Dark / mid / bright gameplay grounds | auto fire 1.1s | Space: fire now", style);
        }
    }
}
