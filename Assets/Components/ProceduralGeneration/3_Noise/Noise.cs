using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Components.ProceduralGeneration._3_Noise
{
    [CreateAssetMenu(menuName = "Procedural Generation Method/Noise")]
    public class Noise : ProceduralGenerationMethod
    {
        [Header("Taille")]
        [SerializeField] private int largeur = 128;
        [SerializeField] private int longueur = 128;

        [Header("Noise")]
        [SerializeField] private int seed = 1234;
        [SerializeField, Range(0.0001f, 1f)] private float frequency = 0.02f;
        [SerializeField, Range(1, 10)] private int octaves = 4;
        [SerializeField, Range(1.5f, 3.0f)] private float lacunarity = 2.0f;
        [SerializeField, Range(0.2f, 0.9f)] private float gain = 0.5f;
        [SerializeField] private Vector2 offset = Vector2.zero;

        [Header("Hauteur des niveaux")]
        [SerializeField, Range(0f, 1f)] private float waterLevel = 0.35f;
        [SerializeField, Range(0f, 1f)] private float sandLevel = 0.45f;
        [SerializeField, Range(0f, 1f)] private float grassLevel = 0.70f;
        
        private FastNoiseLite _noise;

        private void SetupNoise()
        {
            var n = new FastNoiseLite(seed);
            n.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            n.SetFractalType(FastNoiseLite.FractalType.FBm);
            n.SetFractalOctaves(octaves);
            n.SetFractalLacunarity(lacunarity);
            n.SetFractalGain(gain);
            n.SetFrequency(frequency);
            _noise = n;
        }
        private float Sample01(float x, float y)
        {
            float v = _noise.GetNoise(x + offset.x, y + offset.y);
            return (v + 1f) * 0.5f;
        }

        private string PickTileName(float h)
        {
            if (h < waterLevel) return WATER_TILE_NAME;
            if (h < sandLevel)  return SAND_TILE_NAME;
            if (h < grassLevel) return GRASS_TILE_NAME;
            return ROCK_TILE_NAME;
        }

        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            SetupNoise();
            int width = Mathf.Clamp(largeur, 1, Grid.Width);
            int length = Mathf.Clamp(longueur, 1, Grid.Lenght);
            int work = 0;
            for (int y = 0; y < length; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    float h = Sample01(x, y);
                    string tileName = PickTileName(h);
                    if (Grid.TryGetCellByCoordinates(x, y, out var cell))
                    {
                        AddTileToCell(cell, tileName, true);
                    }
                    work++;
                    if (work % 1024 == 0)
                    {
                        await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                    }
                }
            }
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        private void OnValidate()
        {
            sandLevel  = Mathf.Max(sandLevel,  waterLevel);
            grassLevel = Mathf.Max(grassLevel, sandLevel);
            largeur = Mathf.Max(1, largeur);
            longueur = Mathf.Max(1, longueur);
            frequency = Mathf.Clamp(frequency, 0.0001f, 1f);
            octaves = Mathf.Clamp(octaves, 1, 12);
            lacunarity = Mathf.Clamp(lacunarity, 1.0f, 4.0f);
            gain = Mathf.Clamp(gain, 0.2f, 0.9f);
        }
    }
}
