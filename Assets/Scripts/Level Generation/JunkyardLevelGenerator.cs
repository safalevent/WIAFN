using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using WIAFN.LevelGeneration;
using Random = UnityEngine.Random;

[System.Serializable]
public struct LevelObjectData
{
    [HideInInspector]
    public string name;

    public GameObject levelObject;
    public float weight;
}

[RequireComponent(typeof(LevelMeshController))]
public class JunkyardLevelGenerator : LevelGeneratorBase
{
    private class GridGenerationOperation
    {
        public Task task;
        public CancellationTokenSource cancellationTokenSource;
        public GridGenerator generator;

        public GridGenerationOperation(Task task, GridGenerator generator, CancellationTokenSource ct)
        {
            this.task = task;
            this.generator = generator;
            this.cancellationTokenSource = ct;
        }
    }

    public Vector2Int resolution;

    public int itemCount;
    public List<LevelObjectData> levelObjects;

    private Grid _currentGrid;

    private float _weightSum;

    private Transform _itemsParent;
    private GridGenerationOperation _waitingForOperation;

    private ItemPoolGenerator _itemPoolGenerator;

    private new void Awake()
    {
        base.Awake();

        _weightSum = -1f;

        _itemsParent = (new GameObject("Items")).transform;
        _itemsParent.parent = transform.parent;

        levelMeshController.OnLevelMeshGenerated += OnLevelMeshGenerated;
    }

    void Start()
    {
        _waitingForOperation = null;
        _itemPoolGenerator = new ItemPoolGenerator(itemCount);

        GenerateLevel();
        GenerateItemPool();
    }

    private void Update()
    {
        if (_waitingForOperation != null && _waitingForOperation.task.IsCompleted)
        {
            GridGenerationOperation gridGenerationOp = _waitingForOperation;
            _waitingForOperation = null;

            _currentGrid = gridGenerationOp.generator.grid;

            levelMeshController.Initialize(_currentGrid.Size);
            levelMeshController.Generate(_currentGrid, true);
        }

    }

    private void OnDestroy()
    {
        levelMeshController.OnLevelMeshGenerated -= OnLevelMeshGenerated;
        _waitingForOperation?.cancellationTokenSource.Cancel();

        _itemPoolGenerator?.DestroyItems();
        _itemPoolGenerator = null;
    }

    public void GenerateLevel()
    {
        //GenerateGrid();
        GenerateGridMultithreaded();
    }

    private void OnLevelMeshGenerated()
    {
        StartCoroutine(MoveItemsOnItemPoolIsReady());

        CallOnGenerationCompleted();
    }

    public void GenerateGrid()
    {
        //int width = resolution.x;
        //int height = resolution.y;

        Vector3 halfLevelSize = HalfLevelDimensionsInMeters;
        GridGenerator gridGenerator = new GridGenerator(this, levelSizeInMeters.x, levelSizeInMeters.y, resolution, new Vector3(-halfLevelSize.x, 0f, -halfLevelSize.z));
        levelMeshController.Initialize(new Vector3Int(resolution.x, resolution.y, resolution.x));

        gridGenerator.GenerateGrid();
        Grid grid = gridGenerator.grid;

        _currentGrid = grid;
        levelMeshController.Generate(_currentGrid, multithreaded: false);
    }

