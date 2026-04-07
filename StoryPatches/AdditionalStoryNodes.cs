using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CobaltCoreArchipelago.GameplayPatches;
using CobaltCoreArchipelago.Map;

namespace CobaltCoreArchipelago.StoryPatches;

internal class CustomSay : Say
{
    internal string[] lineKey = [];

    private string LocalizedLine => ModEntry.Instance.Localizations.Localize(lineKey);
    private string Hash => lineKey.Aggregate((a, b) => a + ':' + b);

    public override bool Execute(G g, IScriptTarget target, ScriptCtx ctx)
    {
        if (lineKey.Length == 0) return base.Execute(g, target, ctx);

        hash = $"{GetType().FullName}:{Hash}";
        DB.currentLocale.strings[GetLocKey(ctx.script, hash)] = LocalizedLine;
        return base.Execute(g, target, ctx);
    }
}

internal class AdditionalStoryNodes
{
    internal static string AmDizzy => Deck.dizzy.Key();
    internal static string AmRiggs => Deck.riggs.Key();
    internal static string AmPeri => Deck.peri.Key();
    internal static string AmIsaac => Deck.goat.Key();
    internal static string AmDrake => Deck.eunice.Key();
    internal static string AmMax => Deck.hacker.Key();
    internal static string AmBooks => Deck.shard.Key();
    internal static string AmCat => "comp";
    internal static string AmCatDeck => Deck.colorless.Key();
    internal static string AmVoid => "void";
    internal static string AmCleo => "nerd";

    internal static void Register(Dictionary<string, StoryNode> nodes)
    {
        foreach (var kvp in nodes)
        {
            DB.story.all[kvp.Key] = kvp.Value;
        }
    }

    internal static void Register(Dictionary<string, MethodInfo> choiceFuncs)
    {
        foreach (var kvp in choiceFuncs)
        {
            DB.eventChoiceFns[kvp.Key] = kvp.Value;
        }
    }

