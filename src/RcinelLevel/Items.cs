using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Rcinel.Level;

/// <summary>
/// Creates the probe, the level hoe, the hoe's piece and the terrain tiles from the game's own
/// prefabs once they are loaded.
/// </summary>
/// <remarks>
/// <para>
/// Both tools are clones of game items, so they keep the game's equip effects, physics and pickup:
/// the probe is the torch without its fire (the torch is the game's rod held in either hand), the
/// level hoe is the hoe with its own piece table. The game's models are hidden and ours, from
/// <c>models/build.py</c>, take their place under the same attach point (decisions 0009, 0011).
/// </para>
/// <para>
/// The probe is a shield as far as the game's hands go, because shields always go to the left hand;
/// it animates like a torch held in the left hand. A tool normally empties both hands, which
/// <see cref="TwoToolsPatch"/> relaxes for exactly these two items.
/// </para>
/// </remarks>
internal static class Items
{
  public const string ProbePrefab = "RcinelLevel_Probe";
  public const string HoePrefab = "RcinelLevel_LevelHoe";
  public const string PiecePrefab = "RcinelLevel_Level";
  public const string PieceTableName = "_RcinelLevelHoePieceTable";
  public const string ModelName = "rcinellevel_model";

  private static PluginFiles _files = null!;
  private static Settings _settings = null!;
  private static ManualLogSource _logger = null!;
  private static Mesh? _probeMesh, _hoeMesh;
  private static Texture2D? _atlas;
  private static Sprite? _probeIcon, _hoeIcon;

  /// <summary>The dotted segment of the game's range rings (the workbench's), for the frame.</summary>
  public static GameObject? SegmentPrefab { get; private set; }

  /// <summary>Loads the plugin's own files; runs at start-up, before the game's prefabs exist.</summary>
  public static void Prepare(PluginFiles files, Settings settings, ManualLogSource logger)
  {
    _files = files;
    _settings = settings;
    _logger = logger;
    _probeMesh = files.LoadMesh("probe.obj");
    _hoeMesh = files.LoadMesh("level-hoe.obj");
    _atlas = files.LoadTexture("models", "atlas.png");
    _probeIcon = files.LoadSprite("probe-128.png");
    _hoeIcon = files.LoadSprite("level-hoe-128.png");
    settings.Reloaded += ApplyModelPlacement;
  }

  public static bool IsProbe(ItemDrop.ItemData? item) => item?.m_shared.m_name == Texts.ProbeName;

  public static bool IsLevelHoe(ItemDrop.ItemData? item) => item?.m_shared.m_name == Texts.HoeName;

  /// <summary>Jotunn's <c>OnVanillaPrefabsAvailable</c>: runs once, in the main menu.</summary>
  public static void Create()
  {
    PrefabManager.OnVanillaPrefabsAvailable -= Create;
    try
    {
      var material = MakeMaterials();
      CreateTiles();
      MeasureStoneCost();
      CreateProbe(material);
      CreatePieceTableAndPiece();
      CreateHoe(material);
      SegmentPrefab = PrefabManager.Instance.GetPrefab("piece_workbench")?.GetComponentInChildren<CircleProjector>(true)?.m_prefab;
      if (SegmentPrefab is null)
      {
        _logger.LogWarning("The workbench's range ring wasn't found, so the frame is drawn without it.");
      }
    }
    catch (Exception e)
    {
      // A game update that moves things around must not take the game down with it.
      _logger.LogError($"Creating RcinelLevel's items failed, the tools won't be available: {e}");
    }
  }

