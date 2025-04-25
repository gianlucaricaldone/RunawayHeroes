using UnityEngine;

public class LoadingIconRotator : MonoBehaviour {
    public float rotationSpeed = 90f; // Gradi al secondo
    
    void Update() {
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }
}