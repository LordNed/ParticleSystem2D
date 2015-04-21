using UnityEngine;
using System.Collections;

public class ParticleSystem2D : MonoBehaviour
{
    private class Particle2D
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float LifeTime;
    }

    /// <summary> Set to 0 for no limit. Otherwise, only this many particles will be alive at any given time. </summary>
    public int MaxParticles;
    public Vector2 StartVelocity;
    public int EmissionRate;
    public float StartLifeTime;

    private Particle2D[] m_particleCache;
    private float m_particleExcessAccumulator;
    private int m_currentParticleCount;

    private MeshRenderer m_meshRenderer;
    private MeshFilter m_meshFilterCache;
    private Mesh m_meshCache;

    private void Awake()
    {
        m_particleCache = new Particle2D[MaxParticles];
        for (int i = 0; i < m_particleCache.Length; i++)
            m_particleCache[i] = new Particle2D();

        m_meshRenderer = gameObject.AddComponent<MeshRenderer>();
        m_meshCache = new Mesh();
        m_meshCache.name = "ParticleSystem2D";

        m_meshFilterCache = gameObject.AddComponent<MeshFilter>();
        m_meshFilterCache.mesh = m_meshCache;
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);

        for(int i = 0; i < m_currentParticleCount; i++)
        {
            Gizmos.DrawWireSphere(m_particleCache[i].Position, 0.1f);
        }
    }

    private void LateUpdate()
    {
        //////////////////
        /// SPAWN NEW PARTICLES (IF NEEDED/POSSIBLE)
        //////////////////

        // Calculate the number of particles we need to emit per second.
        m_particleExcessAccumulator += EmissionRate * Time.deltaTime;
        int numParticles = Mathf.FloorToInt(m_particleExcessAccumulator);
        m_particleExcessAccumulator -= numParticles;

        for (int i = 0; i < numParticles; i++)
        {
            if(m_currentParticleCount == MaxParticles)
                return;

            // Set the particle's velocity
            m_particleCache[m_currentParticleCount].Position = Vector3.zero;
            m_particleCache[m_currentParticleCount].Velocity = StartVelocity;
            m_particleCache[m_currentParticleCount].LifeTime = StartLifeTime;
            m_currentParticleCount++;
        }

        //////////////////
        /// UPDATE PARTICLES
        //////////////////
        for(int i = 0; i < m_currentParticleCount; i++)
        {
            m_particleCache[i].Position += m_particleCache[i].Velocity;
            m_particleCache[i].LifeTime -= Time.deltaTime;
        }


        //////////////////
        /// PRUNE DEAD PARTICLES
        //////////////////


        for(int i = 0; i < m_currentParticleCount; i++)
        {
            if (m_particleCache[i].LifeTime <= 0f)
            {
                // Remove the particle by swapping it with our last live particle
                // and then deincrement the live particle count.
                Particle2D lastLive = m_particleCache[m_currentParticleCount - 1];
                Particle2D deadParticle = m_particleCache[i];
                m_particleCache[i] = lastLive;
                m_particleCache[m_currentParticleCount - 1] = deadParticle;
                m_currentParticleCount--;
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
    }
}
