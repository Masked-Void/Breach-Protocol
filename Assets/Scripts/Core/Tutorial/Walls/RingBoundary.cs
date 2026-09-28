using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.Events;

// one ring's boundary, every 5 m wall face around it, 4 on the cell and 12, 20, 28 on rings 1 to 3
// starts them all together and reports done when every face is gone
public class RingBoundary : MonoBehaviour
{
    [Header("Faces")]
    [Tooltip("every dissolving wall face on this ring's boundary, straight and diagonal both go here")]
    [SerializeField] private DissolveWall[] faces;

    [Header("Events")]
    [Tooltip("fires once every face has dissolved")]
    public UnityEvent BoundaryDissolved = new UnityEvent();

    // faces still standing
    private int facesRemaining;

    // stops a second start
    private bool hasStarted = false;

    // true once every face is gone
    public bool IsDissolved => hasStarted && facesRemaining == 0;

    // average progress of all faces from 0 to 1, for sound or ui
    public float Progress
    {
        get
        {
            if (faces == null)
            {
                return 1f;
            }

            float total = 0f;
            int count = 0;
            foreach(DissolveWall face in faces)
            {
                if (face != null)
                {
                    total += face.Progress;
                    count++;
                }
            }

            return count > 0 ? total / count : 1f;
        }
    }

    private void OnEnable()
    {
        if (faces == null)
        {
            return;
        }

        foreach (DissolveWall face in faces)
        {
            if (face != null)
            {
                face.Dissolved.AddListener(handleFaceDissolved);
            }
        }
    }

    private void OnDisable()
    {
        if (faces == null)
        {
            return;
        }

        foreach (DissolveWall face in faces)
        {
            if (face != null)
            {
                face.Dissolved.RemoveListener(handleFaceDissolved);
            }
        }
    }

    // the manager calls this when the ring's lesson is done
    public void StartDissolve()
    {
        if (hasStarted || faces == null)
        {
            return;
        }

        hasStarted = true;

        facesRemaining = 0;

        foreach(DissolveWall face in faces)
        {
            if (face!=null && !face.IsDissolved)
            {
                facesRemaining++;
            }
        }

        if (facesRemaining == 0)
        {
            BoundaryDissolved.Invoke();
            return;
        }

        foreach(DissolveWall face in faces)
        {
            if (face != null)
            {
                face.StartDissolve();
            }
        }
    }

    // debug skip, opens every face this frame
    public void FinishNow()
    {
        if (faces == null)
        {
            return;
        }

        foreach (DissolveWall face in faces)
        {
            if (face != null)
            {
                face.FinishNow();
            }
        }
    }

    // puts every face back, for testing
    public void ResetBoundary()
    {
        hasStarted = false;
        facesRemaining = 0;

        if (faces == null)
        {
            return;
        }

        foreach(DissolveWall face in faces)
        {
            if (face != null)
            {
                face.ResetWall();
            }
        }
    }

    // a face finished
    private void handleFaceDissolved()
    {
        if (!hasStarted || facesRemaining == 0)
        {
            return;
        }

        facesRemaining--;
        if (facesRemaining == 0)
        {
            BoundaryDissolved.Invoke();
        }
    }
}
