using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LibNoise;
using LibNoise.Generator;
using LibNoise.Operator;
using System.Runtime.InteropServices;

public class CaveGeneration : MonoBehaviour
{
    // Terrain variables
    private float frequency = 3.0f;
    private float lacuT = 10.0f;
    private float lacuB = 15.0f;
    private int octavesT = 2;
    private int octavesB = 4;
    private float threshold = 1.0f;
    private float falloff = 0.25f;
    private int seed;

    // Terrain objects
    public Terrain terrainB;
    public Terrain terrainT;
    private List<Vector3> grassPoints;
    private List<Vector3> pinePoints;
    private List<Vector3> rockPoints;
    private List<Vector3> housePoints;
    private List<Vector3> stalacPoints;

    // y value caps
    public int ymaxB = 25;
    public int ymaxT = 60;

    // Bottom object spawn variables
    public GameObject[] houseTree;
    public GameObject[] houseTreeTertiary;
    public int numHouseTree;
    public int[] numHouseTreeSecondary;
    public int numHouseTreeTertiary;
    public int[] houseTreeSpawnRadius;
    public float houseTreeCheckRadius;

    public GameObject[] rock;
    public GameObject[] rockSecondary;
    public int numRock;
    public int numRockSecondary;
    public int rockSpawnRadius;
    public float rockCheckRadius;

    public GameObject[] pineTree;
    public GameObject[] pineTreeSecondary;
    public int numPineTree;
    public int numPineTreeSecondary;
    public int pineTreeSpawnRadius;
    public float pineTreeCheckRadius;

    public GameObject[] grass;
    public int numGrass;
    public int numSecondGrass;
    public int grassSpawnRadius;
    public float grassCheckRadius;

    public GameObject door;
    public int numDoor;
    public int y;

    // Top object spawn varibles
    public GameObject[] stalac;
    public int numStalac;
    public int numStalacSecondary;
    public int stalacSpawnRadius;
    public float stalacCheckRadius;
    Quaternion angStalac = new Quaternion(0, -90, 90, 0);

