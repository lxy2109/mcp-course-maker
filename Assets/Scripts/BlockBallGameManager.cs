using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class BlockBallGameManager : MonoBehaviour
{
    [Header("游戏对象")]
    public Transform platform;
    public Rigidbody ball;
    
    [Header("游戏设置")]
    public float platformSpeed = 10f;
    public float ballLaunchForce = 15f;
    public float platformBounds = 5f;
    
    private int score = 0;
    private bool ballLaunched = false;
    private bool gameOver = false;
    private List<GameObject> blocks = new List<GameObject>();
    private Vector3 ballStartPosition;
    
    void Start()
    {
        // 自动查找游戏对象
        if (platform == null)
            platform = GameObject.Find("Platform").transform;
        if (ball == null)
            ball = GameObject.Find("Ball").GetComponent<Rigidbody>();
            
        ballStartPosition = ball.transform.position;
        
        // 收集所有方块
        CollectBlocks();
        
        // 球初始状态
        ball.isKinematic = true;
        
        Debug.Log("游戏开始！点击鼠标左键发射球！");
    }
    
    void Update()
    {
        if (gameOver) return;
        
        // 平台移动控制
        MovePlatform();
        
        // 发射球
        if (Input.GetMouseButtonDown(0) && !ballLaunched)
        {
            LaunchBall();
        }
        
        // 检查球是否掉落
        if (ball.transform.position.y < -6f && ballLaunched)
        {
            GameOver();
        }
        
        // 检查胜利条件
        if (blocks.Count == 0 && ballLaunched)
        {
            GameWin();
        }
    }
    
    void MovePlatform()
    {
        float mouseX = Input.mousePosition.x / Screen.width * 2 - 1;
        Vector3 newPosition = platform.position;
        newPosition.x = Mathf.Clamp(mouseX * platformBounds, -platformBounds, platformBounds);
        platform.position = newPosition;
        
        // 如果球还没发射，跟随平台移动
        if (!ballLaunched)
        {
            Vector3 ballPos = ball.transform.position;
            ballPos.x = newPosition.x;
            ball.transform.position = ballPos;
        }
    }
    
    void LaunchBall()
    {
        ballLaunched = true;
        ball.isKinematic = false;
        
        Vector3 launchDirection = Vector3.up + Vector3.right * Random.Range(-0.2f, 0.2f);
        ball.AddForce(launchDirection.normalized * ballLaunchForce, ForceMode.Impulse);
        
        Debug.Log("球已发射！");
    }
    
    void CollectBlocks()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith("Block_"))
            {
                blocks.Add(obj);
                BlockController blockController = obj.GetComponent<BlockController>();
                if (blockController == null)
                {
                    blockController = obj.AddComponent<BlockController>();
                }
                blockController.gameManager = this;
            }
        }
        Debug.Log("找到 " + blocks.Count + " 个方块");
    }
    
    public void BlockDestroyed(GameObject block)
    {
        if (blocks.Contains(block))
        {
            blocks.Remove(block);
            score += 10;
            Debug.Log("方块被摧毁！分数: " + score);
            Destroy(block);
        }
    }
    
    void GameOver()
    {
        gameOver = true;
        Debug.Log("游戏失败！最终分数: " + score);
    }
    
    void GameWin()
    {
        gameOver = true;
        Debug.Log("恭喜获胜！最终分数: " + score);
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == ball.gameObject && ballLaunched)
        {
            Vector3 launchDirection = Vector3.up + Vector3.right * Random.Range(-0.3f, 0.3f);
            ball.velocity = Vector3.zero;
            ball.AddForce(launchDirection.normalized * ballLaunchForce, ForceMode.Impulse);
            Debug.Log("球被平台接住并重新发射！");
        }
    }
}