using UnityEngine;

  public class CameraFollow : MonoBehaviour
  {
      public Transform target;         // Il transform del player da seguire
      public Vector3 offset = new Vector3(0, 3, -5);  // Offset rispetto al player
      public float smoothSpeed = 5f;   // Velocità di smussamento del movimento

      private void LateUpdate()
      {
          if (target == null)
          {
              // Cerca automaticamente il player se non impostato
              var playerObj = GameObject.FindGameObjectWithTag("Player");
              if (playerObj != null)
                  target = playerObj.transform;
              else
                  return;
          }

          // Calcola la posizione target
          Vector3 desiredPosition = target.position + offset;

          // Interpola verso la posizione target per un movimento fluido
          Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition,
                                                smoothSpeed * Time.deltaTime);

          // Applica la posizione
          transform.position = smoothedPosition;

          // Guarda verso il player
          transform.LookAt(target.position + Vector3.up);
      }
  }