    void Start()
    {
        // Generate seed
        seed = (int)(Random.value * 0xffffff);

        // Generate terrains
        Generate(terrainB, seed, lacuB, octavesB, 0.0f, 1.0f);
        Generate(terrainT, seed, lacuT, octavesT, 1.0f, 0.0f);

        grassPoints = new List<Vector3>();
        rockPoints = new List<Vector3>();
        pinePoints = new List<Vector3>();
        housePoints = new List<Vector3>();
        stalacPoints = new List<Vector3>();

        // Obtain points for each object type through Perlin noise
        PerlinGenerate(terrainB, "bottom");
        //PerlinGenerate(terrainT, "top");

        // Instantiate lower objects
        SettlementInstantiateObjects(numHouseTree, numHouseTreeSecondary, numHouseTreeTertiary, houseTree, houseTree, houseTreeTertiary, houseTreeSpawnRadius, houseTreeCheckRadius, terrainB, Quaternion.identity, ymaxB, housePoints);
        IterativeInstantiateObjects(numRock, numRockSecondary, rock, rockSecondary, rockSpawnRadius, rockCheckRadius, terrainB, ymaxB, Quaternion.identity, "bottom", rockPoints);
        IterativeInstantiateObjects(numPineTree, numPineTreeSecondary, pineTree, pineTreeSecondary, pineTreeSpawnRadius, pineTreeCheckRadius, terrainB, ymaxB, Quaternion.identity, "bottom", pinePoints);
        IterativeInstantiateObjects(numGrass, numSecondGrass, grass, grass, grassSpawnRadius, grassCheckRadius, terrainB, ymaxB, Quaternion.identity, "bottom", grassPoints);
        //InstantiateDoors(numDoor, door, terrainB, y);

        // Instantiate upper objects
        InstantiateObjects(numStalac, stalac, terrainT, ymaxT, stalacCheckRadius, angStalac, "top");
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

    public void PerlinGenerate(Terrain terrain, string layer)
    {
        // Obtain size of the terrain
        Vector3 terrainSize = terrain.terrainData.size;

        switch (layer)
        {
            case "bottom":
                // Loop through the x and z regions to sample each value
                for (int i = 0; i < terrainSize.x; i++)
                {
                    for (int j = 0; j < terrainSize.z; j++)
                    {
                        // Produce noise at the value
                        float sample = CalculatePerlinNoise(i, j, terrainSize);

                        // Add value to grass spawn region
                        if (sample >= 0f && sample < 0.8f)
                        {
                            Vector3 vec = new Vector3(i, 0f, j);
                            vec.y = terrain.SampleHeight(vec);
                            grassPoints.Add(vec);
                        }

                        // Add value to rock spawn region
                        if (sample >= 0f && sample < 0.2f)
                        {
                            Vector3 vec = new Vector3(i, 0f, j);
                            vec.y = terrain.SampleHeight(vec);
                            rockPoints.Add(vec);
                        }

                        // Add value to pine tree spawn region
                        if (sample >= 0.3f && sample < 0.8f)
                        {
                            Vector3 vec = new Vector3(i, 0f, j);
                            vec.y = terrain.SampleHeight(vec);
                            pinePoints.Add(vec);
                        }

                        // Add value to house tree spawn region
                        if (sample >= 0.9f && sample < 1.0f)
                        {
                            Vector3 vec = new Vector3(i, 0f, j);
                            vec.y = terrain.SampleHeight(vec);
                            housePoints.Add(vec);
                        }
                    }
                }

                break;

            case "top":

                for(int i = 0; i < terrainSize.x; i++)
                {
                    for(int j = 0; j < terrainSize.y; j++)
                    {
                        Vector3 vec = new Vector3(i, 0f, j);
                        vec.y = terrain.SampleHeight(vec);
                        stalacPoints.Add(vec);
                    }
                }
                
                break;

            default:
                break;
        }  
    }

    public float CalculatePerlinNoise(int x, int z, Vector3 terrainSize)
    {
        // Calculate noise coordinates
        float xpos = x / terrainSize.x * 50f;
        float zpos = z / terrainSize.z * 50f;

        // Obtain noise
        float output = Mathf.Clamp(Mathf.PerlinNoise(xpos, zpos), 0f, 1.0f);

        return output;
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

    public Vector3 PointOnRadius(Vector3 point, float radius, Terrain terrain, int count, int totalCount)
    {
        // Calculate angle from original point
        float ang = (count * (2.0f * Mathf.PI)) / totalCount;

        // Find new position using original point and calculated angle
        Vector3 sample = point + (new Vector3(Mathf.Cos(ang) * radius, 0, Mathf.Sin(ang) * radius));

        // Set y to sampled terrain height
        sample.y = terrain.SampleHeight(sample);

        return sample;
    }

    public Vector3 PointOnHalfRadius(Vector3 point, float radius, Terrain terrain, int count, int totalCount)
    {
        // Calculate desired angle from given point
        float ang = (count * Mathf.PI) / totalCount;

        // Find new position using original point and desired angle
        Vector3 sample = point + (new Vector3(Mathf.Cos(ang) * radius, 0, Mathf.Sin(ang) * radius));

        // Set y to the sampled terrain height
        sample.y = terrain.SampleHeight(sample);

        return sample;
    }

    public Vector3 PointOnLine(Vector3 point, Terrain terrain, int count, int gap)
    {
        // Set the point to either side of the original point, depending on the count value being odd or even
        if(count % 2 == 0)
        {
            // Set the multiplication value to determine the distance from the original point
            float i = count / 2;

            // Find the new position using multiplication value and gap
            Vector3 sample = point + new Vector3(gap * i, 0, 0);

            // Set y to the sampled terrain height
            sample.y = terrain.SampleHeight(sample);

            return sample;
        }

        else
        {
            // Set the coefficient to determine the distance from the original point, rounding to the neearest integer
            float i = Mathf.Round(count / 2);

            // Find the new position using the coefficient and gap
            Vector3 sample = point - new Vector3(gap * i, 0, 0);

            // Set y to the sampled terrain height
            sample.y = terrain.SampleHeight(sample);

            return sample;
        }
    }

    public Vector3 PointOnZigzag(Vector3 point, Terrain terrain, int count, int gap)
    {
        // Set the point to either side of the original point, depending on the count value being odd or even, and adjust the z value to a lower value depending on the points' position from the origin
        if (count % 2 == 0)
        {
            // Set the multiplication value to determine the distance from the original point
            float i = count / 2;

            // Find the new position using multiplication value and gap
            Vector3 sample = point + new Vector3(gap * i, 0, 0);

            // Set y to the sampled terrain height
            sample.y = terrain.SampleHeight(sample);

            // Adjust the z position depending on the coefficient
            if(i % 2 == 0)
            {
                sample.z -= 10.0f;
            }

            return sample;
        }

        else
        {
            // Set the coefficient to determine the distance from the original point, rounding to the neearest integer
            float i = Mathf.Round(count / 2);

            // Find the new position using the coefficient and gap
            Vector3 sample = point - new Vector3(gap * i, 0, 0);

            // Set y to the sampled terrain height
            sample.y = terrain.SampleHeight(sample);

            // Adjust the z position depending on the coefficient
            if (i % 2 == 0)
            {
                sample.z -= 10.0f;
            }

            return sample;
        }
    }

    private bool CheckPosition(Vector3 pos, float rad)
    {
        // Check surrounding area for obstacles
        Collider[] colliders = Physics.OverlapSphere(pos, rad);

        // Check all obstacles discovered
        foreach (Collider col in colliders)
        {
            // Return false if obstacle discovered
            if (col.tag == "Object" || col.tag == "House Tree")
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
                        Instantiate(objs[Random.Range(0, objs.Length)], pos, ang);
                    }

                    break;

                case "top":
                    // Find another position if current isn't viable
                    if (pos.y < ymax || clear == false)
                        i--;

                    // Instantiate object if current position is viable
                    else if (clear == true)
                    {
                        Instantiate(objs[Random.Range(0, objs.Length)], pos, ang);
                    }

                    break;

                default:
                    break;
            }
        }
    }

