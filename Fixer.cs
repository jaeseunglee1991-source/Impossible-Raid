using System;
using System.IO;
using System.Text;

class Program {
    static void Main() {
        string dir = @"a:\Impossible Raid\Assets\Scripts";
        foreach(var f in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)) {
            string text = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;
            
            if(text.Contains("DatabaseManager.Instance.Client")) {
                text = text.Replace("DatabaseManager.Instance.Client", "DatabaseManager.Instance.SupabaseClient");
                changed = true;
            }
            if(text.Contains("SupabaseManager.Instance.client")) {
                text = text.Replace("SupabaseManager.Instance.client", "Supabase.Client.Instance");
                changed = true;
            }
            if(f.EndsWith("ReviveService.cs") && text.Contains("BattleManager.Instance.ExecuteRevive();")) {
                text = text.Replace("BattleManager.Instance.ExecuteRevive();", "if(BossRaid.Combat.CombatManager.Instance != null) BossRaid.Combat.CombatManager.Instance.ReviveAllPlayers();");
                changed = true;
            }
            if(f.EndsWith("CombatManager.cs") && text.Contains("private void ReviveAllPlayers()")) {
                text = text.Replace("private void ReviveAllPlayers()", "public void ReviveAllPlayers()");
                changed = true;
            }
            if(f.EndsWith("CombatManager.cs") && text.Contains("player.currentHealth = player.maxHealth * 0.5f;")) {
                text = text.Replace("player.currentHealth = player.maxHealth * 0.5f;", "player.Revive(0.5f);");
                changed = true;
            }
            if(f.EndsWith("GrowthManager.cs") && !text.Contains("public void AddGold")) {
                text = text.Replace("public void AddFakeGold(double amount)
        {
            displayGold += amount;
            OnGoldChanged?.Invoke(displayGold);
        }", "public void AddFakeGold(double amount)
        {
            displayGold += amount;
            OnGoldChanged?.Invoke(displayGold);
        }

        public void AddGold(double amount)
        {
            displayGold += amount;
            OnGoldChanged?.Invoke(displayGold);
        }");
                changed = true;
            }

            if(changed) {
                File.WriteAllText(f, text, Encoding.UTF8);
                Console.WriteLine("Updated " + f);
            }
        }
    }
}
