//
// Copyright (C) 1993-1996 Id Software, Inc.
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//

namespace DoomEngine.Doom.World
{
	using Audio;
	using Common;
	using Event;
	using Game;
	using Map;
	using Math;
	using UserInput;

	public sealed partial class World
	{
		private GameOptions options;
		private DoomGame game;
		private DoomRandom random;

		private Map map;

		private Thinkers thinkers;
		private Specials specials;
		private ThingAllocation thingAllocation;
		private ThingMovement thingMovement;
		private ThingInteraction thingInteraction;
		private MapCollision mapCollision;
		private MapInteraction mapInteraction;
		private PathTraversal pathTraversal;
		private Hitscan hitscan;
		private VisibilityCheck visibilityCheck;
		private SectorAction sectorAction;
		private PlayerBehavior playerBehavior;
		private ItemPickup itemPickup;
		private WeaponBehavior weaponBehavior;
		private MonsterBehavior monsterBehavior;
		private LightingChange lightingChange;
		private StatusBar statusBar;
		private AutoMap autoMap;
		private Cheat cheat;

		private int totalKills;
		private int totalItems;
		private int totalSecrets;

		private int levelTime;
		private bool doneFirstTic;
		private bool secretExit;
		private bool completed;

		private int validCount;

		public World(CommonResource resorces, GameOptions options)
			: this(resorces, options, null)
		{
		}

		public World(CommonResource resorces, GameOptions options, DoomGame game)
		{
			this.options = options;
			this.game = game;

			if (game != null)
			{
				this.random = game.Random;
			}
			else
			{
				this.random = new DoomRandom();
			}

			this.map = new Map(resorces, this);

			this.thinkers = new Thinkers(this);
			this.specials = new Specials(this);
			this.thingAllocation = new ThingAllocation(this);
			this.thingMovement = new ThingMovement(this);
			this.thingInteraction = new ThingInteraction(this);
			this.mapCollision = new MapCollision(this);
			this.mapInteraction = new MapInteraction(this);
			this.pathTraversal = new PathTraversal(this);
			this.hitscan = new Hitscan(this);
			this.visibilityCheck = new VisibilityCheck(this);
			this.sectorAction = new SectorAction(this);
			this.playerBehavior = new PlayerBehavior(this);
			this.itemPickup = new ItemPickup(this);
			this.weaponBehavior = new WeaponBehavior(this);
			this.monsterBehavior = new MonsterBehavior(this);
			this.lightingChange = new LightingChange(this);
			this.autoMap = new AutoMap(this);
			this.cheat = new Cheat(this);

			options.IntermissionInfo.ParTime = 180;

			options.Player.KillCount = 0;
			options.Player.SecretCount = 0;
			options.Player.ItemCount = 0;

			// Initial height of view will be set by player think.
			options.Player.ViewZ = Fixed.Epsilon;

			this.totalKills = 0;
			this.totalItems = 0;
			this.totalSecrets = 0;

			this.LoadThings();

			this.statusBar = new StatusBar(this);

			this.specials.SpawnSpecials();

			this.levelTime = 0;
			this.doneFirstTic = false;
			this.secretExit = false;
			this.completed = false;

			this.validCount = 0;

			options.Music.StartMusic(Map.GetMapBgm(options), true);
		}

		public UpdateResult Update()
		{
			var player = this.options.Player;

			this.playerBehavior.PlayerThink(player);

			this.thinkers.Run();
			this.specials.Update();

			this.statusBar.Update();
			this.autoMap.Update();

			this.levelTime++;

			if (this.completed)
			{
				return UpdateResult.Completed;
			}
			else
			{
				if (this.doneFirstTic)
				{
					return UpdateResult.None;
				}
				else
				{
					this.doneFirstTic = true;

					return UpdateResult.NeedWipe;
				}
			}
		}

