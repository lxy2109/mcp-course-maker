using UnityEngine;

public class GameSetup : MonoBehaviour
{
    void Start()
    {
        SetupBall();
        SetupPlatform();
        SetupBlocks();
    }
    
    void SetupBall()
    {
        GameObject ball = GameObject.Find("Ball");
        if (ball != null)
        {
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = ball.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            
            SphereCollider collider = ball.GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = ball.AddComponent<SphereCollider>();
            }
        }
    }
    
    void SetupPlatform()
    {
        GameObject platform = GameObject.Find("Platform");
        if (platform != null)
        {
            BoxCollider collider = platform.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = platform.AddComponent<BoxCollider>();
            }
            collider.isTrigger = true;
        }
    }
    
    void SetupBlocks()
    {
        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                string blockName = "Block_" + row + "_" + col;
                GameObject block = GameObject.Find(blockName);
                if (block != null)
                {
                    BoxCollider collider = block.GetComponent<BoxCollider>();
                    if (collider == null)
                    {
                        collider = block.AddComponent<BoxCollider>();
                    }
                }
            }
        }
    }
}