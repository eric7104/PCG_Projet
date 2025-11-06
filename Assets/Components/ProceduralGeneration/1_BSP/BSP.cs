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
    [CreateAssetMenu(menuName = "Procedural Generation Method/BSP")]
    public class BSP : ProceduralGenerationMethod
    {
        [SerializeField] private int maxLeafCount = 6;
        [SerializeField] private int maxSplitDepth = 4;

        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            if (Grid == null) throw new InvalidOperationException("Grid is null");
            cancellationToken.ThrowIfCancellationRequested();

            var rootRect = new RectInt(0, 0, Grid.Width, Grid.Lenght);
            List<RectInt> roomRects;

            roomRects = await UniTask.Run(() =>
            {
                var rng = new System.Random();

                int RangeSafe(int minInclusive, int maxExclusive)
                {
                    if (maxExclusive <= minInclusive) return minInclusive;
                    return rng.Next(minInclusive, maxExclusive);
                }

                var root = new Node2(rootRect);
                root.SplitRecursiveThreadSafe(rng, maxSplitDepth, new Vector2Int(5, 5));

                var leaves = new List<Node2>();
                root.CollectLeaves(leaves);

                if (leaves.Count > maxLeafCount)
                    leaves = leaves.GetRange(0, maxLeafCount);

                var rooms = new List<RectInt>(leaves.Count);
                foreach (var leaf in leaves)
                {
                    int roomW = RangeSafe(5, Math.Max(5, leaf.Room.width - 1));
                    int roomH = RangeSafe(5, Math.Max(5, leaf.Room.height - 1));
                    int roomX = RangeSafe(leaf.Room.xMin, leaf.Room.xMax - roomW + 1);
                    int roomY = RangeSafe(leaf.Room.yMin, leaf.Room.yMax - roomH + 1);
                    rooms.Add(new RectInt(roomX, roomY, roomW, roomH));
                }

                return rooms;
            }, cancellationToken: cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            for (int i = 0; i < roomRects.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rect = roomRects[i];

                for (int x = rect.xMin; x < rect.xMin + rect.width; x++)
                {
                    for (int y = rect.yMin; y < rect.yMin + rect.height; y++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                            continue;

                        AddTileToCell(cell, ROOM_TILE_NAME, false);
                    }
                }
                if (i % 2 == 0)
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            for (int i = 1; i < roomRects.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var a = roomRects[i - 1];
                var b = roomRects[i];
                var aCenter = new Vector2Int(a.x + a.width / 2, a.y + a.height / 2);
                var bCenter = new Vector2Int(b.x + b.width / 2, b.y + b.height / 2);
                CreateDogLegCorridor(aCenter, bCenter);

                if (i % 2 == 0)
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            BuildGround();
            return;
        }

        private void BuildGround()
        {
            var groundTemplate = ScriptableObjectDatabase.GetScriptableObject<GridObjectTemplate>("Grass");

            for (int x = 0; x < Grid.Width; x++)
            {
                for (int z = 0; z < Grid.Lenght; z++)
                {
                    if (!Grid.TryGetCellByCoordinates(x, z, out var chosenCell))
                    {
                        Debug.LogError($"Unable to get cell on coordinates : ({x}, {z})");
                        continue;
                    }
                    GridGenerator.AddGridObjectToCell(chosenCell, groundTemplate, false);
                }
            }
        }

        private void CreateDogLegCorridor(Vector2Int start, Vector2Int end)
        {
            bool horizontalFirst = RandomService.Chance(0.5f);

            if (horizontalFirst)
            {
                CreateHorizontalCorridor(start.x, end.x, start.y);
                CreateVerticalCorridor(start.y, end.y, end.x);
            }
            else
            {
                CreateVerticalCorridor(start.y, end.y, start.x);
                CreateHorizontalCorridor(start.x, end.x, end.y);
            }
        }

        private void CreateHorizontalCorridor(int x1, int x2, int y)
        {
            int xMin = Mathf.Min(x1, x2);
            int xMax = Mathf.Max(x1, x2);

            for (int x = xMin; x <= xMax; x++)
            {
                if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                    continue;

                AddTileToCell(cell, CORRIDOR_TILE_NAME, true);
            }
        }

        private void CreateVerticalCorridor(int y1, int y2, int x)
        {
            int yMin = Mathf.Min(y1, y2);
            int yMax = Mathf.Max(y1, y2);

            for (int y = yMin; y <= yMax; y++)
            {
                if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                    continue;

                AddTileToCell(cell, CORRIDOR_TILE_NAME, true);
            }
        }
    }
}

public class Node2
{
    public Node2 child1, child2;
    public RectInt Room => _room;
    private RectInt _room;

    public Node2(RectInt room)
    {
        _room = room;
    }

    public void SplitRecursiveThreadSafe(System.Random rng, int maxDepth, Vector2Int minRoomSize, int currentDepth = 0)
    {
        if (currentDepth >= maxDepth) return;
        if (_room.width < minRoomSize.x * 2 && _room.height < minRoomSize.y * 2) return;

        bool horizontal = rng.NextDouble() < 0.5;

        int RangeSafe(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return rng.Next(minInclusive, maxExclusive);
        }

        if (horizontal && _room.height >= minRoomSize.y * 2)
        {
            int split = RangeSafe(minRoomSize.y, _room.height - minRoomSize.y);
            var top = new RectInt(_room.xMin, _room.yMin, _room.width, split);
            var bottom = new RectInt(_room.xMin, _room.yMin + split, _room.width, _room.height - split);
            child1 = new Node2(top);
            child2 = new Node2(bottom);
        }
        else if (!horizontal && _room.width >= minRoomSize.x * 2)
        {
            int split = RangeSafe(minRoomSize.x, _room.width - minRoomSize.x);
            var left = new RectInt(_room.xMin, _room.yMin, split, _room.height);
            var right = new RectInt(_room.xMin + split, _room.yMin, _room.width - split, _room.height);
            child1 = new Node2(left);
            child2 = new Node2(right);
        }
        else
        {
            return;
        }

        child1.SplitRecursiveThreadSafe(rng, maxDepth, minRoomSize, currentDepth + 1);
        child2.SplitRecursiveThreadSafe(rng, maxDepth, minRoomSize, currentDepth + 1);
    }
    
    public void CollectLeaves(List<Node2> outLeaves)
    {
        if (child1 == null && child2 == null)
        {
            outLeaves.Add(this);
            return;
        }

        child1?.CollectLeaves(outLeaves);
        child2?.CollectLeaves(outLeaves);
    }
}
