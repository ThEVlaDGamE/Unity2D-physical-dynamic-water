using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class WaterManager : MonoBehaviour
{
    LineRenderer Body;

    float[] xpositions;
    float[] ypositions;
    float[] velocities;
    float[] accelerations;

    GameObject[] meshobjects;
    GameObject[] colliders;
    Mesh[] meshes;

    public Material bodyMaterial;
    public GameObject watermesh;

    // Константы физики волн
    const float springconstant = 0.02f;         // Коэффициент жёсткости волн
    const float damping = 0.04f;                // Коэффициент гашения волн 
    const float spread = 0.05f;                 // Коэффициент скорости распростанения волн
    const float z = -1f;

    public float effectMassOnWave = 0.01f;      // Влияние массы тела на создаваемую им волну

    const int lineNodesOn1Width = 5;            // Количество узлов линии воды на единицу длины
    const int numberOfWaterPhysicsPasses = 8;   // Количество проходов при расчёте движения воды

    public float depthDeforeSubmerged = 0.1f;   // Глубина объектов перед погружением
    public float displacementAmount = 3f;       // Величина смещения


    float baseheight;
    float bottom;


    // Создание воды
    void Start()
    {
        SpawnWater(-50, 100, 0, -10);
    }

    // Функция удара об воду
    public void Splash(float xpos, float velocity)
    {
        if (xpos >= xpositions[0] && xpos <= xpositions[xpositions.Length - 1])
        {
            xpos -= xpositions[0];

            int index = Mathf.RoundToInt((xpositions.Length - 1) * (xpos / (xpositions[xpositions.Length - 1] - xpositions[0])));

            velocities[index] += velocity;
        }
    }

    // Создание воды на сцене
    public void SpawnWater(float Left, float Width, float Top, float Bottom)
    {
        // Создание коллайдера воды (для плавания объектов)
        gameObject.AddComponent<BoxCollider2D>();
        gameObject.GetComponent<BoxCollider2D>().offset = new Vector2(Left + Width / 2, (Top + Bottom) / 2);
        gameObject.GetComponent<BoxCollider2D>().size = new Vector2(Width, Top - Bottom);
        gameObject.GetComponent<BoxCollider2D>().isTrigger = true;


        int edgecount = Mathf.RoundToInt(Width) * lineNodesOn1Width;
        int nodecount = edgecount + 1;

        Body = gameObject.AddComponent<LineRenderer>();
        Body.material = bodyMaterial;
        Body.material.renderQueue = 1000;
        Body.positionCount = nodecount;
        Body.startWidth = 0.1f;
        Body.endWidth = 0.1f;

        xpositions = new float[nodecount];
        ypositions = new float[nodecount];
        velocities = new float[nodecount];
        accelerations = new float[nodecount];

        meshobjects = new GameObject[edgecount];
        meshes = new Mesh[edgecount];
        colliders = new GameObject[edgecount];

        baseheight = Top;
        bottom = Bottom;

        for (int i = 0; i < nodecount; i++)
        {
            ypositions[i] = Top;
            xpositions[i] = Left + Width * i / edgecount;
            Body.SetPosition(i, new Vector3(xpositions[i], Top, z));
            accelerations[i] = 0;
            velocities[i] = 0;
        }

        // Настройка мешей
        for (int i = 0; i < edgecount; i++)
        {
            meshes[i] = new Mesh();

            // Определение угловых точек
            Vector3[] Vertices = new Vector3[4];
            Vertices[0] = new Vector3(xpositions[i], ypositions[i], z);
            Vertices[1] = new Vector3(xpositions[i + 1], ypositions[i + 1], z);
            Vertices[2] = new Vector3(xpositions[i], bottom, z);
            Vertices[3] = new Vector3(xpositions[i + 1], bottom, z);

            Vector2[] UVs = new Vector2[4];
            UVs[0] = new Vector2(0, 1);
            UVs[1] = new Vector2(1, 1);
            UVs[2] = new Vector2(0, 0);
            UVs[3] = new Vector2(1, 0);

            int[] tris = new int[6] { 0, 1, 3, 3, 2, 0 };

            meshes[i].vertices = Vertices;
            meshes[i].uv = UVs;
            meshes[i].triangles = tris;

            meshobjects[i] = Instantiate(watermesh, Vector3.zero, Quaternion.identity) as GameObject;
            meshobjects[i].GetComponent<MeshFilter>().mesh = meshes[i];
            meshobjects[i].transform.parent = transform;

            colliders[i] = new GameObject();
            colliders[i].name = "Trigger";
            colliders[i].AddComponent<BoxCollider2D>();
            colliders[i].transform.parent = transform;

            colliders[i].transform.position = new Vector3(Left + Width * (i + 0.5f) / edgecount, Top - 0.5f, 0);
            colliders[i].transform.localScale = new Vector3(Width / edgecount, 1, 1);

            colliders[i].GetComponent<BoxCollider2D>().isTrigger = true;
            colliders[i].AddComponent<WaterDetector>();
        }
    }

    // Обновление позиции мешей
    void UpdateMeshes()
    {
        for (int i = 0; i < meshes.Length; i++)
        {
            Vector3[] Vertices = new Vector3[4];
            Vertices[0] = new Vector3(xpositions[i], ypositions[i], z);
            Vertices[1] = new Vector3(xpositions[i + 1], ypositions[i + 1], z);
            Vertices[2] = new Vector3(xpositions[i], bottom, z);
            Vertices[3] = new Vector3(xpositions[i + 1], bottom, z);

            meshes[i].vertices = Vertices;
        }
    }

    // Физический расчёт состояние воды
    void FixedUpdate()
    {
        for (int i = 0; i < xpositions.Length; i++)
        {
            float force = springconstant * (ypositions[i] - baseheight) + velocities[i] * damping;
            accelerations[i] = -force;
            ypositions[i] += velocities[i];
            velocities[i] += accelerations[i];
            Body.SetPosition(i, new Vector3(xpositions[i], ypositions[i], z));
        }

        float[] leftDeltas = new float[xpositions.Length];
        float[] rightDeltas = new float[xpositions.Length];

        for (int j = 0; j < numberOfWaterPhysicsPasses; j++)
        {
            for (int i = 0; i < xpositions.Length; i++)
            {
                if (i > 0)
                {
                    leftDeltas[i] = spread * (ypositions[i] - ypositions[i - 1]);
                    velocities[i - 1] += leftDeltas[i];
                }
                if (i < xpositions.Length - 1)
                {
                    rightDeltas[i] = spread * (ypositions[i] - ypositions[i + 1]);
                    velocities[i + 1] += rightDeltas[i];
                }
            }

            for (int i = 0; i < xpositions.Length; i++)
            {
                if (i > 0)
                    ypositions[i - 1] += leftDeltas[i];
                if (i < xpositions.Length - 1)
                    ypositions[i + 1] += rightDeltas[i];
            }
        }
        UpdateMeshes();
    }

    public float GetWaterLevel(float _x)
    {
        for (int i = 0; i < xpositions.Length; i++)
        {
            if (xpositions[i] > _x)
            {
                return (ypositions[i] + ypositions[i - 1]) / 2;
            }
        }

        return 0;
    }

    // Плавание тел
    private void OnTriggerStay2D(Collider2D collision)
    {
        Rigidbody2D rigidbody2D = collision.GetComponent<Rigidbody2D>();
        float waveHeight = GetWaterLevel(rigidbody2D.transform.position.x);

        float displacementMultiplier = Mathf.Clamp01((waveHeight - rigidbody2D.transform.position.y) / depthDeforeSubmerged) * displacementAmount;
        rigidbody2D.AddForceAtPosition(new Vector2(0f, Mathf.Abs(Physics2D.gravity.y * displacementMultiplier)/* * rigidbody.mass*/),
            rigidbody2D.transform.position);

        rigidbody2D.AddForce(displacementMultiplier * -rigidbody2D.velocity * 0.99f * Time.fixedDeltaTime);
        rigidbody2D.AddTorque(displacementMultiplier * -rigidbody2D.angularVelocity * 0.5f * Time.fixedDeltaTime);
    }
}
