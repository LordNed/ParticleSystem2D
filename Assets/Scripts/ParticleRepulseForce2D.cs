using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CircleCollider2D))]
public class ParticleRepulseForce2D : BaseParticleForce2D
{
    [SerializeField] private float m_forceAmount;
    [SerializeField] private float m_forceRadius;

    private CircleCollider2D m_triggerArea;
    private Transform m_transformCache;

    private void Awake()
    {
        m_triggerArea = GetComponent<CircleCollider2D>();
        m_triggerArea.isTrigger = true;
        m_triggerArea.radius = m_forceRadius;

        m_transformCache = transform;
    }


    public override bool PointIsInShape(Vector2 pointInWorldspace)
    {
        return m_triggerArea.OverlapPoint(pointInWorldspace);
    }

    public override void ApplyForce(ParticleSystem2D.Particle2D toParticle)
    {
        // Calc the direction to the particle
        Vector2 dir = new Vector2(toParticle.Position.x - m_transformCache.position.x, toParticle.Position.y - m_transformCache.position.y);
        dir.Normalize();

        toParticle.Velocity += m_forceAmount * dir * Time.deltaTime;
    }

    public override void OnDrawGizmosSelected()
    {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
        Gizmos.DrawWireSphere(Vector3.zero, m_forceRadius - 0.01f);
    }
}
