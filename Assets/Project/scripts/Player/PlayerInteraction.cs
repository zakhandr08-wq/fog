using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactableLayer;

    // State
    private IInteractable currentTarget;
    private IInteractable activeInteraction;
    private float interactionProgress;
    private bool isInteracting;

    // Events (UI will subscribe to these)
    public System.Action<string> OnPromptChanged;
    public System.Action<float> OnProgressChanged;
    public System.Action OnInteractionStopped;

    private Camera playerCamera;

    private void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        if (isInteracting)
        {
            HandleActiveInteraction();
        }
        else
        {
            ScanForInteractable();
            TryStartInteraction();
        }

        // Cancel interaction
        if (isInteracting && !Input.GetKey(KeyCode.E))
        {
            CancelInteraction();
        }
    }

    private void ScanForInteractable()
    {
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.Raycast(
            ray, out RaycastHit hit,
            interactionRange, interactableLayer))
        {
            var interactable =
                hit.collider.GetComponent<IInteractable>();

            if (interactable != null && interactable.CanInteract())
            {
                if (currentTarget != interactable)
                {
                    currentTarget = interactable;
                    OnPromptChanged?.Invoke(
                        interactable.GetInteractionPrompt()
                    );
                }
                return;
            }
        }

        if (currentTarget != null)
        {
            currentTarget = null;
            OnPromptChanged?.Invoke(null);
        }
    }

    private void TryStartInteraction()
    {
        if (currentTarget != null && Input.GetKeyDown(KeyCode.E))
        {
            activeInteraction = currentTarget;
            isInteracting = true;
            interactionProgress = 0f;
            activeInteraction.OnInteractionStart();
        }
    }

    private void HandleActiveInteraction()
    {
        float duration = activeInteraction.GetInteractionTime();

        if (duration <= 0f)
        {
            // Instant interaction
            activeInteraction.OnInteractionComplete();
            StopInteraction();
            return;
        }

        interactionProgress += Time.deltaTime;
        OnProgressChanged?.Invoke(
            interactionProgress / duration
        );

        if (interactionProgress >= duration)
        {
            activeInteraction.OnInteractionComplete();
            StopInteraction();
        }
    }

    private void CancelInteraction()
    {
        if (activeInteraction != null)
        {
            activeInteraction.OnInteractionCancel();
        }
        StopInteraction();
    }

    private void StopInteraction()
    {
        isInteracting = false;
        activeInteraction = null;
        interactionProgress = 0f;
        OnInteractionStopped?.Invoke();
    }
}