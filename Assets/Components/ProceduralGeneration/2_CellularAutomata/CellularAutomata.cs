using System.Collections.Generic;
using System.Threading;
using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;
using VTools.Grid;
using VTools.RandomService;
using VTools.ScriptableObjectDatabase;
using VTools.Utility;

[CreateAssetMenu(menuName = "Procedural Generation Method/CellularAutomata")]
public class CellularAutomata : ProceduralGenerationMethod
{

    [SerializeField] private int _noiseDensity = 50;
    private List<Cell> _cell = new List<Cell>();
    private List<Cell> _TempCell = new List<Cell>();
    [SerializeField] private int _NeighborThreshold = 4;
    
    protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
    {
        generategrid();
        for (int i = 0; i < _maxSteps; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            compareCells(); 
            await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
        }

        Debug.Log(_cell.Count);
    }
    
    public void generategrid()
    {
        _cell.Clear();
        for (int x = 0; x < Grid.Width; x++)
        {
            for (int y = 0; y < Grid.Lenght; y++)
            {
                if (!Grid.TryGetCellByCoordinates(x, y, out var cell)) 
                    continue;
                int randomValue = RandomService.Range(0, 100);
                if (randomValue < _noiseDensity)
                {
                    AddTileToCell(cell,WATER_TILE_NAME,false);
                    _cell.Add(cell);
                    
                }
                else
                {
                    AddTileToCell(cell, GRASS_TILE_NAME, false);
                }
            }
        }
    }
    
    public void GenerateTempGrid()
    {
        _TempCell.Clear();
        for (int x = 0; x < Grid.Width; x++)
        {
            for (int y = 0; y < Grid.Lenght; y++)
            {
                if (!Grid.TryGetCellByCoordinates(x, y, out var cell)) 
                    continue;
                int randomValue = RandomService.Range(0, 100);
                if (randomValue < _noiseDensity)
                {
                    AddTileToCell(cell,WATER_TILE_NAME,false);
                    _TempCell.Add(cell);
                    
                }
                else
                {
                    AddTileToCell(cell, GRASS_TILE_NAME, false);
                }
            }
        }
    }
    
    public void compareCells()
    {
        var newWaterCells = new List<Cell>();
           for (int x = 0; x < Grid.Width; x++)
        {
            for (int y = 0; y < Grid.Lenght; y++)
            {
                if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                    continue;
                int waterNeighbors = 0;
                int existingNeighbors = 0;

                for (int nx = -1; nx <= 1; nx++)
                {
                    for (int ny = -1; ny <= 1; ny++)
                    {
                        if (nx == 0 && ny == 0) continue;
                        int cx = x + nx;
                        int cy = y + ny;
                        if (Grid.TryGetCellByCoordinates(cx, cy, out var neighborCell))
                        {
                            existingNeighbors++;
                            if (_cell.Contains(neighborCell))
                                waterNeighbors++;
                        }
                    }
                }
                int grassNeighbors = existingNeighbors - waterNeighbors;
                if (grassNeighbors >= _NeighborThreshold)
                {
                    AddTileToCell(cell, GRASS_TILE_NAME, true);
                }
                else
                {
                    AddTileToCell(cell, WATER_TILE_NAME, true);
                     newWaterCells.Add(cell);
                }
            }
        }
        _cell = newWaterCells;
        _TempCell.Clear();
        _TempCell.AddRange(_cell);
    }
}
