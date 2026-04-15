using UnityEngine;
using System.Collections.Generic;

public class LobbyManager_Test : MonoBehaviour
{
    // This class contains test functions for the LobbyManager that are not used in the actual game.

    // Test function to create a room
    public void CreateRoom(string roomName)
    {
        Debug.Log("Creating room: " + roomName);
        // Logic to create a room goes here
    }

    // Test function to get the list of rooms
    public List<string> GetRoomList()
    {
        Debug.Log("Fetching room list...");
        // Logic to fetch room list goes here
        return new List<string> { "Room1", "Room2", "Room3" };
    }

    // Test function to join a room
    public void JoinRoom(string roomName)
    {
        Debug.Log("Joining room: " + roomName);
        // Logic to join a room goes here
    }
}