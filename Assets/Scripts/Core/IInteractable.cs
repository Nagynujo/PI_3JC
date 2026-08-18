using UnityEngine;

public interface IInteractable
{

    void Interact(PlayerController player);

    bool RequiresHold { get; }

    float HoldDuration { get; }

    bool CanStartHold(PlayerController player);

    void OnHoldStart(PlayerController player);

    void OnHoldProgress(PlayerController player, float progress);

    void OnHoldComplete(PlayerController player);

    void OnHoldCancelled(PlayerController player);
}