    public void GenerateGridMultithreaded()
    {
        //_levelMeshController.levelSizeInChunks.y = 1;// Remark: Level size in chunks y counterpart is constrained for multithreaded.
        //int width = resolution.x;
        //int height = resolution.y;

        //Vector3Int levelSizeInChunks = _levelMeshController.levelSizeInChunks;
        //Vector3Int chunkSizeInVoxels = new Vector3Int(width / levelSizeInChunks.x, height / levelSizeInChunks.y, width / levelSizeInChunks.z);

        //int arraySize = levelSizeInChunks.x * levelSizeInChunks.z;
        //Task[] tasks = new Task[arraySize];
        //GridGenerator[] generators = new GridGenerator[arraySize];

        //Vector3Int currentPos = Vector3Int.zero;
        //int index = 0;
        //for (int x = 0; x < levelSizeInChunks.x; x++)
        //{
        //    for (int z = 0; z < levelSizeInChunks.z; z++)
        //    {
        //        Vector3Int passPos = new Vector3Int(currentPos.x, currentPos.y, currentPos.z);
        //        GridGenerator gridGenerator = new GridGenerator(this, chunkSizeInVoxels.x, chunkSizeInVoxels.y, passPos);
        //        Task chunkTask = Task.Factory.StartNew(gridGenerator.GenerateGrid);
        //        generators[index] = gridGenerator;
        //        tasks[index] = chunkTask;
        //        index++;

        //        currentPos.z += chunkSizeInVoxels.z;
        //    }

        //    currentPos.x += chunkSizeInVoxels.x;
        //}

        //Task.WaitAll(tasks);

        //Grid[] grids = new Grid[levelSizeInChunks.x * levelSizeInChunks.z];
        //for (int i = 0; i < generators.Length; i++)
        //{
        //    grids[i] = generators[i].grid;
        //}

        ////_currentGrid = grid;
        //_levelMeshController.GenerateAllGrids(grids, new Vector3Int(width, height, width), true, tasks);
        //_levelMeshController.UpdateMeshes();

        Vector3 halfLevelSize = HalfLevelDimensionsInMeters;
        GridGenerator gridGenerator = new GridGenerator(this, levelSizeInMeters.x, levelSizeInMeters.y, resolution, new Vector3(-halfLevelSize.x, 0f, -halfLevelSize.z));
        levelMeshController.Initialize(new Vector3Int(resolution.x, resolution.y, resolution.x));

        var ctSource = new CancellationTokenSource();
        Task chunkTask = Task.Factory.StartNew(gridGenerator.GenerateGrid, ctSource.Token);
        _waitingForOperation = new GridGenerationOperation(chunkTask, gridGenerator, ctSource);
    }

    #region Item Generation
    private void GenerateItemPool()
    {
        if (_weightSum < 0)
        {
            CalculateWeights();
        }

        _itemPoolGenerator.StartGeneration(this, ChooseRandomItem, true, _itemsParent);
    }

    private IEnumerator MoveItemsOnItemPoolIsReady()
    {
        while (!_itemPoolGenerator.IsCompleted)
        {
            yield return null;
        }

        foreach (GameObject item in _itemPoolGenerator.Pool)
        {
            item.transform.position = GenerateRandomPositionOnLevel();
            item.transform.rotation = GenerateRandomRotation();
            item.SetActive(true);
        }
    }

    public override Vector3 GenerateRandomPositionOnLevel()
    {
        Vector3 halfLevelSizeInMeters = HalfLevelDimensionsInMeters;
        float x = Random.Range(-halfLevelSizeInMeters.x, halfLevelSizeInMeters.x);
        float z = Random.Range(-halfLevelSizeInMeters.z, halfLevelSizeInMeters.z);
        float y = Random.Range(GetLevelHeightAt(x, z) + 5f, levelSizeInMeters.y);
        return new Vector3(x, y, z);
    }


    private void CalculateWeights()
    {
        float weightSum = 0;
        foreach (LevelObjectData data in levelObjects)
        {
            weightSum += data.weight;
        }
        _weightSum = weightSum;
    }

    private GameObject ChooseRandomItem()
    {
        Debug.Assert(_weightSum > 0f);

        float randomValue = Random.Range(0f, _weightSum);
        int currIndex = -1;
        while (randomValue > 0)
        {
            currIndex++;
            randomValue -= levelObjects[currIndex].weight;
        }

        return levelObjects[currIndex].levelObject;
    }
    #endregion // Item Generation

    public override float GetLevelHeightAt(float x, float z)
    {
        return Mathf.Max(0f, GetNoiseValueAt(x, z) * levelSizeInMeters.y);
    }

    //public Grid CurrentGrid => _currentGrid;

    #region GUI
    // To name elements according to prefab names.
    private void OnValidate()
    {
        for (int i = 0; i < levelObjects.Count; i++)
        {
            var levelObjectData = levelObjects[i];
            string name = levelObjectData.levelObject == null || string.IsNullOrWhiteSpace(levelObjectData.levelObject.name) ? "": levelObjectData.levelObject.name;

            levelObjectData.name = name;
            levelObjects[i] = levelObjectData;
        }
    }
    #endregion //GUI
}
