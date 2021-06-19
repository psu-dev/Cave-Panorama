using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LibNoise;
using LibNoise.Generator;
using LibNoise.Operator;

public class CaveGeneration : MonoBehaviour
{
    // Terrain variables
    private float frequency = 3.0f;
    private float lacuT = 10.0f;
    private float lacuB = 15.0f;
    private int octavesT = 2;
    private int octavesB = 4;
    private float persist = 2.0f;
    private float threshold = 1.0f;
    private float falloff = 0.25f;
    private int seed;

    // Terrain objects
    public Terrain terrainB;
    public Terrain terrainT;

    // y value caps
    public int ymaxB = 25;
    public int ymaxT = 60;

    // Bottom object spawn variables
    public GameObject[] houseTree;
    public int numHouseTree;
    public int numSecondHouseTree;
    public int houseTreeRadius;
    //Quaternion angHouseTree = new Quaternion(0, 0, Mathf.PI / 4, 0);

    public GameObject[] rocks;
    public int numRock;
    public int rockRadius;

    public GameObject[] pineTrees;
    public int numPineTree;
    public int pineTreeRadius;

    public GameObject[] mushrooms;
    public int numMushroom;

    public GameObject[] gems;
    public int numGems;

    public GameObject[] grassA;
    public int numGrassA;
    public int numSecondGrassA;
    public int grassARadius;

    // Top object spawn varibles
    public GameObject[] stalac;
    public int numStalac;
    Quaternion angStalac = new Quaternion(0, -90, 90, 0);

    void Start()
    {
        // Generate seed
        seed = (int)(Random.value * 0xffffff);

        // Generate terrains
        Generate(terrainB, seed, lacuB, octavesB, 0.0f, 1.0f);
        Generate(terrainT, seed, lacuT, octavesT, 1.0f, 0.0f);

        // Instantiate lower objects
        SettlementInstantiateObjects(numHouseTree, numSecondHouseTree, houseTree, houseTree, houseTreeRadius, 2.0f, terrainB, Quaternion.identity, ymaxB);
        IterativeInstantiateObjects(numRock, numGems, rocks, gems, rockRadius, 2.0f, terrainB, ymaxB, Quaternion.identity, "bottom");
        IterativeInstantiateObjects(numPineTree, numMushroom, pineTrees, mushrooms, pineTreeRadius, 2.0f, terrainB, ymaxB, Quaternion.identity, "bottom");
        IterativeInstantiateObjects(numGrassA, numSecondGrassA, grassA, grassA, grassARadius, 1.0f, terrainB, ymaxB, Quaternion.identity, "bottom");

        // Instantiate upper objects
        InstantiateObjects(numStalac, stalac, terrainT, ymaxT, 1.0f, angStalac, "top");
    }

    public void Generate(Terrain terrain, int seed, float lacu, int octaves, float x, float y)
    {
        TerrainData terrainData = terrain.terrainData;

        Debug.Log("Generate function called");

        // A new ridged multifractal generator
        var generator = new RidgedMultifractal(frequency, lacu, octaves, seed, QualityMode.High);

        // The thresholded output -> choose either 0.0 or 1.0, based on the output
        var clamped = new LibNoise.Operator.Select(new Const(x), new Const(y), generator);

        // Set the threshold and falloff rate
        clamped.SetBounds(0.0f, threshold);
        clamped.FallOff = falloff;

        // Create a 2D noise generator for the terrain heightmap, using the generator
        var noise = new Noise2D(terrainData.heightmapResolution, clamped);

        // Generate a plan from [0, 1] on x, [0, 1] on y
        noise.GeneratePlanar(0, 1, 0, 1);

        // Get the data in an array so we can use it to set the heights
        var data = noise.GetNormalizedData();

        // Set the heights
        terrainData.SetHeights(0, 0, data);
    }

    public Vector3 RandomTerrainPosition(Terrain terrain)
    {
        //Get the terrain size in all 3 dimensions
        Vector3 terrainSize = terrain.terrainData.size;

        //Choose a uniformly random x and z to sample y
        float rX = Random.Range(0, terrainSize.x);
        float rZ = Random.Range(0, terrainSize.z);

        //Sample y at this point and put into an offset vec3
        Vector3 sample = new Vector3(rX, 0, rZ);
        sample.y = terrain.SampleHeight(sample);

        return terrain.GetPosition() + sample;
    }

    public Vector3 RandomPointInRadius(Vector3 point, float radius, Terrain terrain)
    {
        // Sample ref point + random offset * rad
        var sample = point + Random.insideUnitSphere * radius;

        // Set y to sampled terrain height
        sample.y = terrain.SampleHeight(sample);

        return sample;
    }

