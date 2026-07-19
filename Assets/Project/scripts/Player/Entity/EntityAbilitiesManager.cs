using UnityEngine;
using System.Collections.Generic;

public class EntityAbilitiesManager : MonoBehaviour
{
    [Header("Abilities (по кнопкам Q, W, E)")]
    [SerializeField] private EntityAbility abilityQ;
    [SerializeField] private EntityAbility abilityW;
    [SerializeField] private EntityAbility abilityE;

    [Header("Keys")]
    [SerializeField] private KeyCode keyQ = KeyCode.Q;
    [SerializeField] private KeyCode keyW = KeyCode.W;
    [SerializeField] private KeyCode keyE = KeyCode.E;

    public EntityAbility AbilityQ => abilityQ;
    public EntityAbility AbilityW => abilityW;
    public EntityAbility AbilityE => abilityE;

    private void Update()
    {
        // ќтладка Ч проверим что Update работает
        if (Input.GetKeyDown(keyQ))
        {
            Debug.Log("=== Q pressed ===");

            if (abilityQ == null)
            {
                Debug.LogError("AbilityQ is NULL!");
                return;
            }

            Debug.Log($"AbilityQ: {abilityQ.AbilityName}");
            bool result = abilityQ.TryUse();
            Debug.Log($"TryUse result: {result}");
        }

        if (Input.GetKeyDown(keyW))
        {
            Debug.Log("=== W pressed ===");

            if (abilityW == null)
            {
                Debug.LogError("AbilityW is NULL!");
                return;
            }

            abilityW.TryUse();
        }

        if (Input.GetKeyDown(keyE))
        {
            Debug.Log("=== E pressed ===");

            if (abilityE == null)
            {
                Debug.LogError("AbilityE is NULL!");
                return;
            }

            abilityE.TryUse();
        }
    }
}
