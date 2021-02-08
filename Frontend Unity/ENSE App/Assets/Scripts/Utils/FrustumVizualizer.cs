using UnityEngine;

public class FrustumVizualizer : MonoBehaviour
{
#if UNITY_EDITOR
  /// <summary>
  /// Callback to draw gizmos that are pickable and always drawn.
  /// </summary>
  public Color color = Color.yellow;
  void OnDrawGizmos()
  {
    var tmp = Gizmos.matrix;

    var color = Gizmos.color;

    if (UnityEditor.Selection.activeGameObject == gameObject)
    {
      Gizmos.color = Color.red;
    }
    else
    {
      Gizmos.color = this.color;
    }

    Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
    Gizmos.DrawFrustum(Vector3.zero, 60, 1, 0, 9f / 16f);
    var tmp2 = Gizmos.matrix;

    for (var z = -2; z < 2; z++)
      for (var y = -2; y < 2; y++)
        for (var x = -2; x < 2; x++)
        {
          Gizmos.matrix = tmp2 * Matrix4x4.Translate(
            Vector3.right * x / 200 +
             Vector3.up * y / 200 +
             Vector3.forward * z / 200);
          Gizmos.DrawFrustum(Vector3.zero, 60, 1, 0, 9f / 16f);
        }

    Gizmos.matrix = tmp;

    Gizmos.color = Color.gray;
    Gizmos.DrawRay(transform.position, transform.forward * 5);
    var parentForward = transform.parent?.up ?? Vector3.up;
    Gizmos.DrawRay(transform.position, parentForward * 5);


    Gizmos.color = Color.black;

    tmp2 = tmp;

    // if (this.color.a == 0)
    //   for (var z = -2f; z < 2; z += 0.5f)
    //     for (var y = -2f; y < 2; y += 0.5f)
    //       for (var x = -2f; x < 2; x += 0.5f)
    //       {
    //         Gizmos.matrix = tmp2 * Matrix4x4.Translate(
    //           Vector3.right * x / 100 +
    //            Vector3.up * y / 100 +
    //            Vector3.forward * z / 100);
    //         for (var i = -1000; i < 1000; i += 10)
    //         {
    //           Gizmos.DrawLine(new Vector3(-1000, 0, i), new Vector3(1000, 0, i));
    //           Gizmos.DrawLine(new Vector3(i, 0, -1000), new Vector3(i, 0, 1000));
    //         }
    //       }


    Gizmos.color = color;
  }
#endif
}