  /// <summary>
  /// The models' two materials (the OBJ's groups): a copy of the game hoe's wood material with our
  /// atlas, and the game hoe's own stone material for the flint blade and point, so light, fog and
  /// shading match the game's tools and the stone looks like the game hoe's blade (owner, 2026-11-03).
  /// </summary>
  /// <remarks>
  /// The hoe's wood material stretches and flips its texture (<c>_MainTex</c> scale 1 × -0.62, read from
  /// the game's files): copied as it was, it showed the atlas's resin corner on the blade, which came
  /// out yellow in the owner's test. The copy's tiling is reset; its other maps (normal, metal) belong
  /// to the game's texture and are dropped.
  /// </remarks>
  private static Material[]? MakeMaterials()
  {
    var visual = PrefabManager.Instance.GetPrefab("Hoe")?.transform.Find("attach/visual");
    var wood = visual?.Find("handle")?.GetComponent<MeshRenderer>();
    var stone = visual?.Find("blade")?.GetComponent<MeshRenderer>();
    if (wood is null || stone is null || _atlas is null)
    {
      _logger.LogWarning("No materials for the models (game hoe or atlas missing); the game's own models stay.");
      return null;
    }

    var material = new Material(wood.sharedMaterial)
    {
      name = "rcinellevel_atlas",
      mainTexture = _atlas,
      mainTextureScale = Vector2.one,
      mainTextureOffset = Vector2.zero,
    };
    foreach (var map in new[] { "_BumpMap", "_MetallicGlossMap", "_EmissionMap", "_DetailAlbedoMap", "_DetailNormalMap" })
    {
      if (material.HasProperty(map))
      {
        material.SetTexture(map, null);
      }
    }

    if (material.HasProperty("_Color"))
    {
      material.color = Color.white;
    }

    return [material, stone.sharedMaterial];
  }

  private static void CreateTiles()
  {
    var original = PrefabManager.Instance.GetPrefab("mud_road_v2")?.GetComponent<TerrainOp>();
    if (original is null)
    {
      throw new InvalidOperationException("the game's level-ground piece (mud_road_v2) wasn't found");
    }

    var s = original.m_settings;
    _logger.LogInfo($"Game's level ground: level {s.m_level} r {s.m_levelRadius} square {s.m_square} offset {s.m_levelOffset}, smooth {s.m_smooth} r {s.m_smoothRadius}, paint {s.m_paintCleared} {s.m_paintType} r {s.m_paintRadius}");

    foreach (var radius in TerrainTiles.Radii)
    {
      // Jotunn clones under an inactive parent, so the op's Awake doesn't run on the clone itself.
      var prefab = PrefabManager.Instance.CreateClonedPrefab(TerrainTiles.PrefabName(radius), original.gameObject);
      Object.DestroyImmediate(prefab.GetComponent<Piece>());
      foreach (Transform child in prefab.transform)
      {
        Object.DestroyImmediate(child.gameObject);
      }

      var op = prefab.GetComponent<TerrainOp>();
      var settings = op.m_settings;
      settings.m_level = true;
      settings.m_levelRadius = radius;
      settings.m_square = true;
      settings.m_levelOffset = 0f;
      settings.m_raise = false;
      settings.m_smooth = false;
      // The game's level ground paints the ground as cleared; keep that look, sized to the tile.
      settings.m_paintRadius = Mathf.Max(radius, 0.5f);
      // One sound for the whole frame comes from the placed piece, not one per tile.
      op.m_onPlacedEffect = new EffectList();
      op.m_spawnOnPlaced = null;
      TerrainTiles.Prefabs[radius] = op;
    }

    // The menu's ObjectDB exists already; the game's own, in the world, registers them on its Awake
    // through RegisterTilesPatch.
    ObjectDB.instance?.UpdateRegisters();
  }

