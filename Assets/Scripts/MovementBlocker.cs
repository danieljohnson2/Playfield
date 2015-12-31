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
    /// MoveEffect is an enum to summarize the result of a move.
    /// </summary>
    public enum MoveEffect
    {
        /// <summary>
        /// None means no move was made, or the move didn't count
        /// because it was blocked. The game state has not changed,
        /// and we don't go onto the next creature's turn.
        /// </summary>
        None,

        /// <summary>
        /// Moved means the move was made normally; the creature
        /// moves to a new place, and other effets (like item
        /// pickups) can happen on the way.
        /// </summary>
        Moved,

        /// <summary>
        /// Action means the move was not made normally, but osmething
        /// else happened like an attack or a teleportion. This is still
        /// a valid move, but you don't wind up in the space you moved
        /// into.
        /// </summary>
        Action
    }

    /// <summary>
    /// This method is called when some object tries to
    /// move into this square. It can return 'Moved' to allow
    /// the move, or 'None' or 'Action' to disallow it. If it
    /// takes any action in response to the move, it should return
    /// Action to indicate that something did happen.
    /// 
    /// Movement blockers are sometimes terrain, so this
    /// can be called on a prefab; for that reasons the destination
    /// must be supplied explicitly.
    /// </summary>
    public virtual MoveEffect Block(GameObject mover, Location destination)
    {
        return passable ? MoveEffect.Moved : MoveEffect.None;
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