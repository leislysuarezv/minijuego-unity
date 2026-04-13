using System;
using System.Collections;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public enum GamePhase
    {
        Painting,
        Finish,
        Results
    }

    public static GamePhase CurrentPhase { get; private set; } = GamePhase.Painting;
    public static event Action<GamePhase> PhaseChanged;

    public int score = 0;
    public int maxScore = 200;

    public GameObject worldScoreGroup;
    public TextMesh mainText;
    public TextMesh shadowText;
    public GameObject finalText;
    public Camera mainCamera;
    public CameraFollow cameraFollow;
    public PlayerFollowMouse playerMovement;
    public Animator playerAnimator;

    public Vector3 finalCamPosition = new Vector3(0, 0, -10);
    public float finalCamSize = 12f;

    private bool hasStartedFinalSequence;

    void Start()
    {
        SetPhase(GamePhase.Painting);

        if (worldScoreGroup != null)
            worldScoreGroup.SetActive(false);

        if (finalText != null)
            finalText.SetActive(false);
    }

    public void AddScore(int amount)
    {
        if (CurrentPhase != GamePhase.Painting)
            return;

        score += amount;
    }

    public void ShowFinalScore()
    {
        if (hasStartedFinalSequence)
            return;

        // The finish state begins as soon as painting is no longer allowed.
        hasStartedFinalSequence = true;
        SetPhase(GamePhase.Finish);

        CursorInputRouter.Instance.ForceRelease();
        AudioManager.Instance.PlayFinishSound();

        StartCoroutine(ShowFinalSequence());
    }

    IEnumerator ShowFinalSequence()
    {
        if (playerMovement != null)
            playerMovement.canMove = false;

        if (playerAnimator != null)
            playerAnimator.enabled = false;

        if (cameraFollow != null)
            cameraFollow.followPlayer = false;

        if (mainCamera != null)
        {
            mainCamera.transform.position = finalCamPosition;
            mainCamera.orthographicSize = finalCamSize;
        }

        yield return new WaitForSeconds(0.5f);

        if (finalText != null)
            finalText.SetActive(true);

        yield return new WaitForSeconds(1.5f);

        if (finalText != null)
            finalText.SetActive(false);

        if (worldScoreGroup != null)
            worldScoreGroup.SetActive(true);

        float percentage = ((float)score / maxScore) * 100f;
        percentage = Mathf.Clamp(percentage, 0f, 100f);

        string finalScore = "Score: " + percentage.ToString("F0") + "%";

        if (mainText != null)
            mainText.text = finalScore;

        if (shadowText != null)
            shadowText.text = finalScore;

        SetPhase(GamePhase.Results);
        AudioManager.Instance.PlayResultsSound();

        Debug.Log("Score: " + percentage + "%");
    }

    void SetPhase(GamePhase newPhase)
    {
        CurrentPhase = newPhase;

        if (PhaseChanged != null)
            PhaseChanged.Invoke(CurrentPhase);
    }
}

