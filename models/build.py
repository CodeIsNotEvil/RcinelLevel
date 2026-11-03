"""Build RcinelLevel's models in Blender, export them, and render previews.

  blender -b --factory-startup --python models/build.py -- [--previews]

Run from the repository root. Writes, under assets/models/:
  probe.obj, level-hoe.obj  the meshes the plugin loads at runtime (decision 0009)
  atlas.png                  the one texture both meshes are mapped onto
  previews/*.png             renders to look at (only with --previews; slow, Cycles on the CPU)

This script is the source of the models. Change a model here and run it again; never edit the
exported files by hand (the same rule as the icons, whose SVGs are the source).

Conventions the plugin's loader relies on:
- Metres. Blender's Z is up; the OBJ is written Y-up with -Z forward, the usual OBJ axes. Unity's
  coordinates are left-handed, so the loader mirrors X when it reads the file.
- The origin is where the hand holds the tool, and the tool's length runs along +Z (Y in the OBJ),
  head or handle up. How it sits in the hand is tuned in the game, at the attach point.
- One mesh, one material, one texture: each part's UVs are fitted into its region of the atlas
  (wood, flint, leather, resin). The plugin puts the atlas on a material copied from a game item, so
  light and shading match Valheim. Triangles only, with normals.

The texture is drawn here with numpy rather than painted, so it rebuilds the same every time (fixed
seeds). It is deliberately small and coarse, like Valheim's own hand-painted textures.
"""

import math
import sys
from pathlib import Path

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector

ROOT = Path.cwd()
OUT = ROOT / "assets" / "models"
ATLAS_SIZE = 256

# Atlas regions as (u0, v0, u1, v1). A margin keeps bilinear filtering and mipmaps from bleeding one
# material into its neighbour at the region's edge.
MARGIN = 6 / ATLAS_SIZE
REGIONS = {
  "wood": (0.0, 0.5, 0.5, 1.0),
  "flint": (0.5, 0.5, 1.0, 1.0),
  "leather": (0.0, 0.0, 0.5, 0.5),
  "resin": (0.5, 0.0, 1.0, 0.5),
}


# --- Texture ---------------------------------------------------------------------------------------

def noise(rng: np.random.Generator, size: int, cells: int) -> np.ndarray:
  """Smooth value noise in 0..1: a coarse random grid scaled up with bilinear interpolation."""
  grid = rng.random((cells + 1, cells + 1))
  t = np.linspace(0, cells, size, endpoint=False)
  i = t.astype(int)
  f = t - i
  f = f * f * (3 - 2 * f)
  rows = grid[i][:, i] * (1 - f)[None, :] + grid[i][:, i + 1] * f[None, :]
  rows_next = grid[i + 1][:, i] * (1 - f)[None, :] + grid[i + 1][:, i + 1] * f[None, :]
  return rows * (1 - f)[:, None] + rows_next * f[:, None]


def tint(base: str, shade: np.ndarray) -> np.ndarray:
  """A colour (#rrggbb) scaled by a 0..2 shade map, as an RGB array."""
  rgb = np.array([int(base[k:k + 2], 16) / 255 for k in (1, 3, 5)])
  return np.clip(shade[..., None] * rgb, 0, 1)


def build_atlas() -> np.ndarray:
  half = ATLAS_SIZE // 2
  rng = np.random.default_rng(7)

  # Wood: grain runs along V, which is along each wooden part's length (see uv_cylinder).
  x = np.linspace(0, 1, half)[None, :].repeat(half, 0)
  grain = 0.5 + 0.5 * np.sin((x * 22 + noise(rng, half, 6) * 3) * math.pi)
  wood = tint("#7a5634", 0.75 + 0.3 * grain + 0.25 * noise(rng, half, 16))

  # Flint: dark blue-grey with lighter flaked patches, the look of knapped stone.
  flakes = noise(rng, half, 5)
  flint = tint("#4a4f58", 0.7 + 0.5 * (flakes > 0.55) * flakes + 0.2 * noise(rng, half, 24))

  # Leather: warm brown, mottled, with the wrap's bands across V.
  y = np.linspace(0, 1, half)[:, None].repeat(half, 1)
  bands = 0.85 + 0.15 * (np.sin(y * math.pi * 14) > -0.6)
  leather = tint("#6b4026", (0.8 + 0.35 * noise(rng, half, 10)) * bands)

  # Resin: amber, darker where it pooled.
  resin = tint("#c27a22", 0.6 + 0.6 * noise(rng, half, 4))

  image = np.zeros((ATLAS_SIZE, ATLAS_SIZE, 3))
  # Rows count from the bottom, as Blender's pixel buffer and UVs do.
  image[half:, :half] = wood
  image[half:, half:] = flint
  image[:half, :half] = leather
  image[:half, half:] = resin
  return image