    public Vector3 RandomPointOnRadius(Vector3 point, float radius, Terrain terrain, int count, int totalCount)
    {
        // Calculate angle from original point
        float ang = (count * (2.0f * Mathf.PI)) / totalCount;

        // Find new position using original point and calculated angle
        Vector3 sample = point + (new Vector3(Mathf.Cos(ang) * radius, 0, Mathf.Sin(ang) * radius));

        // Set y to sampled terrain height
        sample.y = terrain.SampleHeight(sample);

        return sample;
    }

    private bool CheckPosition(Vector3 pos, float rad)
    {
        // Check surrounding area for obstacles
        Collider[] colliders = Physics.OverlapSphere(pos, rad);

        // Check all obstacles discovered
        foreach (Collider col in colliders)
        {
            // Return false if obstacle discovered
            if (col.tag == "Object" /*|| col.tag == "House Tree"*/)
                return false;
        }

        // Return true if no obstacles discovered
        return true;
    }

    private bool CheckHouseTreePosition(Vector3 pos, float rad)
    {
        // Check surrounding area for obstacles
        Collider[] colliders = Physics.OverlapSphere(pos, rad);

        // Check all obstacles discovered
        foreach (Collider col in colliders)
        {
            // Return false if obstacle discovered
            if (col.tag == "House Tree")
                return false;
        }

        // Return true if no obstacles discovered
        return true;
    }

    private bool CheckOrigin(Vector3 pos, Vector3 point, GameObject obj)
    {
        // Obtain collider information for origin object
        SphereCollider col = obj.GetComponent<SphereCollider>();

        // Obtain values for radius squared and difference between centre and generated point squared
        float a = Mathf.Pow(col.radius, 2);
        float b = Mathf.Pow(pos.x - point.x, 2) + Mathf.Pow(pos.z - point.z, 2);

        // Comparing values to determine if generated point is within the origin object
        // Return false if within radius of the origin
        if (b < a || b == a)
        {
            return false;
        }

        // Return true if not
        return true;
    }

    public void InstantiateObjects(int numObject, GameObject[] objs, Terrain terrain, int ymax, float checkRadius, Quaternion ang, string layer)
    {
        for (int i = 0; i < numObject; i++)
        {
            // Find random position
            Vector3 pos = RandomTerrainPosition(terrain);

            // Variable to determine if position is clear for instantiation
            bool clear = CheckPosition(pos, checkRadius);

            // Switch statement determing terrain
            switch (layer)
            {
                case "bottom":
                    // Find another position if current isn't viable
                    if (pos.y > ymax || clear == false)
                        i--;

                    // Instantiate object if current position is viable
                    else if (clear == true)
                    {
                        GameObject x = Instantiate(objs[Random.Range(0, objs.Length)], pos, ang);
                    }

                    break;

                case "top":
                    // Find another position if current isn't viable
                    if (pos.y < ymax || clear == false)
                        i--;

                    // Instantiate object if current position is viable
                    else if (clear == true)
                    {
                        GameObject x = Instantiate(objs[Random.Range(0, objs.Length)], pos, ang);
                    }

                    break;

                default:
                    break;
            }
        }
    }

