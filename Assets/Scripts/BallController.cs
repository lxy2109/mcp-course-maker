using UnityEngine;

public class BallController : MonoBehaviour
{
    private Rigidbody rb;
    public float maxSpeed = 20f;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    void Update()
    {
        // 限制球的最大速度
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // 确保球在碰撞后有合理的反弹
        if (collision.gameObject.name == "Platform")
        {
            Vector3 hitPoint = collision.contacts[0].point;
            Vector3 platformCenter = collision.transform.position;
            
            // 根据击中位置计算反弹方向
            float hitFactor = (hitPoint.x - platformCenter.x) / collision.collider.bounds.size.x;
            Vector3 direction = new Vector3(hitFactor, 1, 0).normalized;
            
            rb.velocity = direction * 15f;
        }
    }
}