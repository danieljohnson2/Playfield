using UnityEngine;
using System.Collections;

/// <summary>
/// This is a component that blocks other object's movement,
/// so they either can't move into the same square as this one,
/// or something happens if they do.
/// 
/// This is the most basic base-class for interactions in
/// the game world; when a creature steps into a blocker,
/// the blocker can take action.
/// </summary>
public class MovementBlocker : SavingController
{
    /// <summary>
    /// This property returns the map controller; this
    /// is a singleton that is cached on first
    /// access.
    /// </summary>
    protected static MapController mapController
    {
        get { return MapController.instance; }
    }

    /// <summary>
    /// This property proides conevnient access to
    /// the transcript object, so we can print messages.
    /// </summary>
    protected TranscriptController transcript
    {
        get { return mapController.transcript; }
    }

    /// <summary>
    /// If true, creatures can try to move into this square
    /// (though Block() might still reject them).
    /// </summary>
    public bool passable { get; set; }

    /// <summary>
    /// This may be set to cause the AddTransaction() methods
    /// to go silent if this object is not on the active map.
    /// </summary>
    public bool transcribesLocallyOnly;

    public virtual bool? pathable
    {
        get { return passable; }
    }

    /// <summary>
    /// If this returns true, the AI pathfinding paths through this
    /// square; if false the square blocks the AI's awareness
    /// of the square, though they can still move randonly into
    /// it anyway.
    /// </summary>
    public virtual bool IsPathableFor(GameObject mover)
    {
        return passable;
    }

    /// <summary>
    /// This method is called when some object tries to
    /// move into this square. It can return true to allow
    /// the move or false to fail it, and can take other
    /// actions triggered by the movement as well.
    /// 
    /// Movement blockers are sometimes terrain, so this
    /// can be called on a prefab; for that reasons the destination
    /// must be supplied explicitly.
    /// </summary>
    public virtual bool Block(GameObject mover, Location destination)
    {
        return passable;
    }

    /// <summary>
    /// This method adds a line to the transcript. If 'transcribesLocally'
    /// is set, this will do so only if this object is on the active map.
    /// </summary>
    public void AddTranscriptLine(string text)
    {
        if (transcribesLocallyOnly)
            AddLocalTranscriptLine(text);
        else
            transcript.AddLine(text);
    }

    /// <summary>
    /// This method adds a line to the transcript, but only
    /// this object is on the active map.
    /// </summary>
    public void AddTranscriptLine(string format, params object[] parameters)
    {
        AddTranscriptLine(string.Format(format, parameters));
    }

    /// <summary>
    /// This method adds a line to the transcript. However, this will
    /// do it only if this object is on the active map (regardless of what
    /// transcribesLocallyOnly is set to).
    /// </summary>
    public void AddLocalTranscriptLine(string text)
    {
        Map activeMap = mapController.activeMap;
        if (activeMap != null && activeMap.mapIndex == Location.Of(gameObject).mapIndex)
            transcript.AddLine(text);
    }

    /// <summary>
    /// This method adds a line to the transcript. However, this will
    /// do it only if this object is on the active map (regardless of what
    /// transcribesLocallyOnly is set to).
    /// </summary>
    public void AddLocalTranscriptLine(string format, params object[] parameters)
    {
        AddLocalTranscriptLine(string.Format(format, parameters));
    }
}