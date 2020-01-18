﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManagedDoom;

namespace ManagedDoomTest
{
    [TestClass]
    public class BlockMapTest
    {
        [TestMethod]
        public void LoadE1M1()
        {
            using (var wad = new Wad(WadPath.Doom1))
            {
                var flats = new FlatLookup(wad);
                var textures = new TextureLookup(wad);
                var map = wad.GetLumpNumber("E1M1");
                var vertices = Vertex.FromWad(wad, map + 4);
                var sectors = Sector.FromWad(wad, map + 8, flats);
                var sides = SideDef.FromWad(wad, map + 3, textures, sectors);
                var lines = LineDef.FromWad(wad, map + 2, vertices, sides);
                var blockMap = BlockMap.FromWad(wad, map + 10, lines);

                {
                    var minX = vertices.Select(v => v.X.ToDouble()).Min();
                    var maxX = vertices.Select(v => v.X.ToDouble()).Max();
                    var minY = vertices.Select(v => v.Y.ToDouble()).Min();
                    var maxY = vertices.Select(v => v.Y.ToDouble()).Max();

                    Assert.AreEqual(blockMap.OriginX.ToDouble(), minX, 64);
                    Assert.AreEqual(blockMap.OriginY.ToDouble(), minY, 64);
                    Assert.AreEqual((blockMap.OriginX + BlockMap.MapBlockSize * blockMap.Width).ToDouble(), maxX, 128);
                    Assert.AreEqual((blockMap.OriginY + BlockMap.MapBlockSize * blockMap.Height).ToDouble(), maxY, 128);
                }

                var total = 0;
                for (var blockY = -2; blockY < blockMap.Height + 2; blockY++)
                {
                    for (var blockX = -2; blockX < blockMap.Width + 2; blockX++)
                    {
                        var minX = double.MaxValue;
                        var maxX = double.MinValue;
                        var minY = double.MaxValue;
                        var maxY = double.MinValue;
                        var count = 0;

                        blockMap.EnumBlockLines(
                            blockX,
                            blockY,
                            line =>
                            {
                                if (count != 0)
                                {
                                    minX = Math.Min(Math.Min(line.Vertex1.X.ToDouble(), line.Vertex2.X.ToDouble()), minX);
                                    maxX = Math.Max(Math.Max(line.Vertex1.X.ToDouble(), line.Vertex2.X.ToDouble()), maxX);
                                    minY = Math.Min(Math.Min(line.Vertex1.Y.ToDouble(), line.Vertex2.Y.ToDouble()), minY);
                                    maxY = Math.Max(Math.Max(line.Vertex1.Y.ToDouble(), line.Vertex2.Y.ToDouble()), maxY);
                                }
                                count++;
                                return true;
                            },
                            1);

                        if (count > 1)
                        {
                            Assert.IsTrue(minX <= (blockMap.OriginX + BlockMap.MapBlockSize * (blockX + 1)).ToDouble());
                            Assert.IsTrue(maxX >= (blockMap.OriginX + BlockMap.MapBlockSize * blockX).ToDouble());
                            Assert.IsTrue(minY <= (blockMap.OriginY + BlockMap.MapBlockSize * (blockY + 1)).ToDouble());
                            Assert.IsTrue(maxY >= (blockMap.OriginY + BlockMap.MapBlockSize * blockY).ToDouble());
                        }

                        total += count;
                    }
                }

                Assert.AreEqual(lines.Length, total);
            }
        }

        [TestMethod]
        public void LoadMap01()
        {
            using (var wad = new Wad(WadPath.Doom2))
            {
                var flats = new FlatLookup(wad);
                var textures = new TextureLookup(wad);
                var map = wad.GetLumpNumber("MAP01");
                var vertices = Vertex.FromWad(wad, map + 4);
                var sectors = Sector.FromWad(wad, map + 8, flats);
                var sides = SideDef.FromWad(wad, map + 3, textures, sectors);
                var lines = LineDef.FromWad(wad, map + 2, vertices, sides);
                var blockMap = BlockMap.FromWad(wad, map + 10, lines);

                {
                    var minX = vertices.Select(v => v.X.ToDouble()).Min();
                    var maxX = vertices.Select(v => v.X.ToDouble()).Max();
                    var minY = vertices.Select(v => v.Y.ToDouble()).Min();
                    var maxY = vertices.Select(v => v.Y.ToDouble()).Max();

                    Assert.AreEqual(blockMap.OriginX.ToDouble(), minX, 64);
                    Assert.AreEqual(blockMap.OriginY.ToDouble(), minY, 64);
                    Assert.AreEqual((blockMap.OriginX + BlockMap.MapBlockSize * blockMap.Width).ToDouble(), maxX, 128);
                    Assert.AreEqual((blockMap.OriginY + BlockMap.MapBlockSize * blockMap.Height).ToDouble(), maxY, 128);
                }

                var total = 0;
                for (var blockY = -2; blockY < blockMap.Height + 2; blockY++)
                {
                    for (var blockX = -2; blockX < blockMap.Width + 2; blockX++)
                    {
                        var minX = double.MaxValue;
                        var maxX = double.MinValue;
                        var minY = double.MaxValue;
                        var maxY = double.MinValue;
                        var count = 0;

                        blockMap.EnumBlockLines(
                            blockX,
                            blockY,
                            line =>
                            {
                                if (count != 0)
                                {
                                    minX = Math.Min(Math.Min(line.Vertex1.X.ToDouble(), line.Vertex2.X.ToDouble()), minX);
                                    maxX = Math.Max(Math.Max(line.Vertex1.X.ToDouble(), line.Vertex2.X.ToDouble()), maxX);
                                    minY = Math.Min(Math.Min(line.Vertex1.Y.ToDouble(), line.Vertex2.Y.ToDouble()), minY);
                                    maxY = Math.Max(Math.Max(line.Vertex1.Y.ToDouble(), line.Vertex2.Y.ToDouble()), maxY);
                                }
                                count++;
                                return true;
                            },
                            1);

                        if (count > 1)
                        {
                            Assert.IsTrue(minX <= (blockMap.OriginX + BlockMap.MapBlockSize * (blockX + 1)).ToDouble());
                            Assert.IsTrue(maxX >= (blockMap.OriginX + BlockMap.MapBlockSize * blockX).ToDouble());
                            Assert.IsTrue(minY <= (blockMap.OriginY + BlockMap.MapBlockSize * (blockY + 1)).ToDouble());
                            Assert.IsTrue(maxY >= (blockMap.OriginY + BlockMap.MapBlockSize * blockY).ToDouble());
                        }

                        total += count;
                    }
                }

                Assert.AreEqual(lines.Length, total);
            }
        }
    }
}