using UnityEngine;

public class PlayerFollowMouse : MonoBehaviour
{
    public float speed = 5f;
    public bool canMove = true;

    void OnEnable()
    {
        CursorInputRouter.Instance.Held += HandleCursorHeld;
    }

    void OnDisable()
    {
        if (!CursorInputRouter.HasInstance)
        {
            return;
        }

        CursorInputRouter.Instance.Held -= HandleCursorHeld;
    }

    void HandleCursorHeld(Vector3 worldPosition)
    {
        if (!canMove || ScoreManager.CurrentPhase != ScoreManager.GamePhase.Painting)
        {
            return;
        }

        worldPosition.z = 0f;
        transform.position = Vector2.Lerp(transform.position, worldPosition, speed * Time.deltaTime);
    }
}

