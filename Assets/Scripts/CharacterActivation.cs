using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

/// <summary>
/// CharacterActivation is a static class that keeps track of which characters are active.
/// Most characters are inactive until you kill them in game; they then become available
/// to play as.
/// 
/// This is static because there is just one list of active characters across all games;
/// this date is simply kept in a file.
/// 
/// We do keep track of 'recent' activations and deactivations for the UI though; these
/// are not persisted to disk.
/// </summary>
internal static class CharacterActivation
{
    private const string defaultCharacterName = "Hero";

    /// <summary>
    /// isDefault is true if no changes have been made to the
    /// activation settings.
    /// </summary>
    public static bool isDefault
    {
        get { return !File.Exists(GetActiveCharactersPath()); }
    }

    /// <summary>
    /// Reset() discards all changes, deletes the storage
    /// files, and puts this activation to its default state.
    /// </summary>
    public static void Reset()
    {
        File.Delete(GetActiveCharactersPath());
    }

    /// <summary>
    /// GetActivatedCharacters() reads the list of active
    /// characters and returns them in a set.
    /// </summary>
    public static HashSet<string> GetActivatedCharacters()
    {
        HashSet<string> names = ReadStorage();

        // just in case somebody left blank lines in there!
        names.RemoveWhere(n => n == "");
        
        if (names.Count == 0)
            names.Add(defaultCharacterName);

        return names;
    }

    /// <summary>
    /// Activate() actives a character; the change is written
    /// to a disk file immediately.
    /// </summary>
    public static void Activate(string characterName)
    {
        HashSet<string> names = GetActivatedCharacters();

        if (names.Add(characterName))
        {
            lock (recentChanges)
            {
                recentChanges[characterName] = true;
            }
        }

        WriteStorage(names);
    }

    /// <summary>
    /// Deactivate() deactives a character; the change is written
    /// to a disk file immediately.
    /// </summary>
    public static void Deactivate(string characterName)
    {
        HashSet<string> names = GetActivatedCharacters();

        if (names.Remove(characterName))
        {
            lock (recentChanges)
            {
                recentChanges[characterName] = true;
            }
        }

        WriteStorage(names);
    }

    #region Lock and Unlock Tracking

    private static readonly Dictionary<string, bool> recentChanges = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);

    /// <summary>
    /// ResetRecentChanges() resets the storage of recent changes, so that
    /// RecentlyLocked() and RecentlyUnlocked() return empty collections.
    /// </summary>
    public static void ResetRecentChanges()
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
    public static IEnumerable<string> RecentlyLocked()
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
    public static IEnumerable<string> RecentlyUnlocked()
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
    /// ReadStorage() reads the lines of the text file at the
    /// path given, and returns them in a set. The set is case-insensitive.
    /// If the file at the path is missing, this method returns an empty
    /// set.
    /// </summary>
    private static HashSet<string> ReadStorage()
    {
        string path = GetActiveCharactersPath();

        if (File.Exists(path))
        {
            return new HashSet<string>(
                File.ReadAllLines(path),
                StringComparer.InvariantCultureIgnoreCase);
        }
        else
            return new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// WriteStorage() writes the names in 'activeCharacters' to the
    /// path we loaded from, creating this file if needed.
    /// 
    /// This method sorts the names just to make it look neat.
    /// </summary>
    private static void WriteStorage(IEnumerable<string> activeCharacters)
    {
        string[] sorted = activeCharacters.ToArray();
        System.Array.Sort(sorted);

        File.WriteAllLines(GetActiveCharactersPath(), sorted);
    }

    /// <summary>
    /// GetActiveCharactersPath() generates the path where we will save the list
    /// of active characters.
    /// </summary>
    public static string GetActiveCharactersPath()
    {
        return Path.Combine(UnityEngine.Application.persistentDataPath, @"ActiveCharacters.txt");
    }

    #endregion
}