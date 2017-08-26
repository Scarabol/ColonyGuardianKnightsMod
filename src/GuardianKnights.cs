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
    public static string JOB_ITEM_KEY = MOD_PREFIX + "knight";
    public static string JOB_TOOL_KEY = MOD_PREFIX + "sword";
    private static string AssetsDirectory;
    private static string RelativeTexturesPath;
    private static string RelativeIconsPath;
    private static string RelativeMeshesPath;
    private static Recipe recipeKnight;
    private static Recipe recipeSword;

    [ModLoader.ModCallback(ModLoader.EModCallbackType.OnAssemblyLoaded, "scarabol.guardianknights.assemblyload")]
    public static void OnAssemblyLoaded(string path)
    {
      AssetsDirectory = Path.Combine(Path.GetDirectoryName(path), "assets");
      ModLocalizationHelper.localize(Path.Combine(AssetsDirectory, "localization"), MOD_PREFIX, false);
      // TODO this is really hacky (maybe better in future ModAPI)
      RelativeTexturesPath = new Uri(MultiPath.Combine(Path.GetFullPath("gamedata"), "textures", "materials", "blocks", "albedo", "dummyfile")).MakeRelativeUri(new Uri(MultiPath.Combine(AssetsDirectory, "textures", "albedo"))).OriginalString;
      RelativeIconsPath = new Uri(MultiPath.Combine(Path.GetFullPath("gamedata"), "textures", "icons", "dummyfile")).MakeRelativeUri(new Uri(MultiPath.Combine(AssetsDirectory, "icons"))).OriginalString;
      RelativeMeshesPath = new Uri(MultiPath.Combine(Path.GetFullPath("gamedata"), "meshes", "dummyfile")).MakeRelativeUri(new Uri(Path.Combine(AssetsDirectory, "meshes"))).OriginalString;
    }

    [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterStartup, "scarabol.guardianknights.registercallbacks")]
    public static void AfterStartup()
    {
      Pipliz.Log.Write("Loaded GuardianKnights Mod 1.1 by Scarabol");
    }

    [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterDefiningNPCTypes, "scarabol.guardianknights.registerjobs")]
    [ModLoader.ModCallbackProvidesFor("pipliz.apiprovider.jobs.resolvetypes")]
    public static void AfterDefiningNPCTypes()
    {
      BlockJobManagerTracker.Register<GuardianKnightJob>(JOB_ITEM_KEY);
    }

    [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterAddingBaseTypes, "scarabol.guardianknights.addrawtypes")]
    public static void AfterAddingBaseTypes()
    {
      ItemTypesServer.AddTextureMapping(JOB_ITEM_KEY, new JSONNode()
                                        .SetAs("albedo", Path.Combine(RelativeTexturesPath, "knight"))
                                        .SetAs("normal", "neutral")
                                        .SetAs("emissive", "neutral")
                                        .SetAs("height", "neutral")
      );
      ItemTypes.AddRawType(JOB_ITEM_KEY, new JSONNode(NodeType.Object)
                           .SetAs("icon", Path.Combine(RelativeIconsPath, "knight.png"))
                           .SetAs("maxStackSize", 400)
                           .SetAs("needsBase", true)
                           .SetAs("isSolid", false)
                           .SetAs("npcLimit", 0)
                           .SetAs("sideall", "SELF")
                           .SetAs("isRotatable", true)
                           .SetAs("rotatablex+", JOB_ITEM_KEY + "x+")
                           .SetAs("rotatablex-", JOB_ITEM_KEY + "x-")
                           .SetAs("rotatablez+", JOB_ITEM_KEY + "z+")
                           .SetAs("rotatablez-", JOB_ITEM_KEY + "z-")
      );
      foreach (string xz in new string[] { "x+", "x-", "z+", "z-" }) {
        ItemTypes.AddRawType(JOB_ITEM_KEY + xz, new JSONNode()
                             .SetAs("parentType", JOB_ITEM_KEY)
                             .SetAs("mesh", Path.Combine(RelativeMeshesPath, "sword" + xz + ".obj"))
        );
      }
      ItemTypes.AddRawType(JOB_TOOL_KEY, new JSONNode(NodeType.Object)
                           .SetAs("npcLimit", 1)
                           .SetAs("icon", Path.Combine(RelativeIconsPath, "sword.png"))
                           .SetAs("isPlaceable", false)
      );
    }

    [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, "scarabol.guardianknights.loadrecipes")]
    [ModLoader.ModCallbackDependsOn("pipliz.blocknpcs.loadrecipes")]
    [ModLoader.ModCallbackProvidesFor("pipliz.apiprovider.registerrecipes")]
    public static void AfterItemTypesDefined()
    {
      recipeKnight = new Recipe(new List<InventoryItem>() { new InventoryItem("ironingot", 3), new InventoryItem("planks", 1) }, new InventoryItem(JOB_ITEM_KEY, 1));
      recipeSword = new Recipe(new InventoryItem("ironingot", 1), new InventoryItem(JOB_TOOL_KEY, 1));
      RecipeManager.AddRecipes("pipliz.crafter", new List<Recipe>() { recipeKnight, recipeSword });
    }

    [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterWorldLoad, "scarabol.guardianknights.addplayercrafts")]
    public static void AfterWorldLoad()
    {
      // add recipes here, otherwise they're inserted before vanilla recipes in player crafts
      RecipePlayer.AllRecipes.Add(recipeKnight);
      RecipePlayer.AllRecipes.Add(recipeSword);
    }
  }

  public class GuardianKnightJob : BlockJobBase, IBlockJobBase, INPCTypeDefiner
  {
    ushort knightType;
    Zombie target;

    public override string NPCTypeKey { get { return "scarabol.guardknight"; } }

    public override float TimeBetweenJobs { get { return 1.2f; } }

    public override bool ToSleep { get { return false; } }

    public override InventoryItem RecruitementItem { get { return new InventoryItem(ItemTypes.IndexLookup.GetIndex(GuardianKnightsModEntries.JOB_TOOL_KEY), 1); } }

    public override ITrackableBlock InitializeFromJSON (Players.Player player, JSONNode node)
    {
      knightType = ItemTypes.IndexLookup.GetIndex(node.GetAs<string>("type"));
      InitializeJob(player, (Vector3Int)node["position"], node.GetAs<int>("npcID"));
      return this;
    }

    public ITrackableBlock InitializeOnAdd (Vector3Int position, ushort type, Players.Player player)
    {
      knightType = type;
      InitializeJob(player, position, 0);
      return this;
    }

    public override JSONNode GetJSON ()
    {
      return base.GetJSON()
        .SetAs("type", ItemTypes.IndexLookup.GetName(knightType));
    }

    public override void OnNPCDoJob (ref NPCBase.NPCState state)
    {
      if (target != null && target.IsValid) {
        Vector3 npcPos = usedNPC.Position + Vector3.up;
        Vector3 targetPos = target.Position + Vector3.up;
        if (General.Physics.Physics.CanSee(npcPos, targetPos)) {
          usedNPC.LookAt(targetPos);
          Arrow.New(npcPos, targetPos, target.Direction);
        } else {
          target = null;
        }
      }
      if (target == null || !target.IsValid) {
        target = ZombieTracker.Find(position.Add(0, 1, 0), 3);
        if (target == null) {
          Vector3 desiredPosition = usedNPC.Position;
          if (knightType == ItemTypes.IndexLookup.GetIndex(GuardianKnightsModEntries.JOB_ITEM_KEY + "x-")) {
            desiredPosition += Vector3.left;
          } else if (knightType == ItemTypes.IndexLookup.GetIndex(GuardianKnightsModEntries.JOB_ITEM_KEY + "x+")) {
            desiredPosition += Vector3.right;
          } else if (knightType == ItemTypes.IndexLookup.GetIndex(GuardianKnightsModEntries.JOB_ITEM_KEY + "z+")) {
            desiredPosition += Vector3.forward;
          } else if (knightType == ItemTypes.IndexLookup.GetIndex(GuardianKnightsModEntries.JOB_ITEM_KEY + "z-")) {
            desiredPosition += Vector3.back;
          }
          usedNPC.LookAt(desiredPosition);
        } else {
          OverrideCooldown(0.3);
        }
      }
    }

    NPCTypeSettings INPCTypeDefiner.GetNPCTypeDefinition ()
    {
      NPCTypeSettings def = NPCTypeSettings.Default;
      def.keyName = NPCTypeKey;
      def.printName = "Knight guard";
      def.maskColor1 = new Color32(32, 32, 32, 255);
      def.type = NPCTypeID.GetNextID();
      return def;
    }
  }

}
