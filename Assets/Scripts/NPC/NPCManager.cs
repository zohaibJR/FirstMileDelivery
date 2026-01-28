using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    [System.Serializable]
    public class NPCSlot
    {
        public NPC npcPrefab;
        public Transform house;
    }

    public List<NPCSlot> npcs = new List<NPCSlot>();

    void Start()
    {
        foreach (var slot in npcs)
        {
            if (slot.npcPrefab != null && slot.house != null)
                SpawnNPC(slot);
        }
    }

    void SpawnNPC(NPCSlot slot)
    {
        NPC npc = Instantiate(slot.npcPrefab);
        npc.Init(slot.house, () =>
        {
            float delay = Random.Range(1f, 5f);
            StartCoroutine(RespawnAfterDelay(slot, delay));
        });
    }

    IEnumerator RespawnAfterDelay(NPCSlot slot, float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnNPC(slot);
    }
}
