using UnityEngine;

namespace Ruinas
{
    public enum BlockShape : byte { Empty, Full, SlabBottom }

    /// <summary>Índices de tiles no atlas do mundo (gerado pelo TextureForge na mesma ordem).</summary>
    public enum Tile : ushort
    {
        FloorA0, FloorA1, FloorA2, FloorA3,
        FloorB0, FloorB1, FloorB2, FloorB3,
        FloorC0, FloorC1, FloorC2, FloorC3,
        FloorDark0, FloorDark1, FloorDark2, FloorDark3,
        WallBrick, WallBrick2, WallBrickMossy, WallBrickMossy2,
        WallCap, PillarSide, PillarTop, StoneRough,
        StoneRoughDark, DirtTop, DirtTop2, DirtSide,
        GrassTop, GrassTop2, GrassSide, LeafLitter,
        RedBrickSide, RedBrickTop, GoldSide, GoldTop,
        WoodSide, WoodTop, CarvedTop, PathCobble,
        PathCobble2, Water, Leaves, LeavesDark,
        ObsidianSide, ObsidianTop, MossTop, WallBrickDark,
        StairFront, BambooSide, BambooTop, SandTop,
        Count,
    }

    public struct BlockDef
    {
        public string name;
        public BlockShape shape;
        public Tile top, side, bottom;
        /// <summary>Topo usa um macro-tile 2×2 (3×3 ladrilhos por 2 blocos): quatro quartos por variante.</summary>
        public bool macroTop;
        public Tile[] macroVariants;
        public Tile[] sideVariants;
        public Tile mossySide;
        public bool solid;
        public bool visualOnly;
        public bool occluder;
        public bool walkable;
        public Color32 tint;
        public float jitter;
    }

    /// <summary>Tipos de bloco do cenário e sua aparência por face.</summary>
    public class BlockRegistry
    {
        public const byte Air = 0, Floor = 1, FloorDark = 2, Wall = 3, WallMossy = 4, Pillar = 5, Stone = 6, Dirt = 7, Grass = 8,
            RedBrick = 9, Gold = 10, Wood = 11, Carved = 12, Path = 13, Water = 14, Leaves = 15, StairFull = 16, StairSlab = 17,
            FloorSlab = 18, Obsidian = 19, Litter = 20, WallDark = 21, LeavesDark = 22, Bamboo = 23, Sand = 24;

        readonly BlockDef[] defs = new BlockDef[256];

        public BlockDef this[byte id] => defs[id];