  /// <summary>
  /// Works out what the game's own Raise ground costs per cubic metre: its stone divided by the ground
  /// one use raises (its radius, height and falloff, as <c>TerrainComp.RaiseTerrain</c> applies them on
  /// the 1 m grid). The level hoe charges the same rate (decision 0015), so it is never cheaper than
  /// raising by hand.
  /// </summary>
  private static void MeasureStoneCost()
  {
    var table = PrefabManager.Instance.GetPrefab("Hoe")?.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces;
    foreach (var piece in table is null ? [] : table.m_pieces)
    {
      var op = piece == null ? null : piece.GetComponent<TerrainOp>();
      if (op is null || !op.m_settings.m_raise || op.m_settings.m_raiseDelta <= 0f)
      {
        continue;
      }

      var stone = 0;
      foreach (var requirement in piece!.GetComponent<Piece>()?.m_resources ?? [])
      {
        if (requirement.m_resItem != null && requirement.m_resItem.name == "Stone")
        {
          stone += requirement.m_amount;
        }
      }

      var s = op.m_settings;
      var reach = Mathf.CeilToInt(s.m_raiseRadius);
      var volume = 0f;
      for (var z = -reach; z <= reach; z++)
      {
        for (var x = -reach; x <= reach; x++)
        {
          var distance = Mathf.Sqrt(x * x + z * z);
          if (!s.m_square && distance > s.m_raiseRadius)
          {
            continue;
          }

          var strength = s.m_square || s.m_raisePower <= 0f ? 1f : Mathf.Pow(1f - distance / s.m_raiseRadius, s.m_raisePower);
          volume += s.m_raiseDelta * strength;
        }
      }

      if (stone > 0 && volume > 0f)
      {
        Stroke.StonePerCubicMetre = stone / volume;
        _logger.LogInfo($"The game's {piece.name}: {stone} stone for {volume:0.00} m³ (radius {s.m_raiseRadius}, delta {s.m_raiseDelta}, power {s.m_raisePower}), so {Stroke.StonePerCubicMetre:0.00} stone per m³.");
        return;
      }
    }

    _logger.LogWarning($"The game's Raise ground wasn't found; the level hoe charges {Stroke.StonePerCubicMetre} stone per m³.");
  }

  private static void CreateProbe(Material[]? material)
  {
    var item = new CustomItem(ProbePrefab, "Torch", new ItemConfig
    {
      Name = Texts.ProbeName,
      Description = Texts.ProbeDescription,
      CraftingStation = CraftingStations.Workbench,
      Requirements =
      [
        new RequirementConfig("Wood", 2),
        new RequirementConfig("Flint", 2),
        new RequirementConfig("Resin", 1),
      ],
      Icons = _probeIcon is null ? null : [_probeIcon],
    });

    var prefab = item.ItemPrefab;
    var attach = prefab.transform.Find("attach");
    // The burning part: light, fire, warmth and its sound.
    var fire = attach?.Find("equiped");
    if (fire is not null)
    {
      Object.DestroyImmediate(fire.gameObject);
    }

    ReplaceModel(attach, attach?.Find("model"), _probeMesh, material, _settings.ProbePosition.Value, _settings.ProbeRotation.Value);

    var shared = item.ItemDrop.m_itemData.m_shared;
    shared.m_itemType = ItemDrop.ItemData.ItemType.Shield;
    shared.m_animationState = ItemDrop.ItemData.AnimationState.LeftTorch;
    shared.m_attachOverride = ItemDrop.ItemData.ItemType.None;
    shared.m_useDurability = false;
    shared.m_durabilityDrain = 0f;
    shared.m_damages = new HitData.DamageTypes();
    shared.m_blockPower = 0f;
    shared.m_deflectionForce = 0f;
    shared.m_timedBlockBonus = 1f;
    shared.m_equipStatusEffect = null;
    shared.m_maxStackSize = 1;
    shared.m_weight = 1f;
    ItemManager.Instance.AddItem(item);
  }

  private static void CreatePieceTableAndPiece()
  {
    PieceManager.Instance.AddPieceTable(new CustomPieceTable(PieceTableName, new PieceTableConfig
    {
      CanRemovePieces = false,
    }));

    var piece = new CustomPiece(PiecePrefab, "mud_road_v2", new PieceConfig
    {
      Name = Texts.LevelPieceName,
      Description = Texts.LevelPieceDescription,
      PieceTable = PieceTableName,
      Icon = _hoeIcon,
    });

    // The piece doesn't level the spot it's placed on; LevelAction levels the frame instead.
    Object.DestroyImmediate(piece.PiecePrefab.GetComponent<TerrainOp>());
    piece.PiecePrefab.AddComponent<LevelAction>();
    // Turning the ghost means nothing for world-aligned frames, and the wheel is ours with a modifier.
    piece.Piece.m_canRotate = false;
    PieceManager.Instance.AddPiece(piece);
  }

