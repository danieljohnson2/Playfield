using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

/// <summary>
/// CharacterActivation is a singleton that keeps track of which characters are active.
/// Most characters are inactive until you kill them in game; they then become available
/// to play as.
/// 
/// This is a singleton because it represents a single file; the list of characters is shared
/// between all games.
/// 
/// The singleton keeps a list of characters locked and unlocked; this can be reset, and is used
/// to populate the game-over screen.
/// </summary>
internal sealed class CharacterActivation
{
    public static readonly CharacterActivation instance = new CharacterActivation(GetActiveCharactersPath());

    private readonly string path;
    private readonly HashSet<string> activeCharacters;
    private const string initialCharacterName = "Hero";

    private CharacterActivation(string path)
    {
        this.path = path;
        this.activeCharacters = ReadActiveCharacters(path);
        this.activeCharacters.Add(initialCharacterName);
    }

    /// <summary>
    /// This indexer lets you check to see if a character is active, or
    /// to change that set.
    /// 
    /// If you use this indexer to activate or deactivate a character, it
    /// will immediately write the storage file out.
    /// 
    /// (To access the getter, use
    ///    CharacterActivation.instance["Bob"]
    /// to access the setter, use
    ///   CharacterActivation.instance["Bob"] = true
    /// C# is full of fun stuff like this!).
    /// 
    /// This indexer is synchronized; multiple threads can use this at a the
    /// same time. However, we don't actually do this. I'm just being paranoid.
    /// </summary>
    public bool this[string characterName]
    {
        get
        {
            lock (activeCharacters)
            {
                return activeCharacters.Contains(characterName);
            }
        }

        set
        {
            bool changed;

            lock (activeCharacters)
            {

                if (value)
                    changed = activeCharacters.Add(characterName);
                else
                    changed = activeCharacters.Remove(characterName);

                if (changed)
                    WriteActiveCharacters();
            }

            if (changed)
            {
                lock (recentChanges)
                {
                    recentChanges[characterName] = value;
                }
            }
        }
    }

    #region Lock and Unlock Tracking

    private readonly Dictionary<string, bool> recentChanges = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);

    /// <summary>
    /// ResetRecentChanges() resets the storage of recent changes, so that
    /// RecentlyLocked() and RecentlyUnlocked() return empty collections.
    /// </summary>
    public void ResetRecentChanges()
    {
        lock (recentChanges)
        {
            recentChanges.Clear();
        }
    }

    /// <summary>
    /// RecentlyLocked() returns the alphabetized list of characters locked
    /// since we started the game, or last called ResetRecentChanges().
    /// </summary>
    public IEnumerable<string> RecentlyLocked()
    {
        lock (recentChanges)
        {
            return (from pair in recentChanges
                    where !pair.Value
                    orderby pair.Key
                    select pair.Key).ToArray();
        }
    }

    /// <summary>
    /// RecentlyUnlocked() returns the alphabetized list of characters unlocked
    /// since we started the game, or last called ResetRecentChanges().
    /// </summary>
    public IEnumerable<string> RecentlyUnlocked()
    {
        lock (recentChanges)
        {
            return (from pair in recentChanges
                    where pair.Value
                    orderby pair.Key
                    select pair.Key).ToArray();
        }
    }

    #endregion

    #region File Access

    /// <summary>
    /// ReadActiveCharacters() reads the lines of the text file at the
    /// path given, and returns them in a set. The set is case-insensitive.
    /// If the file at the path is missing, this method returns an empty
    /// set.
    /// </summary>
    private static HashSet<string> ReadActiveCharacters(string path)
    {
        if (File.Exists(path))
        {
            return new HashSet<string>(
                File.ReadAllLines(path),
                StringComparer.InvariantCultureIgnoreCase);
        }
        else
            return new HashSet<string>();
    }

    /// <summary>
    /// WriteActiveCharacters() writes the names in 'activeCharacters' to the
    /// path we loaded from, creating this file if needed.
    /// 
    /// This method sorts the names just to make it look neat.
    /// </summary>
    private void WriteActiveCharacters()
    {
        lock (activeCharacters)
        {
            string[] sorted = activeCharacters.ToArray();
            System.Array.Sort(sorted);

            File.WriteAllLines(path, sorted);
        }
    }

    /// <summary>
    /// GetActiveCharactersPath() generates the path where we will save the list
    /// of active characters.
    /// </summary>
    private static string GetActiveCharactersPath()
    {
        return Path.Combine(UnityEngine.Application.persistentDataPath, @"ActiveCharacters.txt");
    }

    #endregion
}