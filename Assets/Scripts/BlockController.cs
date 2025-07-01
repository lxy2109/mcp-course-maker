using UnityEngine;

public class BlockController : MonoBehaviour
{
    public BlockBallGameManager gameManager;
    
    void OnCollisionEnter(Collision collision)
    {
        // 检查是否被球击中
        if (collision.gameObject.name == "Ball")
        {
            // 通知游戏管理器方块被摧毁
            if (gameManager != null)
            {
                gameManager.BlockDestroyed(gameObject);
            }
        }
    }
}