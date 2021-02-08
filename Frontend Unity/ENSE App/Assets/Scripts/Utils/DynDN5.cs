using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class DynDN5Lookup
{
  public DynDN5Entry colmap;
}

[Serializable]
public class DynDN5Entry
{
  public string ip;
  public string ipv6;
  public int port;
}

public class JSONContainer<T>
{
  public T result;
}

[Serializable]
public struct ColMapError
{
  public string error;
}

[Serializable]
public struct ColMapPoint3D
{

  [SerializeField] private float[] c;
  [SerializeField] private long i;
  [SerializeField] private float[] p;

  public ColMapPoint3D(float[] c = null, long i = 0, float[] p = null)
  {
    this.c = c ?? new float[0];
    this.i = i;
    this.p = p ?? new float[0];
  }

  public long id => i;
  public Color color => new Color(c[0] / 255f, c[1] / 255f, c[2] / 255f, 1);
  public Vector3 position => new Vector3(p[0], -p[1], p[2]);
}

[Serializable]
public struct ColMapImage
{
  [SerializeField]
  private string name;
  [SerializeField]
  private float[] position;
  [SerializeField]
  private float[] rotation;

  public ColMapImage(string name = "", float[] position = null, float[] rotation = null)
  {
    this.name = name;
    this.position = position ?? new float[3];
    this.rotation = rotation ?? new float[4];
  }

  public string Name => name;
  public Vector3 Position => new Vector3(position[0], position[1], position[2]);
  public Quaternion Rotation => new Quaternion(
    rotation[0],
    rotation[1],
    rotation[2],
    rotation[3]);
}

[Serializable]
public class ColMapResult
{
  public string name;
  [SerializeField] private float[] position = new float[3];
  [SerializeField] private float[] rotation = new float[4];

  public Vector3 Position
  {
    get =>
      new Vector3(
        position[0],
        position[1],
        position[2]
      );
  }

  public Quaternion Rotation => new Quaternion(
      rotation[1],
      rotation[2],
      rotation[3],
      rotation[0]
    );
}

[Serializable]
public class ColMapPlace : IEquatable<ColMapPlace>
{
  public string name;
  public ColMapLocation location;

  public override string ToString()
  {
    return $"{name} ({location})";
  }

  public bool Equals(ColMapPlace other)
  {
    return name.Equals(other.name) && location.Equals(other.location);
  }

  public Dictionary<string, string> AsQueryDictionary => new Dictionary<string, string> {
    { "name",  name }
  };
}

[Serializable]
public class ColMapLocation : IEquatable<ColMapLocation>
{
  [SerializeField] private double lat = 0;
  [SerializeField] private double @long = 0;

  public double Latitude => lat;
  public double Longitude => @long;

  public Dictionary<string, string> AsQueryDictionary => new Dictionary<string, string> {
    { "lat",  lat.ToString()   },
    { "long", @long.ToString() }
  };

  public override string ToString()
  {
    return $"{Latitude}, {Longitude}";
  }

  public float DistanceTo(double otherLat, double otherLong)
  {
    // Adapted from http://www.movable-type.co.uk/scripts/latlong.html
    var earthRadius = 6371e3f;
    var thisLatRad = (float)this.lat * Mathf.Deg2Rad;
    var thisLongRad = (float)this.@long * Mathf.Deg2Rad;

    var otherLatRad = (float)otherLat * Mathf.Deg2Rad;
    var otherLongRad = (float)otherLong * Mathf.Deg2Rad;
    var deltaLat = otherLatRad - thisLatRad;
    var deltaLong = otherLongRad - thisLongRad;

    var a = Mathf.Sin(deltaLat / 2) * Mathf.Sin(deltaLat / 2)
          + Mathf.Cos(thisLatRad) * Mathf.Cos(otherLatRad)
          * Mathf.Sin(deltaLong / 2) * Mathf.Sin(deltaLong / 2);
    var c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
    var distance = earthRadius * c;

    return distance;
  }

  public bool Equals(ColMapLocation other)
  {
    return Math.Abs(lat - other.lat) < 1.0e-8 && Math.Abs(@long - other.@long) < 1.0e-8;
  }
}