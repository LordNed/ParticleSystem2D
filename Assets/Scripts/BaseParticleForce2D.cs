using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class BaseParticleForce2D : MonoBehaviour
{
    protected List<ParticleSystem2D> m_affectingSystems = new List<ParticleSystem2D>();

    protected virtual void OnDestroy()
    {
        for(int i = 0; i < m_affectingSystems.Count; i++)
        {
            m_affectingSystems[i].RemoveForce(this);
        }
    }

    public virtual void OnForceAddedToSystem(ParticleSystem2D system)
    {
        if(m_affectingSystems.Contains(system))
        {
            Debug.LogWarning(string.Format("Attempted to add particle system {0} to force {1} but force already belongs to system!", system.name, gameObject.name));
            return;
        }

        m_affectingSystems.Add(system);
    }

    public virtual void OnForceRemovedFromSystem(ParticleSystem2D system)
    {
        if (!m_affectingSystems.Contains(system))
        {
            Debug.LogWarning(string.Format("Attempted to remove particle system {0} from force {1} but force doesn't belong to this system!", system.name, gameObject.name));
            return;
        }

        m_affectingSystems.Remove(system);
    }

    public abstract bool PointIsInShape(Vector2 pointInWorldspace);
    public abstract void ApplyForce(ParticleSystem2D.Particle2D toParticle);
    public abstract void OnDrawGizmosSelected();
}
