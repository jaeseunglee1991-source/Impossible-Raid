using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class SecurePlayerPrefs
{
    // 🚨 주의: 실제 출시 시점에는 이 키값(32바이트)과 IV값(16바이트)을 
    // 본인만의 무작위 영문/숫자로 반드시 변경하세요! (현재는 임시 예시입니다)
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("YourVerySecretKey123456789012345"); 
    private static readonly byte[] IV = Encoding.UTF8.GetBytes("YourSecretIV1234");

    // 문자열 암호화 저장
    public static void SetString(string key, string value)
    {
        string encryptedValue = Encrypt(value);
        PlayerPrefs.SetString(key, encryptedValue);
    }

    // 문자열 복호화 불러오기
    public static string GetString(string key, string defaultValue = "")
    {
        string encryptedValue = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(encryptedValue)) return defaultValue;
        try 
        { 
            return Decrypt(encryptedValue); 
        } 
        catch 
        { 
            // 해커가 파일을 임의로 변조해서 복호화에 실패한 경우
            Debug.LogWarning("로컬 세이브 데이터 변조 감지!");
            return defaultValue; 
        }
    }

    // 오프라인 보상용 시간(DateTime) 저장 편의 함수
    public static void SetDateTime(string key, DateTime value)
    {
        SetString(key, value.ToString("o")); // 표준 시간 포맷으로 저장
        PlayerPrefs.Save(); // 즉시 디스크에 기록
    }

    // 오프라인 보상용 시간(DateTime) 불러오기 편의 함수
    public static DateTime GetDateTime(string key, DateTime defaultValue)
    {
        string timeStr = GetString(key, "");
        if (DateTime.TryParse(timeStr, out DateTime result)) return result;
        return defaultValue;
    }

    // --- 내부 AES 암호화/복호화 엔진 ---
    private static string Encrypt(string plainText)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Key; aes.IV = IV;
            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
            return Convert.ToBase64String(encryptedBytes);
        }
    }

    private static string Decrypt(string cipherText)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Key; aes.IV = IV;
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            byte[] inputBytes = Convert.FromBase64String(cipherText);
            byte[] decryptedBytes = decryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}
