using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class OoB : MonoBehaviour
{
    [Tooltip("Tag used to identify the player.")] 
    public string playerTag = "Player";

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnValidate()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        var playerStats = other.GetComponent<PlayerStats>()
                          ?? other.GetComponentInParent<PlayerStats>()
                          ?? other.transform.root.GetComponent<PlayerStats>();

        if (playerStats == null)
        {
            Debug.LogWarning($"{name}: Player entered trigger but no PlayerStats found on the colliding object or its parents.");
            return;
        }

        SetPlayerHealthToMinusOne(playerStats);
    }

    private static void SetPlayerHealthToMinusOne(PlayerStats playerStats)
    {
        if (playerStats == null) return;

        Type t = playerStats.GetType();

        try
        {
            // 1) Try to set the private 'currentHealth' field to -1 (PlayerStats uses private float currentHealth)
            var field = t.GetField("currentHealth", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
            {
                if (field.FieldType == typeof(float))
                {
                    field.SetValue(playerStats, -1f);
                }
                else if (field.FieldType == typeof(double))
                {
                    field.SetValue(playerStats, -1.0);
                }
                else if (field.FieldType == typeof(int))
                {
                    field.SetValue(playerStats, -1);
                }
            }

            // 2) Invoke OnHealthChanged UnityEvent<float> if present so UI/listeners update
            var onHealthField = t.GetField("OnHealthChanged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (onHealthField != null)
            {
                var onHealthObj = onHealthField.GetValue(playerStats);
                if (onHealthObj is UnityEvent<float> onHealthEvent)
                {
                    onHealthEvent.Invoke(-1f);
                }
                else
                {
                    // fallback: try invoking via reflection in case the event type differs
                    var invokeMethod = onHealthObj?.GetType().GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public);
                    invokeMethod?.Invoke(onHealthObj, new object[] { -1f });
                }
            }

            // 3) Invoke OnDeath UnityEvent if present (health is now < 0)
            var onDeathField = t.GetField("OnDeath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (onDeathField != null)
            {
                var onDeathObj = onDeathField.GetValue(playerStats);
                if (onDeathObj is UnityEvent onDeathEvent)
                {
                    onDeathEvent.Invoke();
                }
                else
                {
                    var invokeMethod = onDeathObj?.GetType().GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public);
                    invokeMethod?.Invoke(onDeathObj, null);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"OoB: failed to force player health to -1 via reflection: {ex.Message}");
        }
    }
}
