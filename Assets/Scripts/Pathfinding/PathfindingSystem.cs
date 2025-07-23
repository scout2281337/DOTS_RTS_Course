using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

partial struct PathfindingSystem : ISystem
{
    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;
    
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }

    private void FindPath(int2 startPosition, int2 endPosition) 
    {
        int2 gridSize = new int2(4, 4);

        NativeArray<PathNode> pathNodeArray = new NativeArray<PathNode>(gridSize.x *  gridSize.y, Allocator.Temp);

        for (int x = 0; x < gridSize.x; x++) 
        {
            for (int y = 0; y < gridSize.y; y++) 
            {
                PathNode pathNode = new PathNode();
                pathNode.NodePosition = new int2(x , y);

                pathNode.index = CalculateIndex(x , y, gridSize.x);
                pathNode.gCost = int.MaxValue;
                pathNode.hCost = CalculateDistanceCost(new int2 (x , y), endPosition);
                pathNode.fCost = pathNode.gCost + pathNode.hCost;

                pathNode.isWalkable = true;
                pathNode.cameFromNodeIndex = -1;

                pathNodeArray[pathNode.index] = pathNode;
            }
            
        }

        NativeArray<int2> neighbourOffsetArray = new NativeArray<int2>(new int2[] 
        {
            new int2 (-1, 0),
            new int2 (+1, 0),
            new int2 (0, +1),
            new int2 (0, -1),
            new int2 (-1, -1),
            new int2 (-1, +1),
            new int2 (+1, -1),
            new int2 (+1, +1),




        }, Allocator.Temp);
        int endNodeIndex = CalculateIndex(endPosition.x , endPosition.y, gridSize.x);

        
        
        PathNode startNode = pathNodeArray[CalculateIndex(startPosition.x, startPosition.y, gridSize.x)];
        startNode.gCost = 0;
        startNode.fCost = startNode.gCost + startNode.hCost;
        pathNodeArray[startNode.index] = startNode;

        NativeList<int> openList = new NativeList<int>(Allocator.Temp);
        NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

        while (openList.Length > 0) 
        {
            int currentNodeIndex = GetLowestCostFNodeIndex(openList, pathNodeArray);
            PathNode currentNode = pathNodeArray[currentNodeIndex];


            if (currentNodeIndex == endNodeIndex) 
            {
                //reached destination
                break;
            }

            //remove current node from open list
            for (int i = 0; i < openList.Length; i++) 
            {
                if (openList[i] == currentNodeIndex) 
                {
                    openList.RemoveAtSwapBack(i);
                    break;
                }
            }

            closedList.Add(currentNodeIndex);

            for ( int i = 0; i < neighbourOffsetArray.Length; i++) 
            {
                int2 neighbourOffset = neighbourOffsetArray[i];
                int2 neighbourPosition = new int2(currentNode.NodePosition.x + neighbourOffset.x, currentNode.NodePosition.y + neighbourOffset.y);



            }
        }

        pathNodeArray.Dispose();
        neighbourOffsetArray.Dispose();
        openList.Dispose();
        closedList.Dispose();
    }


    private int CalculateIndex(int x, int y, int gridWidth) 
    {
        return x + y * gridWidth;
    }

    private int CalculateDistanceCost(int2 aPosition, int2 bPosition) 
    {
        int xDistance = math.abs(aPosition.x - bPosition.x);
        int yDistance = math.abs(aPosition.y - bPosition.y);
        int remaining = math.abs(xDistance - yDistance);
        return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
    }

    private int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray) 
    {
        PathNode lowestCostPathNode = pathNodeArray[openList[0]];
        for (int i = 1; i < openList.Length; i++) 
        {
            PathNode testPathNode = pathNodeArray[openList[i]];
            if (testPathNode.fCost < lowestCostPathNode.fCost) 
            {
                lowestCostPathNode = testPathNode;
            }
        }
        return lowestCostPathNode.index;
    }

    private bool IsPositionInsideGrid(int2 gridPosition, int2 gridSize) 
    {
        return 
            gridPosition.x >= 0 &&
            gridPosition.y >= 0 &&
            gridPosition.x < gridSize.x &&
            gridPosition.y < gridSize.y;
    }
}
