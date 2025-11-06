using UnityEngine;
using UnityEngine.UI;

public class CrosshairAim : MonoBehaviour
{
    public Transform gunEnd; 
    public Camera mainCamera; 
    public LayerMask groundMask = 1; 
    public float maxDistance = 1000f;

    private Image crosshairImage;
    private RectTransform rectTransform;

    void Start()
    {
        crosshairImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        if (crosshairImage == null || rectTransform == null)
        {
            Debug.LogError("UI Image или RectTransform не найдены на Crosshair!");
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main; 
        }
    }

    void LateUpdate()
    {
        if (gunEnd == null || mainCamera == null)
        {
            return; 
        }

        Ray ray = new Ray(gunEnd.position, gunEnd.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance, groundMask))
        {
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(hit.point);

            if (screenPoint.z > 0)
            {
                rectTransform.position = screenPoint;
            }
         
        }
      
    }
}