def save_atlas(path: Path) -> bpy.types.Image:
  pixels = build_atlas()
  image = bpy.data.images.new("atlas", ATLAS_SIZE, ATLAS_SIZE, alpha=False)
  rgba = np.concatenate([pixels, np.ones((ATLAS_SIZE, ATLAS_SIZE, 1))], axis=2)
  image.pixels.foreach_set(rgba.astype(np.float32).ravel())
  image.filepath_raw = str(path)
  image.file_format = "PNG"
  image.save()
  return image


# --- Geometry --------------------------------------------------------------------------------------

class Model:
  """One mesh built from parts. Each part is made at the origin along Z, UV-mapped into its
  material's atlas region in that local frame, and then moved into place."""

  def __init__(self) -> None:
    self.bm = bmesh.new()
    self.uv = self.bm.loops.layers.uv.new("UVMap")

  def _place(self, faces, verts, region: str, matrix: Matrix, smooth: bool, mapping) -> None:
    u0, v0, u1, v1 = REGIONS[region]
    u0, v0, u1, v1 = u0 + MARGIN, v0 + MARGIN, u1 - MARGIN, v1 - MARGIN
    for face in faces:
      coords = [mapping(loop.vert.co, face.normal) for loop in face.loops]
      # A face across the cylinder's seam would wrap the whole texture backwards; unwrap it.
      us = [c[0] for c in coords]
      if max(us) - min(us) > 0.5:
        coords = [(c[0] + 1 if c[0] < 0.5 else c[0], c[1]) for c in coords]
      for loop, (u, v) in zip(face.loops, coords):
        loop[self.uv].uv = (u0 + (u1 - u0) * (u % 1.0001), v0 + (v1 - v0) * v)
      face.smooth = smooth
    bmesh.ops.transform(self.bm, matrix=matrix, verts=verts)

  def cylinder(self, region: str, radius: float, length: float, at, rotation=Matrix.Identity(4),
               segments: int = 6, radius_top: float | None = None, smooth: bool = True) -> None:
    """A prism along Z from -length/2 to +length/2, then rotated and moved to `at`."""
    before = set(self.bm.faces)
    top = radius if radius_top is None else radius_top
    result = bmesh.ops.create_cone(self.bm, cap_ends=True, cap_tris=False, segments=segments,
                                   radius1=radius, radius2=top, depth=length)
    faces = [f for f in self.bm.faces if f not in before]
    r = max(radius, top) or 1

    def mapping(co: Vector, normal: Vector):
      if abs(normal.z) > 0.9:  # cap: planar
        return (0.5 + co.x / (2 * r), 0.5 + co.y / (2 * r))
      return ((math.atan2(co.y, co.x) / (2 * math.pi)) % 1.0, co.z / length + 0.5)

    self._place(faces, result["verts"], region, Matrix.Translation(at) @ rotation, smooth, mapping)

  def box(self, region: str, size, at, rotation=Matrix.Identity(4)) -> None:
    """A box of size (x, y, z) centred on `at`, flat shaded, mapped per face along its long side."""
    before = set(self.bm.faces)
    result = bmesh.ops.create_cube(self.bm, size=1)
    faces = [f for f in self.bm.faces if f not in before]
    sx, sy, sz = size
    bmesh.ops.scale(self.bm, vec=(sx, sy, sz), verts=result["verts"])

    def mapping(co: Vector, normal: Vector):
      n = [abs(normal.x), abs(normal.y), abs(normal.z)]
      axes = [a for a in range(3) if a != n.index(max(n))]
      # V along the longer of the two axes, so wood grain follows the board.
      a, b = sorted(axes, key=lambda k: size[k])
      return (co[a] / size[a] + 0.5, co[b] / size[b] + 0.5)

    self._place(faces, result["verts"], region, Matrix.Translation(at) @ rotation, False, mapping)

  def to_object(self, name: str) -> bpy.types.Object:
    bmesh.ops.triangulate(self.bm, faces=self.bm.faces)
    mesh = bpy.data.meshes.new(name)
    self.bm.to_mesh(mesh)
    self.bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def lying(axis: str) -> Matrix:
  """Turns a part made along Z to lie along X or Y."""
  return Matrix.Rotation(math.radians(90), 4, "Y" if axis == "X" else "X")