  private static void CreateHoe(Material[]? material)
  {
    var item = new CustomItem(HoePrefab, "Hoe", new ItemConfig
    {
      Name = Texts.HoeName,
      Description = Texts.HoeDescription,
      CraftingStation = CraftingStations.Workbench,
      Requirements =
      [
        // Upgrades (three, decision 0019) cost flint and leather again, not another hoe.
        new RequirementConfig("Hoe", 1, amountPerLevel: 0, recover: false),
        new RequirementConfig("Flint", 4, amountPerLevel: 2),
        new RequirementConfig("LeatherScraps", 2, amountPerLevel: 1),
      ],
      Icons = _hoeIcon is null ? null : [_hoeIcon],
    });

    var visual = item.ItemPrefab.transform.Find("attach/visual");
    foreach (var part in new[] { "blade", "handle" })
    {
      visual?.Find(part)?.gameObject.SetActive(false);
    }

    ReplaceModel(visual, null, _hoeMesh, material, _settings.HoePosition.Value, _settings.HoeRotation.Value);

    var table = PieceManager.Instance.GetPieceTable(PieceTableName);
    var shared = item.ItemDrop.m_itemData.m_shared;
    shared.m_buildPieces = table;
    // Upgradeable three times at the workbench (levels 2 to 4), each adding durability like the game's
    // own tools do; the game takes the workbench level a quality needs from the recipe.
    shared.m_maxQuality = 4;
    shared.m_durabilityPerLevel = 50f;
    ItemManager.Instance.AddItem(item);
  }

  /// <summary>Hides the game's model and puts ours under <paramref name="parent"/>.</summary>
  private static void ReplaceModel(Transform? parent, Transform? old, Mesh? mesh, Material[]? material, Vector3 position, Vector3 rotation)
  {
    if (parent is null || mesh is null || material is null)
    {
      _logger.LogWarning($"Keeping the game's model under {parent?.name ?? "(missing)"}: no mesh or material.");
      return;
    }

    old?.gameObject.SetActive(false);
    var model = new GameObject(ModelName);
    model.transform.SetParent(parent, worldPositionStays: false);
    model.transform.localPosition = position;
    model.transform.localEulerAngles = rotation;
    model.AddComponent<MeshFilter>().sharedMesh = mesh;
    model.AddComponent<MeshRenderer>().sharedMaterials = material;
  }

  /// <summary>
  /// After the config file changed: moves our models in the prefabs and in every copy that is held
  /// right now, so the placement can be tuned while playing.
  /// </summary>
  private static void ApplyModelPlacement()
  {
    Place(PrefabManager.Instance.GetPrefab(ProbePrefab), _settings.ProbePosition.Value, _settings.ProbeRotation.Value);
    Place(PrefabManager.Instance.GetPrefab(HoePrefab), _settings.HoePosition.Value, _settings.HoeRotation.Value);
    if (Player.m_localPlayer is { } player)
    {
      foreach (var model in player.GetComponentsInChildren<MeshFilter>(true))
      {
        if (model.name != ModelName)
        {
          continue;
        }

        var isProbe = model.sharedMesh == _probeMesh;
        model.transform.localPosition = isProbe ? _settings.ProbePosition.Value : _settings.HoePosition.Value;
        model.transform.localEulerAngles = isProbe ? _settings.ProbeRotation.Value : _settings.HoeRotation.Value;
      }
    }

    static void Place(GameObject? prefab, Vector3 position, Vector3 rotation)
    {
      foreach (var filter in prefab is null ? [] : prefab.GetComponentsInChildren<MeshFilter>(true))
      {
        if (filter.name == ModelName)
        {
          filter.transform.localPosition = position;
          filter.transform.localEulerAngles = rotation;
        }
      }
    }
  }
}
