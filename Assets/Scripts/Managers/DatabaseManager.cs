using UnityEngine;
using Supabase;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using BossRaid.Models; // Model 클래스들이 있는 네임스페이스 (필요시 프로젝트에 맞게 수정)

namespace BossRaid.Managers
{
    public class DatabaseManager : MonoBehaviour
    {
        // 씬 전환 시에도 유지되는 싱글톤 인스턴스
        public static DatabaseManager Instance { get; private set; }

        [Header("Supabase Settings")]
        [Tooltip("Supabase 프로젝트의 URL을 입력하세요.")]
        [SerializeField] private string supabaseUrl = "여기에_Supabase_URL_입력";
        
        [Tooltip("Supabase 프로젝트의 API Keys (anon public)을 입력하세요.")]
        [SerializeField] private string supabaseAnonKey = "여기에_Supabase_Anon_Key_입력";

        private Client _supabase;
        public Client SupabaseClient => _supabase;

        // DB 초기화 완료 여부 확인용
        public bool IsInitialized { get; private set; } = false;

        private async void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // 로비 -> 대기방 -> 인게임 이동 시 파괴 방지
                await InitializeSupabase();
            }
            else
            {
                Destroy(gameObject); // 중복 생성 방지
            }
        }

        /// <summary>
        /// Supabase 클라이언트 초기화 (앱 실행 시 최초 1회)
        /// </summary>
        private async Task InitializeSupabase()
        {
            try
            {
                var options = new SupabaseOptions
                {
                    AutoConnectRealtime = true // 대기방 및 로비의 실시간 동기화를 위해 활성화
                };

                _supabase = new Client(supabaseUrl, supabaseAnonKey, options);
                await _supabase.InitializeAsync();
                
                IsInitialized = true;
                Debug.Log("[DatabaseManager] Supabase 초기화 성공");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseManager] Supabase 초기화 실패: {ex.Message}");
            }
        }

        #region [ 1. 유저 데이터 관리 (UserRecord) ]

        /// <summary>
        /// 특정 유저의 전적 및 정보 가져오기
        /// </summary>
        public async Task<UserRecord> GetUserRecord(string userId)
        {
            if (!IsInitialized) return null;

            try
            {
                var response = await _supabase.From<UserRecord>().Where(x => x.UserId == userId).Single();
                return response;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseManager] 유저 데이터 조회 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 유저 데이터 신규 생성 또는 기존 데이터 덮어쓰기 (승패 기록 갱신 등)
        /// </summary>
        public async Task<bool> SaveOrUpdateUserRecord(UserRecord record)
        {
            if (!IsInitialized) return false;

            try
            {
                await _supabase.From<UserRecord>().Upsert(record);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseManager] 유저 데이터 저장/업데이트 실패: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region [ 2. 대기실/방 데이터 관리 (RoomData) ]

        /// <summary>
        /// 현재 게임 대기 중인(활성화된) 방 목록 가져오기 (Lobby UI 갱신용)
        /// </summary>
        public async Task<List<RoomData>> GetActiveRooms()
        {
            if (!IsInitialized) return new List<RoomData>();

            try
            {
                var response = await _supabase.From<RoomData>().Where(x => x.IsActive == true).Get();
                return response.Models;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseManager] 활성 방 목록 조회 실패: {ex.Message}");
                return new List<RoomData>();
            }
        }

        /// <summary>
        /// 새로운 방 생성 (방장이 '방 만들기' 클릭 시)
        /// </summary>
        public async Task<bool> CreateRoom(RoomData newRoom)
        {
            if (!IsInitialized) return false;

            try
            {
                await _supabase.From<RoomData>().Insert(newRoom);
                Debug.Log($"[DatabaseManager] '{newRoom.RoomName}' 방 생성 완료");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseManager] 방 생성 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 방 상태 업데이트 (인원 변동, 3D 인게임 진입 시 상태 변경 등)
        /// </summary>
        public async Task<bool> UpdateRoomStatus(RoomData roomToUpdate)
        {
            if (!IsInitialized) return false;

            try
            {
                await _supabase.From<RoomData>().Update(roomToUpdate);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseManager] 방 상태 업데이트 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 방 삭제 (방장이 방을 나가거나 게임이 완전히 끝났을 때)
        /// </summary>
        public async Task<bool> DeleteRoom(string roomId)
        {
            if (!IsInitialized) return false;

            try
            {
                await _supabase.From<RoomData>().Where(x => x.Id == roomId).Delete();
                Debug.Log($"[DatabaseManager] 방 삭제 완료 (ID: {roomId})");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseManager] 방 삭제 실패: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region [ 3. 전투 결과 관리 (CombatRecord) ]

        /// <summary>
        /// 보스 레이드 종료 후 전투 결과/데미지/보상 등 기록
        /// </summary>
        public async Task<bool> SaveCombatRecord(CombatRecord record)
        {
            if (!IsInitialized) return false;

            try
            {
                await _supabase.From<CombatRecord>().Insert(record);
                Debug.Log("[DatabaseManager] 전투(레이드) 결과 저장 완료");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseManager] 전투 결과 저장 실패: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}