		private void LoadThings()
		{
			for (var i = 0; i < this.map.Things.Length; i++)
			{
				var mt = this.map.Things[i];

				var spawn = true;

				// Do not spawn cool, new monsters if not commercial.
				if (DoomApplication.Instance.IWad != "doom2"
					&& DoomApplication.Instance.IWad != "freedoom2"
					&& DoomApplication.Instance.IWad != "plutonia"
					&& DoomApplication.Instance.IWad != "tnt")
				{
					switch (mt.Type)
					{
						case 68: // Arachnotron
						case 64: // Archvile
						case 88: // Boss Brain
						case 89: // Boss Shooter
						case 69: // Hell Knight
						case 67: // Mancubus
						case 71: // Pain Elemental
						case 65: // Former Human Commando
						case 66: // Revenant
						case 84: // Wolf SS
							spawn = false;

							break;
					}
				}

				if (!spawn)
				{
					break;
				}

				this.thingAllocation.SpawnMapThing(mt);
			}
		}

		public void ExitLevel()
		{
			this.secretExit = false;
			this.completed = true;
		}

		public void SecretExitLevel()
		{
			this.secretExit = true;
			this.completed = true;
		}

		public void StartSound(Mobj mobj, Sfx sfx, SfxType type)
		{
			this.options.Sound.StartSound(mobj, sfx, type);
		}

		public void StartSound(Mobj mobj, Sfx sfx, SfxType type, int volume)
		{
			this.options.Sound.StartSound(mobj, sfx, type, volume);
		}

		public void StopSound(Mobj mobj)
		{
			this.options.Sound.StopSound(mobj);
		}

		public int GetNewValidCount()
		{
			this.validCount++;

			return this.validCount;
		}

		public bool DoEvent(DoomEvent e)
		{
			if (this.options.Skill != GameSkill.Nightmare)
			{
				this.cheat.DoEvent(e);
			}

			if (this.autoMap.Visible)
			{
				if (this.autoMap.DoEvent(e))
				{
					return true;
				}
			}

			if (e.Key == DoomKey.Tab && e.Type == EventType.KeyDown)
			{
				if (this.autoMap.Visible)
				{
					this.autoMap.Close();
				}
				else
				{
					this.autoMap.Open();
				}

				return true;
			}

			if (e.Key == DoomKey.F12 && e.Type == EventType.KeyDown)
			{
				return true;
			}

			return false;
		}

		public GameOptions Options => this.options;
		public DoomGame Game => this.game;
		public DoomRandom Random => this.random;

		public Map Map => this.map;

		public Thinkers Thinkers => this.thinkers;
		public Specials Specials => this.specials;
		public ThingAllocation ThingAllocation => this.thingAllocation;
		public ThingMovement ThingMovement => this.thingMovement;
		public ThingInteraction ThingInteraction => this.thingInteraction;
		public MapCollision MapCollision => this.mapCollision;
		public MapInteraction MapInteraction => this.mapInteraction;
		public PathTraversal PathTraversal => this.pathTraversal;
		public Hitscan Hitscan => this.hitscan;
		public VisibilityCheck VisibilityCheck => this.visibilityCheck;
		public SectorAction SectorAction => this.sectorAction;
		public PlayerBehavior PlayerBehavior => this.playerBehavior;
		public ItemPickup ItemPickup => this.itemPickup;
		public WeaponBehavior WeaponBehavior => this.weaponBehavior;
		public MonsterBehavior MonsterBehavior => this.monsterBehavior;
		public LightingChange LightingChange => this.lightingChange;
		public StatusBar StatusBar => this.statusBar;
		public AutoMap AutoMap => this.autoMap;
		public Cheat Cheat => this.cheat;

		public int TotalKills
		{
			get => this.totalKills;
			set => this.totalKills = value;
		}

		public int TotalItems
		{
			get => this.totalItems;
			set => this.totalItems = value;
		}

		public int TotalSecrets
		{
			get => this.totalSecrets;
			set => this.totalSecrets = value;
		}

		public int LevelTime
		{
			get => this.levelTime;
			set => this.levelTime = value;
		}

		public int GameTic
		{
			get
			{
				if (this.game != null)
				{
					return this.game.GameTic;
				}
				else
				{
					return this.levelTime;
				}
			}
		}

		public bool SecretExit => this.secretExit;

		public bool FirstTicIsNotYetDone => this.options.Player.ViewZ == Fixed.Epsilon;
	}
}
