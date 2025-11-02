using System.Collections;
using UnityEngine;

public interface IPlayerController
{
    void Initialize(Player playerModel, bool isLocal);
    IEnumerator PerformTurn();
}
