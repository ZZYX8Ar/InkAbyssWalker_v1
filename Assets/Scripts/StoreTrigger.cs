using UnityEngine;

/// <summary>
/// 商店 NPC 触发器：玩家靠近后按 E 键打开商店。
/// 挂载到商店 NPC 物体上，需要 Collider2D（Is Trigger）和 PlayerController 标签检测。
/// 参考 AbilityUnlockTrigger 的碰撞检测模式。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StoreTrigger : MonoBehaviour
{
    [Header("商店配置")]
    [Tooltip("商店配置表（ShopTableSO），留空则使用 ShopService 的默认商店")]
    [SerializeField] private StoreAndInventory.ShopTableSO shopTable;

    [Header("交互提示")]
    [Tooltip("是否显示 '按 E 打开商店' 的提示")]
    [SerializeField] private bool showInteractPrompt = true;

    [Tooltip("提示文本内容")]
    [SerializeField] private string interactText = "按 E 打开商店";

    private bool playerInRange = false;
    private bool shopUILoaded = false;

    private void Start()
    {
        // 确保 Collider2D 是 Trigger
        var col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"[StoreTrigger] {gameObject.name} 的 Collider2D 不是 Trigger，商店触发器可能无法正常工作。");
        }
    }

    private void Update()
    {
        if (!playerInRange) return;

        // 检测 E 键打开商店
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.E))
        {
            OpenShop();
        }
    }

    /// <summary>
    /// 打开商店面板
    /// </summary>
    public void OpenShop()
    {
        // 通过 StoreInventoryPanelController 打开商店
        var panelController = FindObjectOfType<StoreAndInventory.StoreInventoryPanelController>();
        if (panelController != null)
        {
            panelController.OpenShop();
            return;
        }

        // 如果没有 StoreInventoryPanelController，直接通过 ShopService 打开
        var shopService = FindObjectOfType<StoreAndInventory.ShopService>();
        if (shopService != null)
        {
            if (shopTable != null)
                shopService.Open(shopTable);
            else
                shopService.Open();

            // 同时打开 ShopUI
            var shopUI = FindObjectOfType<StoreAndInventory.ShopUI>();
            shopUI?.Open();
            return;
        }

        Debug.LogWarning("[StoreTrigger] 未找到 StoreInventoryPanelController 或 ShopService，请确保场景中有商店服务。");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 检测玩家进入范围
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        playerInRange = true;

        if (showInteractPrompt)
        {
            // TODO: 可以在这里触发 UI 提示（如显示 "按 E 打开商店"）
            Debug.Log($"[StoreTrigger] 玩家进入商店范围：{interactText}");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        playerInRange = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 绘制商店范围指示
        var col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
        Gizmos.DrawCube(transform.position, col.bounds.size);

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireCube(transform.position, col.bounds.size);

        // 绘制商店图标
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position + Vector3.up * 1.5f, 0.15f);
    }
#endif
}