    public void InstantiateDoors(int numObject, GameObject obj, Terrain terrain, int yvalue)
    {
        for (int i = 0; i < numObject; i++)
        {
            // Find random position
            Vector3 pos = RandomTerrainPosition(terrain);

            // Variable to determine if position is clear for instantiation
            //bool clear = CheckPosition(pos, checkRadius);

            // Find another position if current isn't viable
            if (pos.y > yvalue || pos.y < yvalue)
                i--;

            // Instantiate object if current position is viable
            else if (pos.y == yvalue)
            {
                Instantiate(obj, pos, Quaternion.identity);
            }
        }
    }

    public void IterativeInstantiateObjects(int count, int secondCount, GameObject[] initObjs, GameObject[] iterObjs, int spawnRadius, float checkRadius, Terrain terrain, int ymax, Quaternion ang, string layer, List<Vector3> spawnPoints)
    {
        List<Vector3> points = new List<Vector3>();
		List<GameObject> origins = new List<GameObject>();

        // Loop through initial object count
        for (int i = 0; i < count; i++)
        {
            // Find random position from available positions
            Vector3 pos = spawnPoints[Random.Range(0, spawnPoints.Count)];

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
                        ang = Quaternion.Euler(0, Random.Range(0, 360), 0);
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
                        ang = Quaternion.Euler(0, Random.Range(0, 360), 0);
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
                            continue;

                        // Instantiate object if position is within the terrain boundary
                        else if (clearArea == true && clearOrigin == true)
                        {
                            // Instaniate secondary object at given position
                            ang = Quaternion.Euler(0, Random.Range(0, 360), 0);
                            Instantiate(iterObjs[Random.Range(0, (iterObjs.Length - 1))], pos, ang);
                        }

                        break;

                    case "top":
                        // Reroll given position if it exceeds the terrain boundary
                        if (pos.x < 0 || pos.x > terrain.terrainData.size.x || pos.z < 0 || pos.z > terrain.terrainData.size.z || pos.y < ymax || clearArea == false || clearOrigin == false)
                            continue;

                        // Instantiate object if position is within the terrain boundary
                        else if (clearArea == true && clearOrigin == true)
                        {
                            // Instaniate secondary object at given position
                            ang = Quaternion.Euler(0, Random.Range(0, 360), 0);
                            Instantiate(iterObjs[Random.Range(0, iterObjs.Length)], pos, ang);
                        }

                        break;

                    default:
                        break;
                }
            }
        }
    }

    public void SettlementInstantiateObjects(int count, int[] secondCount, int thirdCount, GameObject[] initObjs, GameObject[] iterObjs, GameObject[] detailObjs, int[] spawnRadius, float checkRadius, Terrain terrain, Quaternion ang, int ymax, List<Vector3> spawnPoints)
    {
        List<Vector3> points = new List<Vector3>();
        List<Vector3> pointsSecondary = new List<Vector3>();
        List<GameObject> origins = new List<GameObject>();

        // Loop through initial object count
        for (int i = 0; i < count; i++)
        {
            // Find random position from available positions
            Vector3 pos = spawnPoints[Random.Range(0, spawnPoints.Count)];

            // Variable to determine if position is clear for instantiation
            bool clear = CheckPosition(pos, checkRadius);
            bool settlementClear = CheckHouseTreePosition(pos, 100);

            // Find another position if current isn't viable
            if (pos.y > ymax || clear == false || settlementClear == false)
                i--;

            // Instantiate object if current position is viable
            else if (clear == true && settlementClear == true)
            {
                // Add position to the list of points
                points.Add(pos);

                // Instantiate initial object at random position
                ang = Quaternion.Euler(0, Random.Range(0, 360), 0);
                GameObject x = Instantiate(initObjs[Random.Range(0, initObjs.Length)], pos, ang);
                origins.Add(x);
            }
        }

        int secondCountValue = secondCount[Random.Range(0, secondCount.Length)];
        int spawnRadiusValue = spawnRadius[Random.Range(0, spawnRadius.Length)];

        // Loop through all gathered points
        for (int i = 0; i < points.Count; i++)
        {
            // Loop through secondary object count
            for (int j = 0; j < secondCountValue; j++)
            {
                // Find set position on radius of given point
                Vector3 pos = PointOnRadius(points[i], spawnRadiusValue, terrain, j, secondCountValue);
                //Vector3 pos = PointOnHalfRadius(points[i], spawnRadiusValue, terrain, j, secondCountValue);
                //Vector3 pos = PointOnLine(points[i], terrain, j, 20);
                //Vector3 pos = PointOnZigzag(points[i], terrain, j, 20);

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

        // Loop through all gathered points
        for (int i = 0; i < points.Count; i++)
        {
            // Loop through secondary object count
            for (int j = 0; j < thirdCount; j++)
            {
                // Find random position on radius of given point
                Vector3 pos = RandomPointInRadius(points[i], spawnRadiusValue, terrain);

                // Variables to determine if position is clear for instantiation
                bool clearArea = CheckPosition(pos, checkRadius);
                bool clearOrigin = CheckOrigin(pos, points[i], origins[i]);

                if (pos.x < 0 || pos.x > terrain.terrainData.size.x || pos.z < 0 || pos.z > terrain.terrainData.size.z || pos.y > ymax || clearArea == false || clearOrigin == false)
                    continue;

                // Instantiate object if position is within the terrain boundary
                else if (clearArea == true && clearOrigin == true)
                {
                    // Instaniate secondary object at given position
                    ang = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    GameObject x = Instantiate(detailObjs[Random.Range(0, detailObjs.Length)], pos, ang);
                }
            }
        }
    }
}

