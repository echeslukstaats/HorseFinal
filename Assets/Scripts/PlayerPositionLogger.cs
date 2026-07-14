using UnityEngine;
using System;
using System.IO;
using System.Text;

public class PlayerPositionLogger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player's head (CenterEyeAnchor).")]
    public Transform playerHead;

    [Tooltip("The HorseFsm whose centerOfGravity is read.")]
    public HorseFsm horseFsm;

    [Header("Config")]
    [Tooltip("Sampling interval in seconds.")]
    public float samplingRate = 0.5f;

    private StreamWriter writer;
    private float timer = 0f;
    private string filePath;

    void Start()
    {
        if (!Validate()) { enabled = false; return; }

        string fileName = $"player_position_log_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        filePath = Path.Combine(Application.persistentDataPath, fileName);

        writer = new StreamWriter(filePath, append: false, Encoding.UTF8);
        writer.WriteLine("timestamp,player_x,player_y,player_z");
        writer.Flush();

        Debug.Log($"[PosLogger] Recording started → {filePath}");
    }

    void Update()
    {
        if (writer == null) return;

        timer += Time.deltaTime;
        if (timer < samplingRate) return;
        timer = 0f;

        Vector3 relativePos = horseFsm.centerOfGravity.InverseTransformPoint(playerHead.position);
        string line = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:o},{1:F4},{2:F4},{3:F4}", DateTime.UtcNow, relativePos.x, relativePos.y, relativePos.z);

        writer.WriteLine(line);
        writer.Flush(); // regular flush to avoid losing data if the app crashes
    }

    void OnApplicationQuit()
    {
        StopRecording();
    }

    private void StopRecording()
    {
        if (writer == null) return;
        writer.Flush();
        writer.Close();
        writer = null;
        Debug.Log($"[PosLogger] Recording stopped. File saved at {filePath}");
    }

    private bool Validate()
    {
        if (playerHead == null) { Debug.LogWarning("[PosLogger] playerHead not assigned."); return false; }
        if (horseFsm == null || horseFsm.centerOfGravity == null)
        {
            Debug.LogWarning("[PosLogger] horseFsm.centerOfGravity not assigned.");
            return false;
        }
        if (samplingRate <= 0f) { Debug.LogWarning("[PosLogger] samplingRate must be > 0 seconds."); return false; }
        return true;
    }
}