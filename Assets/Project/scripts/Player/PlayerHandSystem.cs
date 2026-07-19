using UnityEngine;

public class PlayerHandSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Transform handPoint;

    [Header("Hand Sway")]
    [SerializeField] private float swayAmount = 0.02f;
    [SerializeField] private float swaySpeed = 6f;
    [SerializeField] private float tiltAmount = 15f;
    [SerializeField] private float tiltSpeed = 8f;
    [SerializeField] private float bobWhileWalking = 0.01f;
    [SerializeField] private float bobSpeed = 10f;

    // Sway state
    private Vector3 handBasePosition;
    private float swayTimer;

    [Header("Hand Models")]
    [SerializeField] private GameObject torchPrefab;
    [SerializeField] private GameObject knifePrefab;

    [Header("Torch Settings")]
    [SerializeField] private float torchDuration = 180f;
    [SerializeField] private float torchLightRange = 10f;
    [SerializeField] private float torchLightIntensity = 2f;
    [SerializeField]
    private Color torchColor =
        new Color(1f, 0.6f, 0.2f);
    [SerializeField] private float flickerSpeed = 8f;
    [SerializeField] private float flickerAmount = 0.3f;

    // State
    private int selectedSlot = -1;
    private GameObject currentHandObject;
    private Light torchLight;
    private float torchTimer;
    private bool isTorchActive;
    private float baseIntensity;

    // Properties
    public int SelectedSlot => selectedSlot;
    public bool HasTorch => isTorchActive;

    private void Start()
    {
        if (inventory == null)
            inventory = GetComponent<PlayerInventory>();

        if (handPoint != null)
            handBasePosition = handPoint.localPosition;
    }

    private void Update()
    {
        HandleSlotInput();
        UpdateTorch();
        UpdateFlicker();
        UpdateHandSway();
    }

    private void UpdateHandSway()
    {
        if (handPoint == null) return;
        if (currentHandObject == null) return;

        // Mouse movement sway
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Position sway (hand lags behind mouse)
        Vector3 targetPos = handBasePosition;
        targetPos.x -= mouseX * swayAmount;
        targetPos.y -= mouseY * swayAmount;

        // Walking bob
        var controller = GetComponent<CharacterController>();
        if (controller != null && controller.isGrounded
            && controller.velocity.magnitude > 0.5f)
        {
            swayTimer += Time.deltaTime * bobSpeed;
            targetPos.y += Mathf.Sin(swayTimer)
                * bobWhileWalking;
            targetPos.x += Mathf.Cos(swayTimer * 0.5f)
                * bobWhileWalking * 0.5f;
        }

        handPoint.localPosition = Vector3.Lerp(
            handPoint.localPosition,
            targetPos,
            Time.deltaTime * swaySpeed);

        // Rotation tilt (follows look direction)
        float tiltX = -mouseY * tiltAmount;
        float tiltY = mouseX * tiltAmount;

        Quaternion targetRot = Quaternion.Euler(
            tiltX, tiltY, 0f);

        handPoint.localRotation = Quaternion.Slerp(
            handPoint.localRotation,
            targetRot,
            Time.deltaTime * tiltSpeed);
    }

    private void HandleSlotInput()
    {
        int newSlot = -1;

        if (Input.GetKeyDown(KeyCode.Alpha1)) newSlot = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) newSlot = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) newSlot = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) newSlot = 3;

        if (newSlot < 0) return;

        // Toggle if same slot
        if (newSlot == selectedSlot)
        {
            Deselect();
            return;
        }

        SelectSlot(newSlot);
    }

    public void SelectSlot(int slot)
    {
        if (slot < 0 || slot >= inventory.MainInventory.Length)
            return;

        ClearHand();
        selectedSlot = slot;

        var slotData = inventory.MainInventory[slot];

        if (slotData.IsEmpty)
        {
            selectedSlot = -1;
            return;
        }

        string itemId = slotData.item.itemId;

        switch (itemId)
        {
            case "torch":
                SpawnHandItem(torchPrefab, true);
                break;
            case "knife":
                SpawnHandItem(knifePrefab, false);
                break;
            default:
                Debug.Log($"Выбрано: {slotData.item.itemName}");
                break;
        }
    }

    private void Deselect()
    {
        ClearHand();
        selectedSlot = -1;
    }

    private void SpawnHandItem(
        GameObject prefab, bool isTorch)
    {
        if (prefab == null || handPoint == null) return;

        currentHandObject = Instantiate(prefab, handPoint);
        currentHandObject.transform.localPosition =
            Vector3.zero;
        currentHandObject.transform.localRotation =
            Quaternion.identity;

        if (isTorch)
        {
            SetupTorch();
        }
    }

    private void SetupTorch()
    {
        isTorchActive = true;
        torchTimer = torchDuration;

        Transform firePoint = currentHandObject.transform.Find("FirePoint");

        if (firePoint == null)
        {
            Debug.LogWarning("FirePoint not found on torch prefab, using root");
            firePoint = currentHandObject.transform;
        }

        torchLight = currentHandObject.GetComponentInChildren<Light>();

        if (torchLight == null)
        {
            var lightObj = new GameObject("TorchLight");
            lightObj.transform.SetParent(firePoint);
            lightObj.transform.localPosition = Vector3.zero;

            torchLight = lightObj.AddComponent<Light>();
            torchLight.type = LightType.Point;
        }
        else
        {
            torchLight.transform.SetParent(firePoint);
            torchLight.transform.localPosition = Vector3.zero;
        }

        torchLight.type = LightType.Point;
        torchLight.range = torchLightRange;
        torchLight.intensity = torchLightIntensity;
        torchLight.color = torchColor;

        baseIntensity = torchLightIntensity;
    }

    private void UpdateTorch()
    {
        if (!isTorchActive) return;

        torchTimer -= Time.deltaTime;

        if (torchTimer < 30f)
        {
            float t = torchTimer / 30f;
            baseIntensity = torchLightIntensity * t;
            torchLight.range = torchLightRange * t;
        }

        if (torchTimer <= 0f)
        {
            BurnOutTorch();
        }
    }

    private void UpdateFlicker()
    {
        if (!isTorchActive || torchLight == null) return;

        float noise = Mathf.PerlinNoise(
            Time.time * flickerSpeed, 0f);
        torchLight.intensity = baseIntensity
            + (noise - 0.5f) * flickerAmount * baseIntensity;
    }

    private void BurnOutTorch()
    {
        Debug.Log("Факел сгорел!");
        isTorchActive = false;

        if (selectedSlot >= 0)
        {
            var slot = inventory.MainInventory[selectedSlot];
            if (!slot.IsEmpty)
            {
                inventory.RemoveItem(slot.item, 1);
            }
        }

        ClearHand();
        selectedSlot = -1;
    }

    private void ClearHand()
    {
        if (currentHandObject != null)
        {
            Destroy(currentHandObject);
            currentHandObject = null;
        }

        isTorchActive = false;
        torchLight = null;
    }

    public float GetTorchTimeRemaining()
    {
        return isTorchActive ? torchTimer : 0f;
    }
}