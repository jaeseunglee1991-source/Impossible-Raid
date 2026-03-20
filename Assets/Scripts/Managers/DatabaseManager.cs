using UnityEngine;
using Supabase;
using System.Threading.Tasks;

namespace BossRaid.Managers
{
    /// <summary>
    /// Supabase 연결 및 핵심 DB 통신을 담당하는 싱글톤 매니저
    /// </summary>
    public class DatabaseManager : MonoBehaviour
    {
        public static DatabaseManager Instance { get; private set; }

        [Header("Supabase Configuration")]
        public string supabaseUrl = "https://qycfsajwzmdsonkymobe.supabase.co";
        public string supabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InF5Y2ZzYWp3em1kc29ua3ltb2JlIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzM5MzgxMjcsImV4cCI6MjA4OTUxNDEyN30.ldEtt1eVXhHfOFSRQeK_LBM4ZG_DWmYoajBbBfImcEI";

        public Client Client { get; private set; }

        private async void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);

            await InitializeSupabase();
        }

        private async Task InitializeSupabase()
        {
            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseAnonKey))
            {
                Debug.LogError("[DatabaseManager] URL or Key is missing!");
                return;
            }

            var options = new SupabaseOptions
            {
                AutoConnectRealtime = true
            };

            Client = new Client(supabaseUrl, supabaseAnonKey, options);
            await Client.InitializeAsync();
            
            Debug.Log("[DatabaseManager] Supabase initialized successfully.");
        }
    }
}
