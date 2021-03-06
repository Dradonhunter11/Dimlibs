﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dimlibs.API.ReflectionUtils;
using Terraria;
using Terraria.ID;
using Terraria.Map;
using Terraria.Social;
using Terraria.Utilities;

namespace Dimlibs.API.DimensionHandler
{
	public sealed partial class DimensionHandler
	{
		public void Save()
		{
			if (!Directory.Exists(Path.Combine(Main.ActiveWorldFileData.Path.Replace(".wld", ""), ActiveDimensionName)))
			{
				Directory.CreateDirectory(
					Path.Combine(Main.ActiveWorldFileData.Path.Replace(".wld", ""), ActiveDimensionName));
			}
			SaveFileFormatHeader();
			SaveFileHeader();
			SaveCurrentEntity();
			SaveCurrentTile();
			SaveChest();
			SaveModdedStuff();
		}

		private void SaveFileFormatHeader()
		{
			string headerPath = Path.Combine(Main.ActiveWorldFileData.Path.Replace(".wld", ""), ActiveDimensionName) + "/TrueHeader.data";
			if (File.Exists(headerPath))
			{
				File.Delete(headerPath);
			}

			using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(headerPath)))
			{
				short num = 470;
				short num2 = 10;
				writer.Write(194);
				Main.WorldFileMetadata.IncrementAndWrite(writer);
				writer.Write(num2);
				for (int i = 0; i < (int)num2; i++)
				{
					writer.Write(0);
				}
				writer.Write(num);
				byte b = 0;
				byte b2 = 1;
				for (int i = 0; i < (int)num; i++)
				{
					if (Main.tileFrameImportant[i])
					{
						b |= b2;
					}
					if (b2 == 128)
					{
						writer.Write(b);
						b = 0;
						b2 = 1;
					}
					else
					{
						b2 = (byte)(b2 << 1);
					}
				}
				if (b2 != 1)
				{
					writer.Write(b);
				}
			}
		}

		private void SaveFileHeader()
		{
			string headerPath = Path.Combine(Main.ActiveWorldFileData.Path.Replace(".wld", ""), ActiveDimensionName) + "/header.data";
			if (File.Exists(headerPath))
			{
				File.Delete(headerPath);
			}

			using (BinaryWriter headerWriter = new BinaryWriter(File.OpenWrite(headerPath)))
			{
				headerWriter.Write(Main.maxTilesX);
				headerWriter.Write(Main.maxTilesY);

				//Save the limit of the visible world, to avoid framing issue
				headerWriter.Write((float)(Main.maxTilesX * 16 - 16 * 7)); //Main.rightWorld
				headerWriter.Write((float)(Main.maxTilesY * 16 - 16 * 5)); //Main.bottomWorld
				headerWriter.Write(Main.leftWorld);
				headerWriter.Write(Main.topWorld);

				//Save the layer
				headerWriter.Write(Main.worldSurface);
				headerWriter.Write(Main.rockLayer);

			}
		}

		private void SaveCurrentEntity()
		{
			string path = Path.Combine(Main.ActiveWorldFileData.Path.Replace(".wld", ""), ActiveDimensionName) + "/NPC.data";
			if (File.Exists(path))
			{
				File.Delete(path);
			}

			using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(path)))
			{
				int activeNPCCount = Main.npc.Count(i => i != null && !i.townNPC);
				writer.Write(activeNPCCount);
				foreach (NPC npc in Main.npc)
				{
					if (npc.townNPC)
					{
						continue;
					}

					//Save the type of NPC, will make it easier to load NPC later as a simple SetDefault is needed
					writer.Write(npc.type);

					//Save Position
					writer.Write(npc.position.X);
					writer.Write(npc.position.Y);

					//Save velocity
					writer.Write(npc.velocity.X);
					writer.Write(npc.velocity.Y);

					//Save the AI slot
					for (int i = 0; i < npc.ai.Length; i++)
					{
						writer.Write(npc.ai[i]);
					}

					//Save alt texture, might be useful?
					writer.Write(npc.altTexture);

					//Save NPC health, because that is quite important
					writer.Write(npc.life);

					//Will make it load, if the amount of buff is the same
					int buffCountAtThatTime = npc.buffTime.Length;
					writer.Write(buffCountAtThatTime);
					for (int i = 0; i < npc.buffTime.Length; i++)
					{
						writer.Write(npc.buffTime[i]);
					}


				}

				writer.Flush();
				writer.Close();
			}
		}


		private void SaveCurrentTile()
		{
			string path = Path.Combine(Main.ActiveWorldFileData.Path.Replace(".wld", ""), ActiveDimensionName) + "/Tile.data";
			if (File.Exists(path))
			{
				File.Delete(path);
			}

			using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(path)))
			{
				byte[] array = new byte[13];
				for (int i = 0; i < Main.maxTilesX; i++)
				{
					float num = (float)i / (float)Main.maxTilesX;
					Main.statusText = string.Concat(new object[]
					{
						Lang.gen[49].Value,
						" ",
						(int) (num * 100f + 1f),
						"%"
					});
					for (int j = 0; j < Main.maxTilesY; j++)
					{
						Tile tile = Main.tile[i, j];
						int num2 = 3;
						byte b3;
						byte b2;
						byte b = b2 = (b3 = 0);
						bool flag = false;
						if (tile.active() && tile.type < TileID.Count)
						{
							flag = true;
							if (tile.type == 127)
							{
								WorldGen.KillTile(i, j, false, false, false);
								if (!tile.active())
								{
									flag = false;
									if (Main.netMode != 0)
									{
										NetMessage.SendData(17, -1, -1, null, 0, (float)i, (float)j, 0f, 0, 0, 0);
									}
								}
							}
						}

						if (flag)
						{
							b2 |= 2;
							if (tile.type == 127)
							{
								WorldGen.KillTile(i, j, false, false, false);
								if (!tile.active() && Main.netMode != 0)
								{
									NetMessage.SendData(17, -1, -1, null, 0, (float)i, (float)j, 0f, 0, 0, 0);
								}
							}

							array[num2] = (byte)tile.type;
							num2++;
							if (tile.type > 255)
							{
								array[num2] = (byte)(tile.type >> 8);
								num2++;
								b2 |= 32;
							}

							if (Main.tileFrameImportant[(int)tile.type])
							{
								short frameX = tile.frameX;
								typeof(Main).Assembly.GetType("Terraria.ModLoader.IO.TileIO")
									.GetMethod("VanillaSaveFrames", BindingFlags.Static | BindingFlags.NonPublic)
									.Invoke(null, new object[] { tile, frameX });
								array[num2] = (byte)(frameX & 255);
								num2++;
								array[num2] = (byte)(((int)frameX & 65280) >> 8);
								num2++;
								array[num2] = (byte)(tile.frameY & 255);
								num2++;
								array[num2] = (byte)(((int)tile.frameY & 65280) >> 8);
								num2++;
							}

							if (tile.color() != 0)
							{
								b3 |= 8;
								array[num2] = tile.color();
								num2++;
							}
						}

						if (tile.wall != 0 && tile.wall < WallID.Count)
						{
							b2 |= 4;
							array[num2] = (byte)tile.wall;
							num2++;
							if (tile.wallColor() != 0)
							{
								b3 |= 16;
								array[num2] = tile.wallColor();
								num2++;
							}
						}

						if (tile.liquid != 0)
						{
							if (tile.lava())
							{
								b2 |= 16;
							}
							else if (tile.honey())
							{
								b2 |= 24;
							}
							else
							{
								b2 |= 8;
							}

							array[num2] = tile.liquid;
							num2++;
						}

						if (tile.wire())
						{
							b |= 2;
						}

						if (tile.wire2())
						{
							b |= 4;
						}

						if (tile.wire3())
						{
							b |= 8;
						}

						int num3;
						if (tile.halfBrick())
						{
							num3 = 16;
						}
						else if (tile.slope() != 0)
						{
							num3 = (int)(tile.slope() + 1) << 4;
						}
						else
						{
							num3 = 0;
						}

						b |= (byte)num3;
						if (tile.actuator())
						{
							b3 |= 2;
						}

						if (tile.inActive())
						{
							b3 |= 4;
						}

						if (tile.wire4())
						{
							b3 |= 32;
						}

						int num4 = 2;
						if (b3 != 0)
						{
							b |= 1;
							array[num4] = b3;
							num4--;
						}

						if (b != 0)
						{
							b2 |= 1;
							array[num4] = b;
							num4--;
						}

						short num5 = 0;
						int num6 = j + 1;
						int num7 = Main.maxTilesY - j - 1;
						while (num7 > 0 && tile.isTheSameAs(Main.tile[i, num6]))
						{
							num5 += 1;
							num7--;
							num6++;
						}

						j += (int)num5;
						if (num5 > 0)
						{
							array[num2] = (byte)(num5 & 255);
							num2++;
							if (num5 > 255)
							{
								b2 |= 128;
								array[num2] = (byte)(((int)num5 & 65280) >> 8);
								num2++;
							}
							else
							{
								b2 |= 64;
							}
						}

						array[num4] = b2;
						writer.Write(array, num4, num2 - num4);
					}
				}
			}
		}

		private void SaveModdedStuff()
		{
			string path = Path.Combine(Main.ActiveWorldFileData.Path.Replace(".wld", ""), ActiveDimensionName) + "/modded.data";

			if (File.Exists(path))
			{
				File.Delete(path);
			}

			var tag = new Terraria.ModLoader.IO.TagCompound();
			tag["tiles"] = typeof(Main).Assembly.GetType("Terraria.ModLoader.IO.TileIO")
				.GetMethod("SaveTiles", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { });
			tag["containers"] = typeof(Main).Assembly.GetType("Terraria.ModLoader.IO.TileIO")
				.GetMethod("SaveContainers", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { });




			var stream = new MemoryStream();
			Terraria.ModLoader.IO.TagIO.ToStream(tag, stream);
			var data = stream.ToArray();
			using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(path)))
			{
				writer.Write(stream.GetBuffer().Length);
				writer.Write(stream.GetBuffer());
			}
		}

		public void SaveChest()
		{
			string headerPath = Path.Combine(Main.ActiveWorldFileData.Path.Replace(".wld", ""), ActiveDimensionName) + "/chest.data";

			if (File.Exists(headerPath))
			{
				File.Delete(headerPath);
			}

			using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(headerPath)))
			{
				short num = 0;
				for (int i = 0; i < 1000; i++)
				{
					Chest chest = Main.chest[i];
					if (chest != null)
					{
						bool flag = false;
						for (int j = chest.x; j <= chest.x + 1; j++)
						{
							for (int k = chest.y; k <= chest.y + 1; k++)
							{
								if (j < 0 || k < 0 || j >= Main.maxTilesX || k >= Main.maxTilesY)
								{
									flag = true;
									break;
								}
								Tile tile = Main.tile[j, k];
								if (!tile.active() || !Main.tileContainer[(int)tile.type])
								{
									flag = true;
									break;
								}
							}
						}
						if (flag)
						{
							Main.chest[i] = null;
						}
						else
						{
							num += 1;
						}
					}
				}
				writer.Write(num);
				writer.Write((short)40);
				for (int i = 0; i < 1000; i++)
				{
					Chest chest = Main.chest[i];
					if (chest != null)
					{
						writer.Write(chest.x);
						writer.Write(chest.y);
						writer.Write(chest.name);
						for (int l = 0; l < 40; l++)
						{
							Item item = chest.item[l];
							if (item == null || item.modItem != null)
							{
								writer.Write((short)0);
							}
							else
							{
								if (item.stack > item.maxStack)
								{
									item.stack = item.maxStack;
								}
								if (item.stack < 0)
								{
									item.stack = 1;
								}
								writer.Write((short)item.stack);
								if (item.stack > 0)
								{
									writer.Write(item.netID);
									writer.Write(item.prefix);
								}
							}
						}
					}
				}
			}


		}

		public void SaveMap()
		{
			FieldInfo saveLockInfo =
				typeof(MapHelper).GetField("saveLock", BindingFlags.Static | BindingFlags.NonPublic);
			ushort modPosition = (ushort)typeof(MapHelper).GetStaticPrivateField("modPosition").GetStaticValue();
			ushort tilePosition = (ushort)typeof(MapHelper).GetStaticPrivateField("tilePosition").GetStaticValue();
			ushort wallPosition = (ushort)typeof(MapHelper).GetStaticPrivateField("wallPosition").GetStaticValue();
			ushort liquidPosition = (ushort)typeof(MapHelper).GetStaticPrivateField("liquidPosition").GetStaticValue();
			ushort dirtPosition = (ushort)typeof(MapHelper).GetStaticPrivateField("dirtPosition").GetStaticValue();
			ushort hellPosition = (ushort)typeof(MapHelper).GetStaticPrivateField("hellPosition").GetStaticValue();
			ushort skyPosition = (ushort)typeof(MapHelper).GetStaticPrivateField("skyPosition").GetStaticValue();
			ushort rockPosition = (ushort)typeof(MapHelper).GetStaticPrivateField("rockPosition").GetStaticValue();
			bool isCloudSave = Main.ActivePlayerFileData.IsCloudSave;
			if (isCloudSave && SocialAPI.Cloud == null)
			{
				return;
			}

			if (!Main.mapEnabled || (bool)saveLockInfo.GetStaticValue())
			{
				return;
			}

			string text = Main.playerPathName.Substring(0, Main.playerPathName.Length - 4);
			lock (typeof(MapHelper).GetField("padlock", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null))
			{
				try
				{
					saveLockInfo.SetStaticValue(true);
					try
					{
						if (!isCloudSave)
						{
							Directory.CreateDirectory(text);
						}
					}
					catch
					{
					}

					text += Path.DirectorySeparatorChar;
					if (Main.ActiveWorldFileData.UseGuidAsMapName)
					{
						text = text + Main.ActiveWorldFileData.UniqueId.ToString() + ".map";
					}
					else
					{
						text = text + Main.worldID + ".map";
					}

					Stopwatch stopwatch = new Stopwatch();
					stopwatch.Start();
					bool flag2 = false;
					if (!Main.gameMenu)
					{
						flag2 = true;
					}

					using (MemoryStream memoryStream = new MemoryStream(4000))
					{
						using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
						{
							using (DeflateStream deflateStream =
								new DeflateStream(memoryStream, CompressionMode.Compress))
							{
								int num = 0;
								byte[] array = new byte[16384];
								binaryWriter.Write(194);
								Main.MapFileMetadata.IncrementAndWrite(binaryWriter);
								binaryWriter.Write(Main.worldName);
								binaryWriter.Write(Main.worldID);
								binaryWriter.Write(Main.maxTilesY);
								binaryWriter.Write(Main.maxTilesX);
								binaryWriter.Write((short)470);
								binaryWriter.Write((short)231);
								binaryWriter.Write((short)3);
								binaryWriter.Write((short)256);
								binaryWriter.Write((short)256);
								binaryWriter.Write((short)256);
								byte b = 1;
								byte b2 = 0;
								int i;
								for (i = 0; i < 470; i++)
								{
									if (MapHelper.tileOptionCounts[i] != 1)
									{
										b2 |= b;
									}

									if (b == 128)
									{
										binaryWriter.Write(b2);
										b2 = 0;
										b = 1;
									}
									else
									{
										b = (byte)(b << 1);
									}
								}

								if (b != 1)
								{
									binaryWriter.Write(b2);
								}

								i = 0;
								b = 1;
								b2 = 0;
								while (i < 231)
								{
									if (MapHelper.wallOptionCounts[i] != 1)
									{
										b2 |= b;
									}

									if (b == 128)
									{
										binaryWriter.Write(b2);
										b2 = 0;
										b = 1;
									}
									else
									{
										b = (byte)(b << 1);
									}

									i++;
								}

								if (b != 1)
								{
									binaryWriter.Write(b2);
								}

								for (i = 0; i < 470; i++)
								{
									if (MapHelper.tileOptionCounts[i] != 1)
									{
										binaryWriter.Write((byte)MapHelper.tileOptionCounts[i]);
									}
								}

								for (i = 0; i < 231; i++)
								{
									if (MapHelper.wallOptionCounts[i] != 1)
									{
										binaryWriter.Write((byte)MapHelper.wallOptionCounts[i]);
									}
								}

								binaryWriter.Flush();
								for (int j = 0; j < Main.maxTilesY; j++)
								{
									if (!flag2)
									{
										float num2 = (float)j / (float)Main.maxTilesY;
										Main.statusText = string.Concat(new object[]
										{
											Lang.gen[66].Value,
											" ",
											(int) (num2 * 100f + 1f),
											"%"
										});
									}

									for (int k = 0; k < Main.maxTilesX; k++)
									{
										MapTile mapTile = Main.Map[k, j];
										byte b4;
										byte b3 = b4 = 0;
										bool flag3 = true;
										bool flag4 = true;
										int num3 = 0;
										int num4 = 0;
										byte b5 = 0;
										int num5;
										ushort num6;
										int num7;
										if (mapTile.Light <= 18 || mapTile.Type >= modPosition)
										{
											flag4 = false;
											flag3 = false;
											num5 = 0;
											num6 = 0;
											num7 = 0;
											int num8 = k + 1;
											int l = Main.maxTilesX - k - 1;
											while (l > 0)
											{
												if (Main.Map[num8, j].Light > 18)
												{
													break;
												}

												num7++;
												l--;
												num8++;
											}
										}
										else
										{
											b5 = mapTile.Color;
											num6 = mapTile.Type;
											if (num6 < wallPosition)
											{
												num5 = 1;
												num6 -= tilePosition;
											}
											else if (num6 < liquidPosition)
											{
												num5 = 2;
												num6 -= wallPosition;
											}
											else if (num6 < skyPosition)
											{
												num5 = (int)(3 + (num6 - liquidPosition));
												flag3 = false;
											}
											else if (num6 < dirtPosition)
											{
												num5 = 6;
												flag4 = false;
												flag3 = false;
											}
											else if (num6 < hellPosition)
											{
												num5 = 7;
												if (num6 < rockPosition)
												{
													num6 -= dirtPosition;
												}
												else
												{
													num6 -= rockPosition;
												}
											}
											else
											{
												num5 = 6;
												flag3 = false;
											}

											if (mapTile.Light == 255)
											{
												flag4 = false;
											}

											if (flag4)
											{
												num7 = 0;
												int num8 = k + 1;
												int l = Main.maxTilesX - k - 1;
												num3 = num8;
												while (l > 0)
												{
													MapTile mapTile2 = Main.Map[num8, j];
													if (!mapTile.EqualsWithoutLight(ref mapTile2))
													{
														num4 = num8;
														break;
													}

													l--;
													num7++;
													num8++;
												}
											}
											else
											{
												num7 = 0;
												int num8 = k + 1;
												int l = Main.maxTilesX - k - 1;
												while (l > 0)
												{
													MapTile mapTile3 = Main.Map[num8, j];
													if (!mapTile.Equals(ref mapTile3))
													{
														break;
													}

													l--;
													num7++;
													num8++;
												}
											}
										}

										if (b5 > 0)
										{
											b3 |= (byte)(b5 << 1);
										}

										if (b3 != 0)
										{
											b4 |= 1;
										}

										b4 |= (byte)(num5 << 1);
										if (flag3 && num6 > 255)
										{
											b4 |= 16;
										}

										if (flag4)
										{
											b4 |= 32;
										}

										if (num7 > 0)
										{
											if (num7 > 255)
											{
												b4 |= 128;
											}
											else
											{
												b4 |= 64;
											}
										}

										array[num] = b4;
										num++;
										if (b3 != 0)
										{
											array[num] = b3;
											num++;
										}

										if (flag3)
										{
											array[num] = (byte)num6;
											num++;
											if (num6 > 255)
											{
												array[num] = (byte)(num6 >> 8);
												num++;
											}
										}

										if (flag4)
										{
											array[num] = mapTile.Light;
											num++;
										}

										if (num7 > 0)
										{
											array[num] = (byte)num7;
											num++;
											if (num7 > 255)
											{
												array[num] = (byte)(num7 >> 8);
												num++;
											}
										}

										for (int m = num3; m < num4; m++)
										{
											array[num] = Main.Map[m, j].Light;
											num++;
										}

										k += num7;
										if (num >= 4096)
										{
											deflateStream.Write(array, 0, num);
											num = 0;
										}
									}
								}

								if (num > 0)
								{
									deflateStream.Write(array, 0, num);
								}

								deflateStream.Dispose();
								FileUtilities.WriteAllBytes(text, memoryStream.ToArray(), isCloudSave);
								//patch file: text
							}
						}
					}

					typeof(Main).Assembly.GetType("Terraria.ModLoader.IO.MapIO")
						.InvokeStaticMethod("WriteModFile", new object[] { text, isCloudSave });
				}
				catch (Exception e)
				{

				}
			}
		}
	}
}
