using System;
using System.IO;
using System.Collections.Generic;
using Pipliz;
using Pipliz.Chatting;
using Pipliz.JSON;
using Pipliz.Threading;
using Pipliz.APIProvider.Recipes;
using Pipliz.APIProvider.Jobs;
using NPC;
using UnityEngine;

namespace ScarabolMods
{
  [ModLoader.ModManager]
  public static class GuardianKnightsModEntries
  {
    public static string MOD_PREFIX = "mods.scarabol.guardianknights.";
    public static string KNIGHT_ITEM_KEY = MOD_PREFIX + "knight";
    public static string PLATFORM_ITEM_KEY = MOD_PREFIX + "platform";
    public static string JOB_TOOL_KEY = MOD_PREFIX + "sword";
    private static string AssetsDirectory;
    private static string RelativeTexturesPath;
    private static string RelativeIconsPath;
    private static string RelativeMeshesPath;
    private static string RelativeAudioPath;
    private static Recipe recipeKnight;
    private static Recipe recipeWoodenPlatform;
    private static Recipe recipeSword;
    public static Dictionary<Players.Player, GuardianKnightJob> LastPlacedJobs = new Dictionary<Players.Player, GuardianKnightJob> ();

    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnAssemblyLoaded, "scarabol.guardianknights.assemblyload")]
    public static void OnAssemblyLoaded (string path)
    {
      AssetsDirectory = Path.Combine (Path.GetDirectoryName (path), "assets");
      ModLocalizationHelper.localize (Path.Combine (AssetsDirectory, "localization"), MOD_PREFIX, false);
      // TODO this is really hacky (maybe better in future ModAPI)
      RelativeTexturesPath = new Uri (MultiPath.Combine (Path.GetFullPath ("gamedata"), "textures", "materials", "blocks", "albedo", "dummyfile")).MakeRelativeUri (new Uri (MultiPath.Combine (AssetsDirectory, "textures", "albedo"))).OriginalString;
      RelativeIconsPath = new Uri (MultiPath.Combine (Path.GetFullPath ("gamedata"), "textures", "icons", "dummyfile")).MakeRelativeUri (new Uri (MultiPath.Combine (AssetsDirectory, "icons"))).OriginalString;
      RelativeMeshesPath = new Uri (MultiPath.Combine (Path.GetFullPath ("gamedata"), "meshes", "dummyfile")).MakeRelativeUri (new Uri (Path.Combine (AssetsDirectory, "meshes"))).OriginalString;
      RelativeAudioPath = new Uri (MultiPath.Combine (Path.GetFullPath ("gamedata"), "audio", "dummyfile")).MakeRelativeUri (new Uri (Path.Combine (AssetsDirectory, "audio"))).OriginalString;
      ModAudioHelper.IntegrateAudio (Path.Combine (AssetsDirectory, "audio"), MOD_PREFIX, RelativeAudioPath);
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterStartup, "scarabol.guardianknights.registercallbacks")]
    public static void AfterStartup ()
    {
      Pipliz.Log.Write ("Loaded GuardianKnights Mod 1.2 by Scarabol");
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterDefiningNPCTypes, "scarabol.guardianknights.registerjobs")]
    [ModLoader.ModCallbackProvidesFor ("pipliz.apiprovider.jobs.resolvetypes")]
    public static void AfterDefiningNPCTypes ()
    {
      BlockJobManagerTracker.Register<GuardianKnightJob> (KNIGHT_ITEM_KEY);
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterAddingBaseTypes, "scarabol.guardianknights.addrawtypes")]
    public static void AfterAddingBaseTypes ()
    {
      ItemTypesServer.AddTextureMapping (KNIGHT_ITEM_KEY, new JSONNode ()
        .SetAs ("albedo", Path.Combine (RelativeTexturesPath, "knight"))
        .SetAs ("normal", "neutral")
        .SetAs ("emissive", "neutral")
        .SetAs ("height", "neutral")
      );
      ItemTypes.AddRawType (KNIGHT_ITEM_KEY, new JSONNode ()
        .SetAs ("icon", Path.Combine (RelativeIconsPath, "knight.png"))
        .SetAs ("maxStackSize", 400)
        .SetAs ("needsBase", true)
        .SetAs ("isSolid", false)
        .SetAs ("npcLimit", 0)
        .SetAs ("sideall", "SELF")
        .SetAs ("isRotatable", true)
        .SetAs ("rotatablex+", KNIGHT_ITEM_KEY + "x+")
        .SetAs ("rotatablex-", KNIGHT_ITEM_KEY + "x-")
        .SetAs ("rotatablez+", KNIGHT_ITEM_KEY + "z+")
        .SetAs ("rotatablez-", KNIGHT_ITEM_KEY + "z-")
      );
      foreach (string xz in new string[] { "x+", "x-", "z+", "z-" }) {
        ItemTypes.AddRawType (KNIGHT_ITEM_KEY + xz, new JSONNode ()
          .SetAs ("parentType", KNIGHT_ITEM_KEY)
          .SetAs ("mesh", Path.Combine (RelativeMeshesPath, "sword" + xz + ".obj"))
        );
      }
      ItemTypesServer.AddTextureMapping (PLATFORM_ITEM_KEY, new JSONNode ()
        .SetAs ("albedo", Path.Combine (RelativeTexturesPath, "woodenplatform"))
        .SetAs ("normal", "neutral")
        .SetAs ("emissive", "neutral")
        .SetAs ("height", "neutral")
      );
      ItemTypes.AddRawType (PLATFORM_ITEM_KEY, new JSONNode ()
        .SetAs ("icon", Path.Combine (RelativeIconsPath, "woodenplatform.png"))
        .SetAs ("maxStackSize", 400)
        .SetAs ("needsBase", true)
        .SetAs ("isSolid", false)
        .SetAs ("npcLimit", 0)
        .SetAs ("sideall", "SELF")
        .SetAs ("mesh", Path.Combine (RelativeMeshesPath, "woodenplatform.obj"))
      );
      ItemTypes.AddRawType (JOB_TOOL_KEY, new JSONNode ()
        .SetAs ("npcLimit", 1)
        .SetAs ("icon", Path.Combine (RelativeIconsPath, "sword.png"))
        .SetAs ("isPlaceable", false)
      );
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterItemTypesDefined, "scarabol.guardianknights.loadrecipes")]
    [ModLoader.ModCallbackDependsOn ("pipliz.blocknpcs.loadrecipes")]
    [ModLoader.ModCallbackProvidesFor ("pipliz.apiprovider.registerrecipes")]
    public static void AfterItemTypesDefined ()
    {
      recipeKnight = new Recipe (
        new List<InventoryItem> () { new InventoryItem ("ironingot", 3), new InventoryItem ("planks", 1) },
        new InventoryItem (KNIGHT_ITEM_KEY, 1)
      );
      recipeWoodenPlatform = new Recipe (new InventoryItem ("planks", 5), new InventoryItem (PLATFORM_ITEM_KEY, 1));
      recipeSword = new Recipe (new InventoryItem ("ironingot", 1), new InventoryItem (JOB_TOOL_KEY, 1));
      RecipeManager.AddRecipes ("pipliz.crafter", new List<Recipe> () {
        recipeWoodenPlatform,
        recipeKnight,
        recipeSword
      });
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterItemTypesServer, "scarabol.guardianknights.registercallbacks")]
    public static void AfterItemTypesServer ()
    {
      ItemTypesServer.RegisterOnAdd (PLATFORM_ITEM_KEY, KnightBlockCode.OnAddPlatform);
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterWorldLoad, "scarabol.guardianknights.addplayercrafts")]
    public static void AfterWorldLoad ()
    {
      // add recipes here, otherwise they're inserted before vanilla recipes in player crafts
      RecipePlayer.AllRecipes.Add (recipeKnight);
      RecipePlayer.AllRecipes.Add (recipeWoodenPlatform);
      RecipePlayer.AllRecipes.Add (recipeSword);
    }
  }

  public static class KnightBlockCode
  {
    public static void OnAddPlatform (Vector3Int position, ushort newtype, Players.Player causedBy)
    {
      GuardianKnightJob lastJob;
      if (GuardianKnightsModEntries.LastPlacedJobs.TryGetValue (causedBy, out lastJob)) {
        lastJob.waypoint = position;
        GuardianKnightsModEntries.LastPlacedJobs.Remove (causedBy);
      }
    }
  }

  public class GuardianKnightJob : BlockJobBase, IBlockJobBase, INPCTypeDefiner
  {
    string jobtypename;
    Vector3Int jobdirvec;
    public Vector3Int waypoint = Vector3Int.invalidPos;
    Vector3Int moveTarget = Vector3Int.invalidPos;
    Zombie target;

    public override string NPCTypeKey { get { return "scarabol.guardianknight"; } }

    public override float TimeBetweenJobs { get { return 1.5f; } }

    public override bool ToSleep { get { return false; } }

    public override InventoryItem RecruitementItem { get { return new InventoryItem (ItemTypes.IndexLookup.GetIndex (GuardianKnightsModEntries.JOB_TOOL_KEY), 1); } }

    public ITrackableBlock InitializeOnAdd (Vector3Int position, ushort type, Players.Player player)
    {
      GuardianKnightsModEntries.LastPlacedJobs [player] = this;
      jobtypename = ItemTypes.IndexLookup.GetName (type);
      jobdirvec = TypeHelper.RotatableToVector (jobtypename);
      InitializeJob (player, position, 0);
      return this;
    }

    public override ITrackableBlock InitializeFromJSON (Players.Player player, JSONNode node)
    {
      jobtypename = node.GetAs<string> ("jobtypename");
      jobdirvec = TypeHelper.RotatableToVector (jobtypename);
      waypoint = (Vector3Int)node ["waypoint"];
      InitializeJob (player, (Vector3Int)node ["position"], node.GetAs<int> ("npcID"));
      return this;
    }

    public override JSONNode GetJSON ()
    {
      return base.GetJSON ()
        .SetAs ("jobtypename", jobtypename)
        .SetAs ("waypoint", (JSONNode)waypoint);
    }

    public override Vector3Int GetJobLocation ()
    {
      if (!waypoint.IsValid) {
        return base.GetJobLocation ();
      } else if (!moveTarget.IsValid) {
        moveTarget = position + new Vector3Int ((waypoint - position).Vector * Pipliz.Random.NextFloat (0, 1));
      }
      return moveTarget;
    }

    public override void OnNPCDoJob (ref NPCBase.NPCState state)
    {
      if (target != null && target.IsValid) {
        Vector3 npcPos = usedNPC.Position + Vector3.up;
        Vector3 targetPos = target.Position + Vector3.up;
        if (General.Physics.Physics.CanSee (npcPos, targetPos)) {
          usedNPC.LookAt (targetPos);
          target.Ragdoll ();
          ServerManager.SendAudio (position.Vector, GuardianKnightsModEntries.MOD_PREFIX + "swordCut");
          ZombieTracker.Remove (target);
          OverrideCooldown (3.0);
        } else {
          target = null;
        }
      }
      if (target == null || !target.IsValid) {
        target = ZombieTracker.Find (new Vector3Int (usedNPC.Position) + Vector3Int.up, 2);
        if (target != null) {
          OverrideCooldown (0.2);
        } else if (moveTarget.IsValid) {
          usedNPC.LookAt (moveTarget.Vector);
          moveTarget = Vector3Int.invalidPos;
        } else {
          usedNPC.LookAt ((position + jobdirvec).Vector);
        }
      }
    }

    public override void OnRemove ()
    {
      GuardianKnightJob lastJob;
      if (GuardianKnightsModEntries.LastPlacedJobs.TryGetValue (this.owner, out lastJob)) {
        GuardianKnightsModEntries.LastPlacedJobs.Remove (this.owner);
      }
      base.OnRemove ();
    }

    NPCTypeSettings INPCTypeDefiner.GetNPCTypeDefinition ()
    {
      NPCTypeSettings def = NPCTypeSettings.Default;
      def.keyName = NPCTypeKey;
      def.printName = "Guardian Knight";
      def.maskColor1 = new Color32 (32, 32, 32, 255);
      def.type = NPCTypeID.GetNextID ();
      return def;
    }
  }
}
