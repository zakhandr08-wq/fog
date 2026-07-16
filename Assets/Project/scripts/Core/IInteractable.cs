public interface IInteractable
{
    string GetInteractionPrompt();
    float GetInteractionTime();
    bool CanInteract();
    void OnInteractionStart();
    void OnInteractionComplete();
    void OnInteractionCancel();
}