def build_probe() -> bpy.types.Object:
  """A soil probe: a long fire-hardened rod with a flint point and a T-handle, pushed into the ground
  to feel where it gets harder a few metres down. Depth bands of resin every 30 cm. Held in the off
  hand near the top, so the origin is just under the handle. About 1.6 m long."""
  m = Model()
  top, bottom = 0.42, -1.04
  m.cylinder("wood", 0.016, top - bottom, (0, 0, (top + bottom) / 2), segments=6)
  # T-handle, lashed on with leather, sealed with resin.
  m.cylinder("wood", 0.021, 0.32, (0, 0, top + 0.02), lying("X"), segments=6)
  m.cylinder("leather", 0.026, 0.07, (0, 0, top - 0.01), segments=6)
  m.cylinder("resin", 0.024, 0.02, (0, 0, top - 0.055), segments=6)
  # Grip wraps at both ends of the crossbar.
  for x in (-0.11, 0.11):
    m.cylinder("leather", 0.025, 0.08, (x, 0, top + 0.02), lying("X"), segments=6)
  # Depth bands.
  for z in (-0.18, -0.48, -0.78):
    m.cylinder("resin", 0.0195, 0.025, (0, 0, z), segments=6)
  # Flint point, bound with leather where it meets the rod.
  m.cylinder("flint", 0.0, 0.16, (0, 0, bottom - 0.07), segments=4, radius_top=0.026, smooth=False)
  m.cylinder("leather", 0.021, 0.06, (0, 0, bottom + 0.01), segments=6)
  return m.to_object("probe")


def build_level_hoe() -> bpy.types.Object:
  """A square-scoop hoe: the head is a flint floor with a dead-straight front edge, two flint side
  walls and a wooden straightedge as the back wall, all at right angles, so the tool looks like the
  90-degree cuts it makes. The flint and leather are what the recipe adds to the game's hoe
  (decision 0007). Held near the lower end like the game's hoe; about 1.35 m long."""
  m = Model()
  low, high = -0.18, 1.08
  m.cylinder("wood", 0.022, high - low, (0, 0, (low + high) / 2), segments=8)
  m.cylinder("leather", 0.026, 0.28, (0, 0, 0.0), segments=8)
  m.cylinder("resin", 0.024, 0.03, (0, 0, low + 0.02), segments=8)
  # The head sits on the end of the handle: the floor is perpendicular to the handle like a hoe's
  # blade, and its straight edge points away from the handle's side (+Y).
  floor = high + 0.012
  width, depth, wall = 0.50, 0.22, 0.09
  # Back wall: the wooden straightedge, lashed to the handle's end on both sides.
  m.box("wood", (width + 0.06, 0.05, wall + 0.02), (0, 0.0, floor + wall / 2))
  for x in (-0.07, 0.07):
    m.box("leather", (0.035, 0.065, wall + 0.035), (x, 0.0, floor + wall / 2))
  # Floor: the blade, with a thinner strip as its sharpened straight edge.
  m.box("flint", (width, depth, 0.024), (0, depth / 2 + 0.02, floor))
  m.box("flint", (width, 0.03, 0.012), (0, depth + 0.035, floor))
  # Side walls: the square corners.
  for x in (-(width / 2 + 0.011), width / 2 + 0.011):
    m.box("flint", (0.022, depth, wall), (x, depth / 2 + 0.02, floor + wall / 2 - 0.012))
  return m.to_object("level-hoe")


# --- Export and previews ---------------------------------------------------------------------------

def export(obj: bpy.types.Object, path: Path) -> None:
  bpy.ops.object.select_all(action="DESELECT")
  obj.select_set(True)
  bpy.context.view_layer.objects.active = obj
  bpy.ops.wm.obj_export(
    filepath=str(path), export_selected_objects=True, forward_axis="NEGATIVE_Z", up_axis="Y",
    export_uv=True, export_normals=True, export_materials=False, export_triangulated_mesh=True,
    apply_modifiers=True,
  )


def preview_material(atlas: bpy.types.Image) -> bpy.types.Material:
  mat = bpy.data.materials.new("atlas")
  nodes = mat.node_tree.nodes
  tex = nodes.new("ShaderNodeTexImage")
  tex.image = atlas
  tex.interpolation = "Closest"  # shows the texels as the game's low resolution would
  bsdf = nodes["Principled BSDF"]
  bsdf.inputs["Roughness"].default_value = 0.8
  mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
  return mat