    internal static readonly Dictionary<string, StoryNode> eventNodes = new()
    {
        {
            "saltyisaac_archipelago_ShopMissedItem",
            new StoryNode
            {
                type = NodeType.@event,
                lookup = [ "saltyisaac_archipelago_shopMissedItem" ],
                bg = "BGShop",
                lines =
                [
                    new CustomSay
                    {
                        who = AmCleo,
                        flipped = true,
                        lineKey = ["story", "event", "ShopMissedItem"]
                    }
                ],
                choiceFunc = "saltyisaac_archipelago_ShopPickMissedAPItem"
            }
        },
        {
            "saltyisaac_archipelago_BootSequenceUnlockedItem",
            new StoryNode
            {
                type = NodeType.@event,
                lookup = [ "saltyisaac_archipelago_bootSequenceUnlockedItem" ],
                bg = "BGBootSequence",
                lines =
                [
                    new CustomSay
                    {
                        who = AmCat,
                        loopTag = "loading3",
                        lineKey = ["story", "event", "BootSequenceUnlockedItem"]
                    }
                ],
                choiceFunc = "saltyisaac_archipelago_BootSequencePickUnlockedItem"
            }
        },
        {
            "saltyisaac_archipelago_SwapCharacter",
            new StoryNode
            {
                type = NodeType.@event,
                lookup = [ "saltyisaac_archipelago_swapCharacter" ],
                bg = "BGCrystalizedFriend",
                lines =
                [
                    new CustomSay
                    {
                        lineKey = ["story", "event", "MapSwapCharacter", "CAT1"],
                        flipped = true,
                        who = AmCat
                    },
                    new CustomSay
                    {
                        lineKey = ["story", "event", "MapSwapCharacter", "CAT2"],
                        flipped = true,
                        who = AmCat
                    },
                    new CustomSay
                    {
                        lineKey = ["story", "event", "MapSwapCharacter", "CAT3"],
                        flipped = true,
                        loopTag = "squint",
                        who = AmCat
                    },
                    new SaySwitch
                    {
                        lines = 
                        [
                            new CustomSay
                            {
                                lineKey = ["story", "event", "MapSwapCharacter", "IsaacPre"],
                                who = AmIsaac
                            },
                            new CustomSay
                            {
                                lineKey = ["story", "event", "MapSwapCharacter", "DizzyPre"],
                                who = AmDizzy
                            },
                            new CustomSay
                            {
                                lineKey = ["story", "event", "MapSwapCharacter", "PeriPre"],
                                loopTag = "nap",
                                who = AmPeri
                            },
                            new CustomSay
                            {
                                lineKey = ["story", "event", "MapSwapCharacter", "RiggsPre"],
                                who = AmRiggs
                            },
                            new CustomSay
                            {
                                lineKey = ["story", "event", "MapSwapCharacter", "BooksPre"],
                                loopTag = "paws",
                                who = AmBooks
                            },
                            new CustomSay
                            {
                                lineKey = ["story", "event", "MapSwapCharacter", "DrakePre"],
                                loopTag = "sly",
                                who = AmDrake
                            },
                            new CustomSay
                            {
                                lineKey = ["story", "event", "MapSwapCharacter", "MaxPre"],
                                who = AmMax
                            }
                        ]
                    },
                    new CustomSay
                    {
                        lineKey = ["story", "event", "MapSwapCharacter", "CAT4"],
                        flipped = true,
                        who = AmCat
                    }
                ],
                choiceFunc = "saltyisaac_archipelago_ChooseSwapCharacter1"
            }
        },
        {
            "saltyisaac_archipelago_SwapCharacter_ThinkAgain",
            new StoryNode
            {
                type = NodeType.@event,
                lookup = [ "saltyisaac_archipelago_swapCharacter_ThinkAgain" ],
                bg = "BGCrystalizedFriend",
                lines =
                [
                    new CustomSay
                    {
                        lineKey = ["story", "event", "MapSwapCharacter", "CAT4"],
                        flipped = true,
                        who = AmCat
                    }
                ],
                choiceFunc = "saltyisaac_archipelago_ChooseSwapCharacter1"
            }
        },
        {
            "saltyisaac_archipelago_SwapCharacter_NoOut",
            new StoryNode
            {
                type = NodeType.@event,
                lookup = [ "saltyisaac_archipelago_swapCharacter_NoOut" ],
                bg = "BGCrystalizedFriend",
                lines =
                [
                    new CustomSay
                    {
                        lineKey = ["story", "event", "MapSwapCharacter", "CAT1"],
                        flipped = true,
                        who = AmCat
                    },
                    new CustomSay
                    {
                        lineKey = ["story", "event", "MapSwapCharacter", "CAT2"],
                        flipped = true,
                        who = AmCat
                    }
                ],
                choiceFunc = "saltyisaac_archipelago_ChooseSwapCharacter1"
            }
        },
        {
            "saltyisaac_archipelago_SwapCharacter_Refuse",
            new StoryNode
            {
                type = NodeType.@event,
                lookup = [ "saltyisaac_archipelago_swapCharacter_Refuse" ],
                bg = "BGCrystalizedFriend",
                lines = [
                    new CustomSay
                    {
                        lineKey = ["story", "event", "MapSwapCharacter", "CATRefuse"],
                        who = AmCat
                    }
                ]
            }
        },
        {
            "saltyisaac_archipelago_SwapCharacter_Choice2",
            new StoryNode
            {
                type = NodeType.@event,
                lookup = [ "saltyisaac_archipelago_swapCharacter_Choice2" ],
                bg = "BGCrystalizedFriend",
                lines = [
                    new CustomSay
                    {
                        who = AmCat,
                        flipped = true,
                        lineKey = ["story", "event", "MapSwapCharacter", "choice2"]
                    }
                ],
                choiceFunc = "saltyisaac_archipelago_ChooseSwapCharacter2"
            }
        },
        {
            "saltyisaac_archipelago_SwapCharacter_colorless",
            new StoryNode
            {
                type = NodeType.@event,
                lookup = [ "saltyisaac_archipelago_swapCharacter_colorless" ],
                bg = "BGCrystalizedFriend",
                lines = [
                    new Wait { secs = 1.5 },
                    new CustomSay
                    {
                        lineKey = ["story", "event", "MapSwapCharacter", "CATPost"],
                        who = AmCat
                    }
                ]
            }
        },
        {
            "saltyisaac_archipelago_SwapCharacter_dizzy",
            new StoryNode
            {
                type = NodeType.@event,
                lookup = [ "saltyisaac_archipelago_swapCharacter_dizzy" ],
                bg = "BGCrystalizedFriend",
                lines = [
                    new Wait { secs = 1.5 },
                    new CustomSay
                    {
                        lineKey = ["story", "event", "MapSwapCharacter", "DizzyPost"],
                        who = AmDizzy
                    }
                ]
            }
        },
        {
            "saltyisaac_archipelago_SwapCharacter_eunice",
            new StoryNode
            {
                type = NodeType.@event,
                lookup = [ "saltyisaac_archipelago_swapCharacter_eunice" ],
                bg = "BGCrystalizedFriend",
                lines = [
                    new Wait { secs = 1.5 },
                    new CustomSay
                    {
                        lineKey = ["story", "event", "MapSwapCharacter", "DrakePost"],
                        loopTag = "panic",
                        who = AmDrake
                    }
                ]
            }
        },
        {
            "saltyisaac_archipelago_SwapCharacter_goat",
            new StoryNode
            {
                type = NodeType.@event,
                lookup = [ "saltyisaac_archipelago_swapCharacter_goat" ],
                bg = "BGCrystalizedFriend",
                lines = [
                    new Wait { secs = 1.5 },
                    new CustomSay
                    {
                        lineKey = ["story", "event", "MapSwapCharacter", "IsaacPost"],
                        who = AmIsaac
                    }
                ]
            }
        },
        {
            "saltyisaac_archipelago_SwapCharacter_hacker",
            new StoryNode
            {
                type = NodeType.@event,
                lookup = [ "saltyisaac_archipelago_swapCharacter_hacker" ],
                bg = "BGCrystalizedFriend",
                lines = [
                    new Wait { secs = 1.5 },
                    new CustomSay
                    {
                        lineKey = ["story", "event", "MapSwapCharacter", "MaxPost"],
                        who = AmMax
                    }
                ]
            }
        },
        {
            "saltyisaac_archipelago_SwapCharacter_peri",
            new StoryNode
            {
                type = NodeType.@event,
                lookup = [ "saltyisaac_archipelago_swapCharacter_peri" ],
                bg = "BGCrystalizedFriend",
                lines = [
                    new Wait { secs = 1.5 },
                    new CustomSay
                    {
                        lineKey = ["story", "event", "MapSwapCharacter", "PeriPost"],
                        who = AmPeri
                    }
                ]
            }
        },
        {
            "saltyisaac_archipelago_SwapCharacter_riggs",
            new StoryNode
            {
                type = NodeType.@event,
                lookup = [ "saltyisaac_archipelago_swapCharacter_riggs" ],
                bg = "BGCrystalizedFriend",
                lines = [
                    new Wait { secs = 1.5 },
                    new CustomSay
                    {
                        lineKey = ["story", "event", "MapSwapCharacter", "RiggsPost"],
                        who = AmRiggs
                    }
                ]
            }
        },
        {
            "saltyisaac_archipelago_SwapCharacter_shard",
            new StoryNode
            {
                type = NodeType.@event,
                lookup = [ "saltyisaac_archipelago_swapCharacter_shard" ],
                bg = "BGCrystalizedFriend",
                lines = [
                    new Wait { secs = 1.5 },
                    new CustomSay
                    {
                        lineKey = ["story", "event", "MapSwapCharacter", "BooksPost"],
                        who = AmBooks
                    }
                ]
            }
        }
    };

