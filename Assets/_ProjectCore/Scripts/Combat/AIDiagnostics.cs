using UnityEngine;
using BossRaid.Combat;
using BossRaid.Combat.Boss;
using BossRaid.Managers;

public class AIDiagnostics : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            RunDiagnostics();
        }
    }

    private void RunDiagnostics()
    {
        Debug.Log("=== AI Diagnostics Starting ===");
        
        var controllers = FindObjectsByType<TagCharacterController>(FindObjectsSortMode.None);
        Debug.Log($"Found {controllers.Length} TagCharacterControllers in scene.");

        foreach (var c in controllers)
        {
            var cb = c.characterBase;
            bool isCombat = c.GetType().GetField("isCombatActive", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(c) as bool? ?? false;
            bool isIdleAI = c.GetType().GetField("isIdleAIMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(c) as bool? ?? false;
            var boss = c.GetType().GetField("currentBoss", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(c);

            Debug.Log($"[Char: {cb?.characterName}] Active: {isCombat}, IdleAI: {isIdleAI}, Target: {(boss != null ? "Found" : "NULL")}");
            
            if (cb != null)
            {
                Debug.Log($"Stats - Speed: {cb.movementSpeed}, AtkSpeed: {cb.attackSpeed}, AtkRange: {cb.attackRange}");
            }
        }

        var idleBoss = FindFirstObjectByType<IdleBoss>();
        Debug.Log($"IdleBoss in scene: {(idleBoss != null ? "YES" : "NO")}");
        
        if (BattleManager.Instance != null)
        {
            Debug.Log($"BattleManager currentBoss: {(BattleManager.Instance.currentBoss != null ? "YES" : "NO")}");
        }

        Debug.Log("=== AI Diagnostics Ending ===");
    }
}