def render_previews(objects: list[bpy.types.Object], atlas: bpy.types.Image, folder: Path) -> None:
  scene = bpy.context.scene
  try:
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 48
  except TypeError:
    scene.render.engine = "BLENDER_EEVEE"
  scene.render.resolution_x = scene.render.resolution_y = 512
  scene.render.film_transparent = False
  # Previews are committed, so keep them small: they're for looking at, not for detail work.
  scene.render.image_settings.compression = 100
  scene.view_settings.view_transform = "Standard"
  scene.cycles.use_denoising = True
  world = bpy.data.worlds.new("world")
  # #181614, the dark background of the owner's logos, in linear colour.
  world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.009, 0.008, 0.007, 1)
  world.node_tree.nodes["Background"].inputs["Strength"].default_value = 1.0
  scene.world = world

  sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
  sun.data.energy = 4.0
  fill = bpy.data.objects.new("fill", bpy.data.lights.new("fill", "SUN"))
  fill.data.energy = 1.2
  fill.rotation_euler = (math.radians(60), 0, math.radians(200))
  scene.collection.objects.link(fill)
  sun.rotation_euler = (math.radians(50), math.radians(10), math.radians(35))
  scene.collection.objects.link(sun)

  # A 1.8 m figure for scale, the height of a Valheim player, only in the full view.
  figure_mesh = bpy.data.meshes.new("figure")
  fb = bmesh.new()
  bmesh.ops.create_cone(fb, cap_ends=True, segments=12, radius1=0.22, radius2=0.22, depth=1.8)
  bmesh.ops.translate(fb, vec=(0, 0, 0.9), verts=fb.verts)
  fb.to_mesh(figure_mesh)
  fb.free()
  figure = bpy.data.objects.new("figure", figure_mesh)
  grey = bpy.data.materials.new("grey")
  grey.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.08, 0.08, 0.08, 1)
  figure_mesh.materials.append(grey)
  scene.collection.objects.link(figure)

  camera = bpy.data.objects.new("camera", bpy.data.cameras.new("camera"))
  scene.collection.objects.link(camera)
  scene.camera = camera

  material = preview_material(atlas)
  for obj in objects:
    obj.data.materials.clear()
    obj.data.materials.append(material)

  for obj in objects:
    for other in objects:
      other.hide_render = other is not obj
    # Full view: the tool standing next to the figure, as long as it really is.
    obj.location = (0.6, 0, 1.0 - min(v.co.z for v in obj.data.vertices) - 1.0)
    figure.hide_render = False
    look(camera, target=Vector((0.3, 0, 0.9)), distance=3.4, height=1.3, lens=50)
    scene.render.filepath = str(folder / f"{obj.name}.png")
    bpy.ops.render.render(write_still=True)
    # Close-up of the head (hoe) or the handle and point (probe).
    figure.hide_render = True
    obj.location = (0, 0, 0)
    for name, target, distance in detail_views(obj):
      look(camera, target=target, distance=distance, height=target.z + 0.45, lens=50)
      scene.render.filepath = str(folder / f"{obj.name}-{name}.png")
      bpy.ops.render.render(write_still=True)


def detail_views(obj: bpy.types.Object):
  zs = [v.co.z for v in obj.data.vertices]
  if obj.name == "probe":
    return [("handle", Vector((0, 0, max(zs) - 0.05)), 0.9), ("point", Vector((0, 0, min(zs) + 0.12)), 0.7)]
  return [("head", Vector((0, 0.12, max(zs) - 0.04)), 1.0)]


def look(camera: bpy.types.Object, target: Vector, distance: float, height: float, lens: float) -> None:
  camera.data.lens = lens
  direction = Vector((math.cos(math.radians(-35)), math.sin(math.radians(-35)), 0))
  camera.location = target + direction * distance
  camera.location.z = height
  camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()


def main() -> None:
  args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
  (OUT / "previews").mkdir(parents=True, exist_ok=True)
  for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj)

  atlas = save_atlas(OUT / "atlas.png")
  models = [build_probe(), build_level_hoe()]
  for obj in models:
    export(obj, OUT / f"{obj.name}.obj")
    print(f"{obj.name}: {len(obj.data.polygons)} triangles")
  if "--previews" in args:
    render_previews(models, atlas, OUT / "previews")


main()
