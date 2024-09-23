using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

public class UIManager : MonoBehaviour
{
    private Camera arCamera;

    [SerializeField] private TMP_Text coordinatesText;
    [SerializeField] private GameObject coordinatePanel;

    [SerializeField] private LayerMask raycastMask;
    [SerializeField] private XRInputValueReader<Vector2> tapStartPointInput;
    Vector2 tapStartPoint;

    bool m_IsPerformed;
    bool m_WasPerformedThisFrame;
    bool m_WasCompletedThisFrame;

    private void Awake()
    {
        arCamera = Camera.main;
    }
    // Start is called before the first frame update
    void Start()
    {
        ShowCoordinates(new Vector3(0,1,0));
    }

    // Update is called once per frame
    void Update()
    {
        var prevPerformed = m_IsPerformed;

        var prevTapStartPosition = tapStartPoint;
        var tapPerformedThisFrame = tapStartPointInput.TryReadValue(out tapStartPoint) && prevTapStartPosition != tapStartPoint;

        m_IsPerformed = tapPerformedThisFrame;
        m_WasPerformedThisFrame = !prevPerformed && m_IsPerformed;
        //m_WasCompletedThisFrame = prevPerformed && !m_IsPerformed;

        //If the user has performed a tap, check if the tap was on a UI element
        if (m_WasPerformedThisFrame)
        {
            Debug.Log("Tap performed");
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                DetectTap(tapStartPoint);
            }
        }


    }

    // Method to detect tap on hexagon
    private void DetectTap(Vector2 touch)
    {
        // Create a ray from the camera to where the user tapped
        Ray ray = arCamera.ScreenPointToRay(touch);

        // Perform the raycast and check if it hits this hexagon's collider
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, raycastMask))
        {
            // Show the coordinates of the hexagon
            ShowCoordinates(hit.transform.position);
        }
    }

    public void ShowCoordinates(Vector3 position)
    {
        StopAllCoroutines();
        coordinatesText.text = $"Coordinates: ({position.x:F2}, {position.y:F2}, {position.z:F2})";
        StartCoroutine(ShowCoordinatesPanel());
    }

    private IEnumerator ShowCoordinatesPanel()
    {
        coordinatePanel.SetActive(true);
        yield return new WaitForSeconds(3f);
        coordinatePanel.SetActive(false);
    }

}
