using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx.Logging;
using Jotunn.Utils;
using UnityEngine;

namespace Rcinel.Level;

/// <summary>
/// The files next to the plugin's DLL: the models made by <c>models/build.py</c> (decision 0009) and
/// the item icons rendered from <c>assets/items/*.svg</c>.
/// </summary>
/// <remarks>
/// Layout: <c>models/probe.obj</c>, <c>models/level-hoe.obj</c>, <c>models/atlas.png</c>,
/// <c>icons/probe-128.png</c>, <c>icons/level-hoe-128.png</c>. The csproj copies them so, and the
/// Thunderstore package uses the same layout. A missing or broken file is logged and leaves the game's
/// own model or icon in place; it never stops the plugin.
/// </remarks>
internal sealed class PluginFiles(string directory, ManualLogSource logger)
{
  public Mesh? LoadMesh(string name) => Load(Path.Combine(directory, "models", name), path => ObjMesh.Read(path, Path.GetFileNameWithoutExtension(path)));

  public Texture2D? LoadTexture(string folder, string name) => Load(Path.Combine(directory, folder, name), path =>
  {
    // Mipmaps on, so the coarse texture doesn't shimmer at a distance; bilinear like the game's own.
    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: true) { name = Path.GetFileNameWithoutExtension(path) };
    // Through Jotunn: Unity 6's ImageConversionModule is built against netstandard 2.1, which a net472
    // plugin can't reference, so Jotunn calls LoadImage by reflection.
    if (!AssetUtils.LoadImage(texture, File.ReadAllBytes(path)))
    {
      throw new InvalidDataException("not a PNG Unity can read");
    }

    texture.wrapMode = TextureWrapMode.Clamp;
    return texture;
  });

  public Sprite? LoadSprite(string name)
  {
    var texture = LoadTexture("icons", name);
    return texture is null ? null : Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
  }

  private T? Load<T>(string path, Func<string, T> read) where T : class
  {
    try
    {
      return read(path);
    }
    catch (Exception e)
    {
      logger.LogError($"Couldn't load {path}: {e.Message}");
      return null;
    }
  }
}

/// <summary>
/// Reads the OBJ files <c>models/build.py</c> writes: one object, triangles, positions, UVs and normals.
/// </summary>
/// <remarks>
/// OBJ is right-handed and Unity left-handed, so X is mirrored and every triangle's winding reversed;
/// otherwise the model would be inside out. The exporter's axes (Y up, -Z forward) are kept as they are.
/// </remarks>
internal static class ObjMesh
{
  public static Mesh Read(string path, string name)
  {
    var positions = new List<Vector3>();
    var uvs = new List<Vector2>();
    var normals = new List<Vector3>();
    var corners = new Dictionary<(int, int, int), int>();
    var vertices = new List<Vector3>();
    var vertexUvs = new List<Vector2>();
    var vertexNormals = new List<Vector3>();
    var triangles = new List<int>();

    foreach (var raw in File.ReadLines(path))
    {
      var parts = raw.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length == 0)
      {
        continue;
      }

      switch (parts[0])
      {
        case "v":
          positions.Add(new Vector3(-Float(parts[1]), Float(parts[2]), Float(parts[3])));
          break;
        case "vt":
          uvs.Add(new Vector2(Float(parts[1]), Float(parts[2])));
          break;
        case "vn":
          normals.Add(new Vector3(-Float(parts[1]), Float(parts[2]), Float(parts[3])));
          break;
        case "f":
          if (parts.Length != 4)
          {
            throw new InvalidDataException($"only triangles are read, but a face has {parts.Length - 1} corners");
          }

          // Reversed winding, see the remarks.
          triangles.Add(Corner(parts[1]));
          triangles.Add(Corner(parts[3]));
          triangles.Add(Corner(parts[2]));
          break;
      }
    }

    if (triangles.Count == 0)
    {
      throw new InvalidDataException("no faces");
    }

    var mesh = new Mesh { name = name };
    mesh.SetVertices(vertices);
    mesh.SetUVs(0, vertexUvs);
    mesh.SetNormals(vertexNormals);
    mesh.SetTriangles(triangles, 0);
    mesh.RecalculateBounds();
    mesh.RecalculateTangents();
    return mesh;

    // "v/vt/vn", 1-based. Each distinct combination becomes one Unity vertex.
    int Corner(string text)
    {
      var ids = text.Split('/');
      var key = (Index(ids, 0), Index(ids, 1), Index(ids, 2));
      if (!corners.TryGetValue(key, out var index))
      {
        index = vertices.Count;
        corners[key] = index;
        vertices.Add(positions[key.Item1]);
        vertexUvs.Add(key.Item2 >= 0 ? uvs[key.Item2] : Vector2.zero);
        vertexNormals.Add(key.Item3 >= 0 ? normals[key.Item3] : Vector3.up);
      }

      return index;
    }
  }

  private static int Index(string[] ids, int slot) =>
    slot < ids.Length && ids[slot].Length > 0 ? int.Parse(ids[slot], CultureInfo.InvariantCulture) - 1 : -1;

  private static float Float(string text) => float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
}
