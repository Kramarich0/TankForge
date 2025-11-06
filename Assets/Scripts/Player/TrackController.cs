using UnityEngine;

public class TrackController : MonoBehaviour
{
    public Transform tankTransform; 
    public float scrollSpeedFactor = 10.0f; 
    public bool useWorldMovement = true; 

    public Renderer[] trackRenderers; 
    public string textureProperty = "_MainTex"; 

    public Transform[] leftWheels;   
    public Transform[] rightWheels;  
    public float wheelRadius = 0.35f; 

    
    private Vector3 lastPosition;
    private Vector2[] currentOffsets;

    void Start()
    {
        if (tankTransform == null) tankTransform = transform;
        lastPosition = tankTransform.position;

        
        if (trackRenderers != null && trackRenderers.Length > 0)
        {
            currentOffsets = new Vector2[trackRenderers.Length];
            for (int i = 0; i < trackRenderers.Length; i++)
            {
                var mat = trackRenderers[i].material;
                currentOffsets[i] = mat.mainTextureOffset;
            }
        }
    }

    void Update()
    {
        if (tankTransform == null) return;

        
        Vector3 deltaPos = tankTransform.position - lastPosition;
        
        float forwardMovement = Vector3.Dot(tankTransform.forward, deltaPos);

        
        if (trackRenderers != null && trackRenderers.Length > 0)
        {
            for (int i = 0; i < trackRenderers.Length; i++)
            {
                
                
                float scrollAmount = forwardMovement * scrollSpeedFactor;
                float noise = Mathf.PerlinNoise(Time.time * 2f, 0f) * 0.01f;
                currentOffsets[i].y += scrollAmount + noise;
                trackRenderers[i].material.mainTextureOffset = currentOffsets[i];
            }
        }

        
        float distance = forwardMovement; 
        float circumference = 2f * Mathf.PI * Mathf.Max(0.0001f, wheelRadius);
        float rotationDegrees = distance / circumference * 360f;

        if (leftWheels != null)
        {
            foreach (var w in leftWheels)
            {
                if (w != null) w.Rotate(Vector3.right, rotationDegrees, Space.Self);
            }
        }
        if (rightWheels != null)
        {
            foreach (var w in rightWheels)
            {
                if (w != null) w.Rotate(Vector3.right, rotationDegrees, Space.Self);
            }
        }

        lastPosition = tankTransform.position;
    }

    void OnDestroy()
    {
        
        
    }
}