    public void IterativeInstantiateObjects(int count, int secondCount, GameObject[] initObjs, GameObject[] iterObjs, int spawnRadius, float checkRadius, Terrain terrain, int ymax, Quaternion ang, string layer)
    {
        List<Vector3> points = new List<Vector3>();
		List<GameObject> origins = new List<GameObject>();

        // Loop through initial object count
        for (int i = 0; i < count; i++)
        {
            // Find random position for initial object
            Vector3 pos = RandomTerrainPosition(terrainB);

            // Variable to determine if position is clear for instantiation
            bool clear = CheckPosition(pos, checkRadius);

            switch (layer)
            {
                case "bottom":
                    // Find another position if current isn't viable
                    if (pos.y > ymax || clear == false)
                        i--;

                    // Instantiate object if current position is viable
                    else if (clear == true)
                    {
                        // Add position to the list of points
                        points.Add(pos);

                        // Instantiate initial object at random position
                        GameObject x = Instantiate(initObjs[Random.Range(0, initObjs.Length)], pos, ang);
						origins.Add(x);
                    }

                    break;

                case "top":
                    // Find another position if current isn't viable
                    if (pos.y < ymax || clear == false)
                        i--;

                    // Instantiate object if current position is viable
                    else if (clear == true)
                    {
                        // Add position to the list of points
                        points.Add(pos);

                        // Instantiate initial object at random position
                        GameObject x = Instantiate(initObjs[Random.Range(0, initObjs.Length)], pos, ang);
						origins.Add(x);
                    }

                    break;

                default:
                    break;
            }
        }

        // Loop through all gathered points
        for (int i = 0; i < points.Count; i++)
        {
            // Loop through secondary object count
            for (int j = 0; j < secondCount; j++)
            {
                // Find random position within radius of given point
                Vector3 pos = RandomPointInRadius(points[i], spawnRadius, terrain);

                // Variables to determine if position is clear for instantiation
                bool clearArea = CheckPosition(pos, checkRadius);
                bool clearOrigin = CheckOrigin(pos, points[i], origins[i]);

                switch (layer)
                {
                    case "bottom":
                        // Reroll given position if it exceeds the terrain boundary
                        if (pos.x < 0 || pos.x > terrain.terrainData.size.x || pos.z < 0 || pos.z > terrain.terrainData.size.z || pos.y > ymax || clearArea == false || clearOrigin == false)
                            j--;

                        // Instantiate object if position is within the terrain boundary
                        else if (clearArea == true && clearOrigin == true)
                        {
                            // Instaniate secondary object at given position
                            GameObject x = Instantiate(iterObjs[Random.Range(0, iterObjs.Length)], pos, ang);
                        }

                        break;

                    case "top":
                        // Reroll given position if it exceeds the terrain boundary
                        if (pos.x < 0 || pos.x > terrain.terrainData.size.x || pos.z < 0 || pos.z > terrain.terrainData.size.z || pos.y < ymax || clearArea == false || clearOrigin == false)
                            j--;

                        // Instantiate object if position is within the terrain boundary
                        else if (clearArea == true && clearOrigin == true)
                        {
                            // Instaniate secondary object at given position
                            GameObject x = Instantiate(iterObjs[Random.Range(0, iterObjs.Length)], pos, ang);
                        }

                        break;

                    default:
                        break;
                }
            }
        }
    }

    public void SettlementInstantiateObjects(int count, int secondCount, GameObject[] initObjs, GameObject[] iterObjs, int spawnRadius, float checkRadius, Terrain terrain, Quaternion ang, int ymax)
    {
        List<Vector3> points = new List<Vector3>();
        List<GameObject> origins = new List<GameObject>();

        // Loop through initial object count
        for (int i = 0; i < count; i++)
        {
            // Find random position for initial object
            Vector3 pos = RandomTerrainPosition(terrain);

            // Variable to determine if position is clear for instantiation
            bool clear = CheckPosition(pos, checkRadius);
            //bool settlementClear = CheckHouseTreePosition(pos, 75);

            // Find another position if current isn't viable
            if (pos.y > ymax || clear == false /*|| settlementClear == false*/)
                i--;

            // Instantiate object if current position is viable
            else if (clear == true /*|| settlementClear == true*/)
            {
                // Add position to the list of points
                points.Add(pos);

                // Instantiate initial object at random position
                GameObject x = Instantiate(initObjs[Random.Range(0, initObjs.Length)], pos, ang);
                origins.Add(x);
            }
        }

        // Loop through all gathered points
        for (int i = 0; i < points.Count; i++)
        {
            // Loop through secondary object count
            for (int j = 0; j < secondCount; j++)
            {
                // Find random position on radius of given point
                Vector3 pos = RandomPointOnRadius(points[i], spawnRadius, terrain, j, secondCount);

                // Variables to determine if position is clear for instantiation
                bool clearArea = CheckPosition(pos, checkRadius);
                bool clearOrigin = CheckOrigin(pos, points[i], origins[i]);

                if (pos.x < 0 || pos.x > terrain.terrainData.size.x || pos.z < 0 || pos.z > terrain.terrainData.size.z || pos.y > ymax || clearArea == false || clearOrigin == false)
                {
                    continue;
                }

                // Instantiate object if position is within the terrain boundary
                else if (clearArea == true && clearOrigin == true)
                {
                    // Instaniate secondary object at given position
                    GameObject x = Instantiate(iterObjs[Random.Range(0, iterObjs.Length)], pos, ang);

                    // Face the object towards the centre object
                    Vector3 targetDirection = points[i] - x.transform.position;
                    Quaternion rotation = Quaternion.LookRotation(targetDirection, Vector3.up);
                    x.transform.rotation = rotation;
                }
            }
        }
    }
}