    internal static readonly Dictionary<string, MethodInfo> eventChoices = new()
    {
        {
            "saltyisaac_archipelago_ShopPickMissedAPItem",
            typeof(ShopPatch).GetMethod(nameof(ShopPatch.ShopPickMissedAPItem))!
        },
        {
            "saltyisaac_archipelago_BootSequencePickUnlockedItem",
            typeof(BootSequencePatch).GetMethod(nameof(BootSequencePatch.BootSequencePickUnlockedItem))!
        },
        {
            "saltyisaac_archipelago_ChooseSwapCharacter1",
            typeof(MapSwapCharacter).GetMethod(nameof(MapSwapCharacter.ChooseCharToSwapIn))!
        },
        {
            "saltyisaac_archipelago_ChooseSwapCharacter2",
            typeof(MapSwapCharacter).GetMethod(nameof(MapSwapCharacter.ChooseCharToSwapOut))!
        }
    };
    
    internal static readonly Dictionary<string, StoryNode> memoryNodes = new()
    {
        // Triple memory unlock
        {
            "RunWinWho_AllOfThem",
            new StoryNode
            {
                type = NodeType.@event,
                introDelay = false,
                allPresent = [],
                bg = "BGRunWin",
                lookup = [ "runWin_AllOfThem" ],
                lines =
                [
                    new CustomSay
                    {
                        who = AmVoid,
                        flipped = true,
                        lineKey = ["story", "memory", "AllThreeEndTalk", "Void1"]
                    }
                ]
            }
        },
        // Books final talks
        {
            "RunWinWho_Shard_1",
            new StoryNode
            {
                type = NodeType.@event,
                introDelay = false,
                allPresent = [ AmBooks ],
                bg = "BGRunWin",
                lookup = [ $"runWin_{AmBooks}" ],
                lines =
                [
                    new Wait
                    {
                        secs = 3
                    },
                    new CustomSay
                    {
                        who = AmBooks,
                        lineKey = ["story", "memory", "BooksEndTalk1", "Books1"]
                    },
                    new CustomSay
                    {
                        who = AmVoid,
                        flipped = true,
                        lineKey = ["story", "memory", "BooksEndTalk1", "Void1"]
                    }
                ]
            }
        },
        {
            "RunWinWho_Shard_2",
            new StoryNode
            {
                type = NodeType.@event,
                introDelay = false,
                allPresent = [ AmBooks ],
                bg = "BGRunWin",
                lookup = [ $"runWin_{AmBooks}" ],
                requiredScenes = [ "RunWinWho_Shard_1" ],
                lines =
                [
                    new Wait
                    {
                        secs = 3
                    },
                    new CustomSay
                    {
                        who = AmBooks,
                        lineKey = ["story", "memory", "BooksEndTalk2", "Books1"]
                    },
                    new CustomSay
                    {
                        who = AmVoid,
                        flipped = true,
                        lineKey = ["story", "memory", "BooksEndTalk2", "Void1"]
                    }
                ]
            }
        },
        {
            "RunWinWho_Shard_3",
            new StoryNode
            {
                type = NodeType.@event,
                introDelay = false,
                allPresent = [ AmBooks ],
                bg = "BGRunWin",
                lookup = [ $"runWin_{AmBooks}" ],
                requiredScenes = [ "RunWinWho_Shard_2" ],
                lines =
                [
                    new Wait
                    {
                        secs = 3
                    },
                    new CustomSay
                    {
                        who = AmBooks,
                        lineKey = ["story", "memory", "BooksEndTalk3", "Books1"]
                    },
                    new CustomSay
                    {
                        who = AmVoid,
                        flipped = true,
                        lineKey = ["story", "memory", "BooksEndTalk3", "Void1"]
                    }
                ]
            }
        },
        
        // CAT final talks
        {
            "RunWinWho_CAT_1",
            new StoryNode
            {
                type = NodeType.@event,
                introDelay = false,
                allPresent = [ AmCatDeck ],
                bg = "BGRunWin",
                lookup = [ $"runWin_{AmCatDeck}" ],
                lines =
                [
                    new Wait
                    {
                        secs = 3
                    },
                    new CustomSay
                    {
                        who = AmCat,
                        lineKey = ["story", "memory", "CATEndTalk1", "CAT1"]
                    },
                    new CustomSay
                    {
                        who = AmVoid,
                        flipped = true,
                        lineKey = ["story", "memory", "CATEndTalk1", "Void1"]
                    }
                ]
            }
        },
        {
            "RunWinWho_CAT_2",
            new StoryNode
            {
                type = NodeType.@event,
                introDelay = false,
                allPresent = [ AmCatDeck ],
                bg = "BGRunWin",
                lookup = [ $"runWin_{AmCatDeck}" ],
                requiredScenes = [ "RunWinWho_CAT_1" ],
                lines =
                [
                    new Wait
                    {
                        secs = 3
                    },
                    new CustomSay
                    {
                        who = AmCat,
                        lineKey = ["story", "memory", "CATEndTalk2", "CAT1"]
                    },
                    new CustomSay
                    {
                        who = AmVoid,
                        flipped = true,
                        lineKey = ["story", "memory", "CATEndTalk2", "Void1"]
                    }
                ]
            }
        },
        {
            "RunWinWho_CAT_3",
            new StoryNode
            {
                type = NodeType.@event,
                introDelay = false,
                allPresent = [ AmCatDeck ],
                bg = "BGRunWin",
                lookup = [ $"runWin_{AmCatDeck}" ],
                requiredScenes = [ "RunWinWho_CAT_2" ],
                lines =
                [
                    new Wait
                    {
                        secs = 3
                    },
                    new CustomSay
                    {
                        who = AmCat,
                        lineKey = ["story", "memory", "CATEndTalk3", "CAT1"]
                    },
                    new CustomSay
                    {
                        who = AmVoid,
                        flipped = true,
                        lineKey = ["story", "memory", "CATEndTalk3", "Void1"]
                    }
                ]
            }
        },
        
        // Books' memories
        {
            "Shard_Memory_1",
            new StoryNode
            {
                type = NodeType.@event,
                introDelay = false,
                bg = "BGRunWin",
                lookup = [ "vault", $"vault_{AmBooks}" ],
                lines =
                [
                    new TitleCard(),
                    new Wait
                    {
                        secs = 1
                    },
                    new CustomSay
                    {
                        who = AmBooks,
                        lineKey = ["story", "memory", "BooksMemory1", "Books1"]
                    }
                ]
            }
        },
        {
            "Shard_Memory_2",
            new StoryNode
            {
                type = NodeType.@event,
                introDelay = false,
                bg = "BGRunWin",
                lookup = [ "vault", $"vault_{AmBooks}" ],
                lines =
                [
                    new TitleCard(),
                    new Wait
                    {
                        secs = 1
                    },
                    new CustomSay
                    {
                        who = AmBooks,
                        lineKey = ["story", "memory", "BooksMemory2", "Books1"]
                    }
                ]
            }
        },
        {
            "Shard_Memory_3",
            new StoryNode
            {
                type = NodeType.@event,
                introDelay = false,
                bg = "BGRunWin",
                lookup = [ "vault", $"vault_{AmBooks}" ],
                lines =
                [
                    new TitleCard(),
                    new Wait
                    {
                        secs = 1
                    },
                    new CustomSay
                    {
                        who = AmBooks,
                        lineKey = ["story", "memory", "BooksMemory3", "Books1"]
                    }
                ]
            }
        },
        
        // CAT's memories
        {
            "CAT_Memory_1",
            new StoryNode
            {
                type = NodeType.@event,
                introDelay = false,
                bg = "BGRunWin",
                lookup = [ "vault", $"vault_{AmCatDeck}" ],
                lines =
                [
                    new TitleCard(),
                    new Wait
                    {
                        secs = 1
                    },
                    new CustomSay
                    {
                        who = AmCat,
                        lineKey = ["story", "memory", "CATMemory1", "CAT1"]
                    }
                ]
            }
        },
        {
            "CAT_Memory_2",
            new StoryNode
            {
                type = NodeType.@event,
                introDelay = false,
                bg = "BGRunWin",
                lookup = [ "vault", $"vault_{AmCatDeck}" ],
                lines =
                [
                    new TitleCard(),
                    new Wait
                    {
                        secs = 1
                    },
                    new CustomSay
                    {
                        who = AmCat,
                        lineKey = ["story", "memory", "CATMemory2", "CAT1"]
                    }
                ]
            }
        },
        {
            "CAT_Memory_3",
            new StoryNode
            {
                type = NodeType.@event,
                introDelay = false,
                bg = "BGRunWin",
                lookup = [ "vault", $"vault_{AmCatDeck}" ],
                lines =
                [
                    new TitleCard(),
                    new Wait
                    {
                        secs = 1
                    },
                    new CustomSay
                    {
                        who = AmCat,
                        lineKey = ["story", "memory", "CATMemory3", "CAT1"]
                    }
                ]
            }
        }
    };
}