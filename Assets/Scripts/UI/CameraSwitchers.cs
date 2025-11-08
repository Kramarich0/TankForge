using UnityEngine;
using Unity.Cinemachine;

public class CameraSwitcher : MonoBehaviour
{
    public CinemachineCamera[] cameras; // массив всех камер
    private int currentCamIndex = 0;

    void Start()
    {
        // В начале включаем только первую камеру
        for (int i = 0; i < cameras.Length; i++)
            cameras[i].Priority = (i == 0) ? 10 : 0;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V)) // кнопка для переключения
        {
            // выключаем текущую камеру
            cameras[currentCamIndex].Priority = 0;

            // выбираем следующую камеру
            currentCamIndex++;
            if (currentCamIndex >= cameras.Length) currentCamIndex = 0;

            // включаем следующую камеру
            cameras[currentCamIndex].Priority = 10;
        }
    }
}
