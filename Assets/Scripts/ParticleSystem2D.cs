using UnityEngine;
using System.Collections;

public class ParticleSystem2D : MonoBehaviour
{
    private class Particle2D
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float LifeTime;
        public float Size;
    }

    public int MaxParticles;
    public Vector2 StartVelocity;
    public int EmissionRate;
    public float StartLifeTime;
    public float StartSize;
    public float GravityMultiplier = 1f;

    private Particle2D[] m_particleCache;
    private float m_particleExcessAccumulator;
    private int m_currentParticleCount;
    private Vector3[] m_vertices;
    private int[] m_indexes;

    private MeshFilter m_meshFilterCache;
    [SerializeField] private Mesh m_meshCache;

    private void Awake()
    {
        m_particleCache = new Particle2D[MaxParticles];
        for (int i = 0; i < m_particleCache.Length; i++)
            m_particleCache[i] = new Particle2D();

        gameObject.AddComponent<MeshRenderer>();
        m_meshFilterCache = gameObject.AddComponent<MeshFilter>();
        m_meshCache = new Mesh();
        m_meshCache.name = "ParticleSystem2D";
        m_meshCache.MarkDynamic();

        GenerateMesh();
        m_meshFilterCache.mesh = m_meshCache;
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);

        for (int i = 0; i < m_currentParticleCount; i++)
        {
            Gizmos.DrawWireSphere(m_particleCache[i].Position, 0.1f);
        }
    }

    private void LateUpdate()
    {
        bool indiciesUpdated = false;

        if (CheckAndSpawnNewParticles())
            indiciesUpdated = true;

        // Update the particle's position and other things.
        float deltaTime = Time.deltaTime;
        Vector2 gravity = Physics2D.gravity * deltaTime;

        for (int i = 0; i < m_currentParticleCount; i++)
        {
            Particle2D ptc = m_particleCache[i];

            ptc.Velocity = ptc.Velocity + gravity;

            ptc.Position = ptc.Position + (ptc.Velocity * deltaTime);

            ptc.LifeTime -= Time.deltaTime;
        }

        // Prune dead particles
        for (int i = 0; i < m_currentParticleCount; i++)
        {
            if (m_particleCache[i].LifeTime <= 0f)
            {
                // Remove the particle by swapping it with our last live particle
                // and then deincrement the live particle count.
                Particle2D deadParticle = m_particleCache[i];
                Particle2D tailLiveParticle = m_particleCache[m_currentParticleCount - 1];
                m_particleCache[i] = tailLiveParticle;
                m_particleCache[m_currentParticleCount - 1] = deadParticle;


                // Zero out it's position in the index buffer which causes a degenerate triangle so the GPU
                // skips trying to draw it.
                RemoveParticleFromGPU(m_currentParticleCount - 1);

                m_currentParticleCount--;
                indiciesUpdated = true;
            }
        }


        //////////////////
        /// APPLY FORCES
        //////////////////


        //////////////////
        /// RESOLVE/CHECK COLLISIONS
        //////////////////


        //////////////////
        /// UPDATE RENDER MESH
        //////////////////
        for (int i = 0; i < m_currentParticleCount; i++)
        {
            // Update the position of the vertices on the GPU.
            UpdateParticleOnGPU(i);
        }

        m_meshCache.vertices = m_vertices;
        if(indiciesUpdated)
            m_meshCache.SetIndices(m_indexes, MeshTopology.Triangles, 0);
    }

    private bool CheckAndSpawnNewParticles()
    {
        // Calculate the number of particles we need to emit per second.
        m_particleExcessAccumulator += EmissionRate * Time.deltaTime;
        int numParticles = Mathf.FloorToInt(m_particleExcessAccumulator);
        m_particleExcessAccumulator -= numParticles;

        bool indiciesUpdated = false;

        for (int i = 0; i < numParticles; i++)
        {
            if (m_currentParticleCount == MaxParticles)
                break;

            // Set the particle's velocity
            m_particleCache[m_currentParticleCount].Position = Vector3.zero;
            m_particleCache[m_currentParticleCount].Velocity = StartVelocity;
            m_particleCache[m_currentParticleCount].LifeTime = StartLifeTime;
            m_particleCache[m_currentParticleCount].Size = StartSize;

            // Recalculate it's indexes as they were previously crushed.
            AddParticleToGPU(m_currentParticleCount);
            

            m_currentParticleCount++;
            indiciesUpdated = true;
        }


        return indiciesUpdated;
    }

    private void GenerateMesh()
    {
        // Generate the Index and Vertex array. Their positions/indexes will be set
        // when the particle is created so we only need to allocate and assign the 
        // array on the GPU for now.

        m_vertices = new Vector3[MaxParticles * 4];
        m_indexes = new int[MaxParticles * 6];

        Vector3[] normals = new Vector3[MaxParticles * 4];
        for (int i = 0; i < normals.Length; i++)
            normals[i] = Vector3.back;

        Vector2[] uvs = new Vector2[MaxParticles * 4];
        for(int i = 0; i < MaxParticles; i++)
        {
            uvs[(i * 4) + 0] = new Vector2(0, 1); // UL
            uvs[(i * 4) + 1] = new Vector2(1, 1); // UR
            uvs[(i * 4) + 2] = new Vector2(0, 0); // BL
            uvs[(i * 4) + 3] = new Vector2(1, 0); // BR
        }

        m_meshCache.vertices = m_vertices;
        m_meshCache.SetIndices(m_indexes, MeshTopology.Triangles, 0);
        m_meshCache.normals = normals;
        m_meshCache.uv = uvs;
        m_meshCache.Optimize();
    }

    private void RemoveParticleFromGPU(int particleIndex)
    {
        int indexOffset = particleIndex * 6;

        m_indexes[indexOffset + 0] = 0;
        m_indexes[indexOffset + 1] = 0;
        m_indexes[indexOffset + 2] = 0;
        m_indexes[indexOffset + 3] = 0;
        m_indexes[indexOffset + 4] = 0;
        m_indexes[indexOffset + 5] = 0;
    }

    private void AddParticleToGPU(int particleIndex)
    {
        int indexOffset = particleIndex * 6;

        m_indexes[indexOffset + 0] = (m_currentParticleCount * 4) + 0;
        m_indexes[indexOffset + 1] = (m_currentParticleCount * 4) + 1;
        m_indexes[indexOffset + 2] = (m_currentParticleCount * 4) + 2;
        m_indexes[indexOffset + 3] = (m_currentParticleCount * 4) + 2;
        m_indexes[indexOffset + 4] = (m_currentParticleCount * 4) + 1;
        m_indexes[indexOffset + 5] = (m_currentParticleCount * 4) + 3;
    }

    private void UpdateParticleOnGPU(int particleIndex)
    {
        Particle2D prt = m_particleCache[particleIndex];
        float sizeHalf = prt.Size * 0.5f;

        // Upper Left
        m_vertices[(particleIndex * 4) + 0].x = prt.Position.x - sizeHalf;
        m_vertices[(particleIndex * 4) + 0].y = prt.Position.y + sizeHalf;

        // Upper Right
        m_vertices[(particleIndex * 4) + 1].x = prt.Position.x + sizeHalf;
        m_vertices[(particleIndex * 4) + 1].y = prt.Position.y + sizeHalf;

        // Bottom Left
        m_vertices[(particleIndex * 4) + 2].x = prt.Position.x - sizeHalf;
        m_vertices[(particleIndex * 4) + 2].y = prt.Position.y - sizeHalf;

        // Bottom Right
        m_vertices[(particleIndex * 4) + 3].x = prt.Position.x + sizeHalf;
        m_vertices[(particleIndex * 4) + 3].y = prt.Position.y - sizeHalf;
    }
}
