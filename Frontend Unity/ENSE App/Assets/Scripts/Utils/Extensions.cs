using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Utils
{
  public static class Extensions
  {
    
    public static float LocalPitch(this Transform t)
    {
      var parent = Vector3.up;
      var local = t.forward;

      if (t.parent != null)
      {
        parent = t.parent.up;
        local = t.parent.InverseTransformDirection(local);
      }
      return 90 - Vector3.Angle(parent, local);
    }

    static Quaternion GetQuaternion(this Matrix4x4 m)
    {
      // Source: unity-arkit-plugin/.../UnityARMatrixOps.cs
      // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
      Quaternion q = new Quaternion();
      q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
      q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
      q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
      q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
      q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
      q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
      q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
      return q;
    }

    public static Pose GetPose(this Matrix4x4 matrix)
    {
      return new Pose(matrix.GetPosition(), matrix.GetQuaternion());
    }

    static Vector3 GetPosition(this Matrix4x4 matrix)
    {
      return (Vector3)(matrix.GetColumn(3));
    }

    public static Promise<T> Fetch<T>(this MonoBehaviour self, string url, bool wrapWithContainer = false) where T : class
    {
      Debug.Log($"fetching `{url}`");
      if (wrapWithContainer)
      {
        return new Promise<T>(
          (resolve, reject) =>
            self.StartCoroutine(
              Fetch<JSONContainer<T>>(
                container => resolve(container.result),
                reject,
                url
              )
            )
        );
      }
      return new Promise<T>((resolve, reject) => self.StartCoroutine(Fetch(resolve, reject, url)));
    }

    public static IEnumerator Fetch<T>(Action<T> resolve, Action<Exception> reject, string url)
    {
      using (var request = UnityWebRequest.Get(url))
      {
        yield return request.SendWebRequest();

        request.handleResponse(resolve, reject);
      }

      Debug.Log($"Done fetching `{url}`");
    }

    public static IEnumerator UploadToWebService<T>(Action<T> resolve, Action<Exception> reject, byte[] texture, string url, string message)
    {
      Debug.Log($"Uploading: {url}");
      var formData = new WWWForm();
      formData.AddField("message", message);
      formData.AddBinaryData("file", contents:texture, "upload.jpg","image/jpeg" );
      using (var request = UnityWebRequest.Post(url, formData))
      {
        yield return request.SendWebRequest();

        request.handleResponse(resolve, reject);
      }
    }
    
    private static void handleResponse<T>(this UnityWebRequest request, Action<T> resolve, Action<Exception> reject)
    {
      if (request.isNetworkError || (request.isHttpError && (request.downloadHandler.text == null || request.downloadHandler.text.Length == 0)))
      {
        Debug.LogError($"handleResponse: {request.error}");

        reject(new Exception(request.error));
        return;
      }

      var txt = request.downloadHandler.text;

      try
      {
        var result = JsonUtility.FromJson<ColMapError>(txt);

        if (!string.IsNullOrEmpty(result.error))
        {
          reject(new Exception(result.error));
          return;
        }
      }
      catch (Exception)
      {
        // ignore error since it seems there is no error from backend
      }

      try
      {
        var lookup = JsonUtility.FromJson<T>(txt);
        resolve(lookup);
      }
      catch (Exception e)
      {
        reject(e);
      }
    }

    async public static Task<string> URLTo(this Promise<DynDN5Lookup> self, string path, Dictionary<string, string> args = null)
    {
      var lookup = await self;

      if (string.IsNullOrEmpty(lookup.colmap?.ip))
      {
        return null;
      }

      var builder = new UriBuilder()
      {
        Scheme = "http",
        Host = lookup.colmap.ip,
        Port = lookup.colmap.port,
        Path = path
      };

      var locationData = new Dictionary<string, string>();

      if (Input.location.status == LocationServiceStatus.Running)
      {
        locationData = new Dictionary<string, string>()
        {
          { "lat" , Input.location.lastData.latitude.ToString() },
          { "long" , Input.location.lastData.longitude.ToString()},
          { "horAcc" , Input.location.lastData.horizontalAccuracy.ToString()},
          { "altitude" , Input.location.lastData.altitude.ToString()},
          { "verAcc" , Input.location.lastData.verticalAccuracy.ToString()},

          { "magneticHeading", Input.compass.magneticHeading.ToString() },
          { "trueHeading", Input.compass.trueHeading.ToString() },
          { "headAcc", Input.compass.headingAccuracy.ToString() },
        };
      }


      if (args == null || args.Count == 0) { args = locationData; }
      else { args = locationData.Concat(args).ToDictionary(x => x.Key, x => x.Value); }

      if (args != null)
      {
        // missing urlencode for key and value
        builder.Query = String.Join("&", System.Linq.Enumerable.Select(args, pair => $"{pair.Key}={pair.Value}"));
      }

      return builder.ToString();
    }

    public static Vector3 ProgressAsScale(this float progress)
    {
      return new Vector3(progress, 1, 1);
    }

    public static Color WithAlpha(this Color color, float alpha)
    {
      color.a = alpha;
      return color;
    }

    public static float Curvate(this float self, AnimationCurve curve)
    {
      return curve.Evaluate(self);
    }
  }

  public class Promise<T> where T : class
  {
    private TaskCompletionSource<T> value = new TaskCompletionSource<T>();
    public Promise(Action<Action<T>, Action<Exception>> promise)
    {
      promise(resolve, reject);
    }
    private void reject(Exception obj)
    {
      value.SetException(obj);
    }

    private void resolve(T obj)
    {
      value.SetResult(obj);
    }

    public TaskAwaiter<T> GetAwaiter()
    {
      return value.Task.GetAwaiter();
    }

    public bool IsResolved => value.Task.IsCompleted && !value.Task.IsFaulted;
    public T Result => IsResolved ? value.Task.Result : null;
  }
  
}