        public BlockRegistry()
        {
            var white = new Color32(255, 255, 255, 255);
            defs[Air] = new BlockDef { name = "Ar", shape = BlockShape.Empty };
            var floorMacro = new[] { Tile.FloorA0, Tile.FloorB0, Tile.FloorC0 };
            defs[Floor] = new BlockDef { name = "Piso", shape = BlockShape.Full, macroTop = true, macroVariants = floorMacro, side = Tile.WallBrick, sideVariants = new[] { Tile.WallBrick, Tile.WallBrick2 }, mossySide = Tile.WallBrickMossy, bottom = Tile.StoneRough, solid = true, occluder = true, walkable = true, tint = white, jitter = 0.05f };
            defs[FloorDark] = new BlockDef { name = "PisoEscuro", shape = BlockShape.Full, macroTop = true, macroVariants = new[] { Tile.FloorDark0 }, side = Tile.WallBrick, sideVariants = new[] { Tile.WallBrick, Tile.WallBrick2 }, mossySide = Tile.WallBrickMossy, bottom = Tile.StoneRough, solid = true, occluder = true, walkable = true, tint = white, jitter = 0.05f };
            defs[Wall] = new BlockDef { name = "Parede", shape = BlockShape.Full, top = Tile.WallCap, side = Tile.WallBrick, sideVariants = new[] { Tile.WallBrick, Tile.WallBrick2 }, mossySide = Tile.WallBrickMossy, bottom = Tile.StoneRough, solid = true, occluder = true, tint = white, jitter = 0.05f };
            defs[WallMossy] = new BlockDef { name = "ParedeMusgo", shape = BlockShape.Full, top = Tile.MossTop, side = Tile.WallBrickMossy, sideVariants = new[] { Tile.WallBrickMossy, Tile.WallBrickMossy2 }, mossySide = Tile.WallBrickMossy2, bottom = Tile.StoneRough, solid = true, occluder = true, tint = white, jitter = 0.05f };
            defs[WallDark] = new BlockDef { name = "ParedeEscura", shape = BlockShape.Full, top = Tile.WallCap, side = Tile.WallBrickDark, sideVariants = new[] { Tile.WallBrickDark }, mossySide = Tile.WallBrickMossy, bottom = Tile.StoneRoughDark, solid = true, occluder = true, tint = white, jitter = 0.04f };
            defs[Pillar] = new BlockDef { name = "Pilar", shape = BlockShape.Full, top = Tile.PillarTop, side = Tile.PillarSide, sideVariants = new[] { Tile.PillarSide }, mossySide = Tile.WallBrickMossy, bottom = Tile.StoneRough, solid = true, occluder = true, tint = white, jitter = 0.03f };
            defs[Stone] = new BlockDef { name = "Pedra", shape = BlockShape.Full, top = Tile.StoneRough, side = Tile.StoneRough, sideVariants = new[] { Tile.StoneRough, Tile.StoneRoughDark }, mossySide = Tile.WallBrickMossy, bottom = Tile.StoneRoughDark, solid = true, occluder = true, walkable = true, tint = white, jitter = 0.06f };
            defs[Dirt] = new BlockDef { name = "Terra", shape = BlockShape.Full, top = Tile.DirtTop, macroVariants = null, side = Tile.DirtSide, sideVariants = new[] { Tile.DirtSide }, mossySide = Tile.DirtSide, bottom = Tile.DirtSide, solid = true, occluder = true, walkable = true, tint = white, jitter = 0.07f };
            defs[Grass] = new BlockDef { name = "Grama", shape = BlockShape.Full, top = Tile.GrassTop, side = Tile.GrassSide, sideVariants = new[] { Tile.GrassSide }, mossySide = Tile.GrassSide, bottom = Tile.DirtSide, solid = true, occluder = true, walkable = true, tint = white, jitter = 0.08f };
            defs[Litter] = new BlockDef { name = "Folhagem", shape = BlockShape.Full, top = Tile.LeafLitter, side = Tile.DirtSide, sideVariants = new[] { Tile.DirtSide }, mossySide = Tile.DirtSide, bottom = Tile.DirtSide, solid = true, occluder = true, walkable = true, tint = white, jitter = 0.08f };
            defs[RedBrick] = new BlockDef { name = "TijoloVermelho", shape = BlockShape.Full, top = Tile.RedBrickTop, side = Tile.RedBrickSide, sideVariants = new[] { Tile.RedBrickSide }, mossySide = Tile.RedBrickSide, bottom = Tile.StoneRough, solid = true, occluder = true, tint = white, jitter = 0.04f };
            defs[Gold] = new BlockDef { name = "Latao", shape = BlockShape.Full, top = Tile.GoldTop, side = Tile.GoldSide, sideVariants = new[] { Tile.GoldSide }, mossySide = Tile.GoldSide, bottom = Tile.GoldSide, solid = true, occluder = true, tint = white, jitter = 0.02f };
            defs[Wood] = new BlockDef { name = "Madeira", shape = BlockShape.Full, top = Tile.WoodTop, side = Tile.WoodSide, sideVariants = new[] { Tile.WoodSide }, mossySide = Tile.WoodSide, bottom = Tile.WoodTop, solid = true, occluder = true, walkable = true, tint = white, jitter = 0.05f };
            defs[Carved] = new BlockDef { name = "Entalhe", shape = BlockShape.Full, top = Tile.CarvedTop, side = Tile.WallBrick, sideVariants = new[] { Tile.WallBrick }, mossySide = Tile.WallBrickMossy, bottom = Tile.StoneRough, solid = true, occluder = true, walkable = true, tint = white, jitter = 0.03f };
            defs[Path] = new BlockDef { name = "Caminho", shape = BlockShape.Full, top = Tile.PathCobble, macroVariants = null, side = Tile.StoneRough, sideVariants = new[] { Tile.StoneRough }, mossySide = Tile.WallBrickMossy, bottom = Tile.StoneRoughDark, solid = true, occluder = true, walkable = true, tint = white, jitter = 0.06f };
            defs[Water] = new BlockDef { name = "Agua", shape = BlockShape.SlabBottom, top = Tile.Water, side = Tile.Water, bottom = Tile.Water, sideVariants = new[] { Tile.Water }, mossySide = Tile.Water, solid = false, visualOnly = true, occluder = false, tint = white, jitter = 0.02f };
            defs[Leaves] = new BlockDef { name = "Folhas", shape = BlockShape.Full, top = Tile.Leaves, side = Tile.Leaves, bottom = Tile.LeavesDark, sideVariants = new[] { Tile.Leaves, Tile.LeavesDark }, mossySide = Tile.LeavesDark, solid = false, visualOnly = true, occluder = true, tint = white, jitter = 0.1f };
            defs[LeavesDark] = new BlockDef { name = "FolhasEscuras", shape = BlockShape.Full, top = Tile.LeavesDark, side = Tile.LeavesDark, bottom = Tile.LeavesDark, sideVariants = new[] { Tile.LeavesDark }, mossySide = Tile.LeavesDark, solid = false, visualOnly = true, occluder = true, tint = white, jitter = 0.1f };
            defs[StairFull] = new BlockDef { name = "DegrauCheio", shape = BlockShape.Full, macroTop = true, macroVariants = floorMacro, side = Tile.StairFront, sideVariants = new[] { Tile.StairFront }, mossySide = Tile.WallBrickMossy, bottom = Tile.StoneRough, solid = false, visualOnly = true, occluder = true, walkable = true, tint = white, jitter = 0.05f };
            defs[StairSlab] = new BlockDef { name = "Degrau", shape = BlockShape.SlabBottom, macroTop = true, macroVariants = floorMacro, side = Tile.StairFront, sideVariants = new[] { Tile.StairFront }, mossySide = Tile.StairFront, bottom = Tile.StoneRough, solid = false, visualOnly = true, occluder = false, walkable = true, tint = white, jitter = 0.05f };
            defs[FloorSlab] = new BlockDef { name = "PisoMeio", shape = BlockShape.SlabBottom, macroTop = true, macroVariants = floorMacro, side = Tile.StairFront, sideVariants = new[] { Tile.StairFront }, mossySide = Tile.StairFront, bottom = Tile.StoneRough, solid = true, occluder = false, walkable = true, tint = white, jitter = 0.05f };
            defs[Obsidian] = new BlockDef { name = "Obsidiana", shape = BlockShape.Full, top = Tile.ObsidianTop, side = Tile.ObsidianSide, sideVariants = new[] { Tile.ObsidianSide }, mossySide = Tile.ObsidianSide, bottom = Tile.ObsidianSide, solid = true, occluder = true, tint = white, jitter = 0.02f };
            defs[Bamboo] = new BlockDef { name = "Bambu", shape = BlockShape.Full, top = Tile.BambooTop, side = Tile.BambooSide, sideVariants = new[] { Tile.BambooSide }, mossySide = Tile.BambooSide, bottom = Tile.BambooTop, solid = true, occluder = true, tint = white, jitter = 0.05f };
            defs[Sand] = new BlockDef { name = "Areia", shape = BlockShape.Full, top = Tile.SandTop, side = Tile.DirtSide, sideVariants = new[] { Tile.DirtSide }, mossySide = Tile.DirtSide, bottom = Tile.DirtSide, solid = true, occluder = true, walkable = true, tint = white, jitter = 0.05f };
        }

        public bool IsFullOpaque(byte id)
        {
            var d = defs[id];
            return d.shape == BlockShape.Full && d.occluder;
        }
    }
}
