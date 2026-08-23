public interface IInteractable
{
    string GetInteractPrompt();

    void Interact(PlayerController player);
}