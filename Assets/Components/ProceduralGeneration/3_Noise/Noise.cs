using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VTools.Grid;
using VTools.RandomService;
using VTools.ScriptableObjectDatabase;
using VTools.Utility;
using Grid = UnityEngine.Grid;

namespace Components.ProceduralGeneration.SimpleRoomPlacement
{
    [CreateAssetMenu(menuName = "Procedural Generation Method/Noise")]
    public class Noise : ProceduralGenerationMethod
    {

        private FastNoiseLite fastNoise = new FastNoiseLite();
        
        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            fastNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            float[,] noiseData = new float[128, 128];

            for (int x = 0; x < 128; x++)
            {
                for (int y = 0; y < 128; y++)
                {
                    noiseData[x, y] = fastNoise.GetNoise(x, y);
                }
            }
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            
        }
    }
}
