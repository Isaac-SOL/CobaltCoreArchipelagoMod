using System.Collections.Generic;
using System.Linq;

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
    internal static string AmCat => "comp";
    internal static string AmCatDeck => Deck.colorless.Key();
    internal static string AmBooks => Deck.shard.Key();
    internal static string AmVoid => "void";

    internal static void Register(Dictionary<string, StoryNode> nodes)
    {
        foreach (var kvp in nodes)
        {
            DB.story.all[kvp.Key] = kvp.Value;
        }
    }
    
    internal static readonly Dictionary<string, StoryNode> memoryNodes = new()
    {
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