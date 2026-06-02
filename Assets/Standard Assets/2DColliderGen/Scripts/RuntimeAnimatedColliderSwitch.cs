#if !(UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2)
#define UNITY_4_3_AND_LATER
#endif

#if !(UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)
#define UNITY_4_6_AND_LATER
#endif

#if !(UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5)
#define UNITY_4_AND_LATER
#endif

#if !(UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7)
#define UNITY_5_AND_LATER
#endif

#if UNITY_4_6_AND_LATER
#define UNITY_SUPPORTS_EDGECOLLIDER2D
#endif

#if UNITY_4_3_AND_LATER

using UnityEngine;
using System.Collections;

public class RuntimeAnimatedColliderSwitch : MonoBehaviour {

	protected enum ColliderMode {
		NONE,
		POLYGON_COLLIDER_2D,
#if UNITY_SUPPORTS_EDGECOLLIDER2D
        EDGE_COLLIDER_2D,
#endif
        MESH_COLLIDER
    }

	protected SpriteRenderer mSpriteRenderer;
	[SerializeField] protected PolygonCollider2D[] mPolygonCollidersToSwitch;
#if UNITY_SUPPORTS_EDGECOLLIDER2D
    [SerializeField] protected EdgeCollider2D[] mEdgeCollidersToSwitch;
#endif
    [SerializeField] protected MeshCollider[] mMeshCollidersToSwitch;
	[SerializeField] protected string[] mColliderIDStrings;
    [SerializeField] protected int[] mRemovedFrameReferenceIndex;
    [SerializeField] protected bool mSyncToParentSpriteRenderer = false;
    public int mActiveColliderIndex = 0;
	public PolygonCollider2D mActivePolygonCollider = null;
#if UNITY_SUPPORTS_EDGECOLLIDER2D
    public EdgeCollider2D mActiveEdgeCollider = null;
#endif
    public MeshCollider mActiveMeshCollider = null;
	protected ColliderMode mColliderMode = ColliderMode.NONE;

	// Setters and Getters
	public PolygonCollider2D[] PolygonCollidersToSwitchNoReferencing {
		get {
			return mPolygonCollidersToSwitch;
		}
		set {
			mPolygonCollidersToSwitch = value;
			if (value != null && value.Length > 0) {
				mColliderMode = ColliderMode.POLYGON_COLLIDER_2D;
			}
		}
	}
#if UNITY_SUPPORTS_EDGECOLLIDER2D
    public EdgeCollider2D[] EdgeCollidersToSwitchNoReferencing {
        get {
            return mEdgeCollidersToSwitch;
        }
        set {
            mEdgeCollidersToSwitch = value;
            if (value != null && value.Length > 0) {
                mColliderMode = ColliderMode.EDGE_COLLIDER_2D;
            }
        }
    }
#endif
    public MeshCollider[] MeshCollidersToSwitchNoReferencing {
		get {
			return mMeshCollidersToSwitch;
		}
		set {
			mMeshCollidersToSwitch = value;
			if (value != null && value.Length > 0) {
				mColliderMode = ColliderMode.MESH_COLLIDER;
			}
		}
	}
    public bool SyncToParentSpriteRenderer {
        get {
            return mSyncToParentSpriteRenderer;
        }
        set {
            mSyncToParentSpriteRenderer = value;
        }
    }

    public string[] ColliderIDStrings {
		get {
			return mColliderIDStrings;
		}
		set {
			mColliderIDStrings = value;
		}
	}

    public int[] RemovedFrameReferenceIndex {
        set {
            mRemovedFrameReferenceIndex = value;
        }
    }

    //-------------------------------------------------------------------------
    public void UpdateAnimationSyncSpriteRenderer() {
        if (mSyncToParentSpriteRenderer) {
            mSpriteRenderer = this.transform.parent.GetComponentInParent<SpriteRenderer>();
        }
    }

    //-------------------------------------------------------------------------
    public void SyncAnimationTo(SpriteRenderer spriteRenderer) {
        mSpriteRenderer = spriteRenderer;
    }

    //-------------------------------------------------------------------------
    void Awake() {
        if (mSyncToParentSpriteRenderer) {
            mSpriteRenderer = this.transform.parent.GetComponentInParent<SpriteRenderer>();
        }
        else {
            mSpriteRenderer = this.GetComponent<SpriteRenderer>();
        }

        SetupRemovedFrameReferences();
        EnableActiveFrameCollider();
        
		if (mColliderMode == ColliderMode.NONE) {
			if (mPolygonCollidersToSwitch != null && mPolygonCollidersToSwitch.Length > 0)
				mColliderMode = ColliderMode.POLYGON_COLLIDER_2D;
#if UNITY_SUPPORTS_EDGECOLLIDER2D
            else if (mEdgeCollidersToSwitch != null && mEdgeCollidersToSwitch.Length > 0)
                mColliderMode = ColliderMode.EDGE_COLLIDER_2D;
#endif
            else if (mMeshCollidersToSwitch != null && mMeshCollidersToSwitch.Length > 0)
				mColliderMode = ColliderMode.MESH_COLLIDER;
		}
	}
	
	//-------------------------------------------------------------------------
	void LateUpdate () {
		if (mSpriteRenderer == null || mSpriteRenderer.sprite == null || (mPolygonCollidersToSwitch.Length == 0 && mMeshCollidersToSwitch.Length == 0)) {
			return;
		}

		if ((mActivePolygonCollider == null && mActiveMeshCollider == null) ||
		    !mSpriteRenderer.sprite.name.Equals(mColliderIDStrings[mActiveColliderIndex])) {

			if (mColliderMode == ColliderMode.POLYGON_COLLIDER_2D)
				SwitchPolygonCollider();
			else if (mColliderMode == ColliderMode.MESH_COLLIDER)
				SwitchMeshCollider();
		}
	}

	//-------------------------------------------------------------------------
	bool SwitchPolygonCollider() {

		string spriteName = mSpriteRenderer.sprite.name;

		bool wasSuitableColliderFound = true;
		int startIndex = mActiveColliderIndex;
		while (!spriteName.Equals(mColliderIDStrings[mActiveColliderIndex])) {
			mActiveColliderIndex = (mActiveColliderIndex+1) % mPolygonCollidersToSwitch.Length;
			if (mActiveColliderIndex == startIndex) {
				wasSuitableColliderFound = false;
				break;
			}
		}
		if (wasSuitableColliderFound) {
			// disable last active, activate new one
			if (mActivePolygonCollider != null) {
				mActivePolygonCollider.enabled = false;
			}

			mActivePolygonCollider = mPolygonCollidersToSwitch[mActiveColliderIndex];
			mActivePolygonCollider.enabled = true;
		}

		return wasSuitableColliderFound;
	}

	//-------------------------------------------------------------------------
    bool SetupRemovedFrameReferences() {

        if (mPolygonCollidersToSwitch != null) {
            for (int index = 0; index < mPolygonCollidersToSwitch.Length; ++index) {
                if (mPolygonCollidersToSwitch[index] == null) {
                    mPolygonCollidersToSwitch[index] = PolygonCollidersToSwitch(index);
                }
            }
        }
#if UNITY_SUPPORTS_EDGECOLLIDER2D
        if (mEdgeCollidersToSwitch != null) {
            // not supported for now.
        }
#endif
        if (mMeshCollidersToSwitch != null) {
            for (int index = 0; index < mMeshCollidersToSwitch.Length; ++index) {
                if (mMeshCollidersToSwitch[index] == null) {
                    mMeshCollidersToSwitch[index] = MeshCollidersToSwitch(index);
                }
            }
        }

        return true;
    }

    //-------------------------------------------------------------------------
    PolygonCollider2D PolygonCollidersToSwitch(int frameIndex) {
        PolygonCollider2D collider = mPolygonCollidersToSwitch[frameIndex];
        if (collider == null) {
            int replacementIndex = mRemovedFrameReferenceIndex[frameIndex];
            return PolygonCollidersToSwitch(replacementIndex);
        }
        else {
            return collider;
        }
    }

    //-------------------------------------------------------------------------
    MeshCollider MeshCollidersToSwitch(int frameIndex) {
        MeshCollider collider = mMeshCollidersToSwitch[frameIndex];
        if (collider == null) {
            int replacementIndex = mRemovedFrameReferenceIndex[frameIndex];
            return MeshCollidersToSwitch(replacementIndex);
        }
        else {
            return collider;
        }
    }

    //-------------------------------------------------------------------------
    bool EnableActiveFrameCollider() {

        if (mPolygonCollidersToSwitch != null && mPolygonCollidersToSwitch.Length > 0) {
            for (int index = 0; index < mPolygonCollidersToSwitch.Length; ++index) {
                if (index != mActiveColliderIndex) {
                    mPolygonCollidersToSwitch[index].enabled = false;
                }
            }
            mPolygonCollidersToSwitch[mActiveColliderIndex].enabled = true;
        }
#if UNITY_SUPPORTS_EDGECOLLIDER2D
        if (mEdgeCollidersToSwitch != null && mEdgeCollidersToSwitch.Length > 0) {
            for (int index = 0; index < mEdgeCollidersToSwitch.Length; ++index) {
                if (index != mActiveColliderIndex) {
                    mEdgeCollidersToSwitch[index].enabled = false;
                }
            }
            mEdgeCollidersToSwitch[mActiveColliderIndex].enabled = true;
        }
#endif
        if (mMeshCollidersToSwitch != null && mMeshCollidersToSwitch.Length > 0) {
            for (int index = 0; index < mMeshCollidersToSwitch.Length; ++index) {
                if (index != mActiveColliderIndex) {
                    mMeshCollidersToSwitch[index].enabled = false;
                }
            }
            mMeshCollidersToSwitch[mActiveColliderIndex].enabled = true;
        }
        return true;
    }

    //-------------------------------------------------------------------------
    bool SwitchMeshCollider() {
		
		string spriteName = mSpriteRenderer.sprite.name;
		
		bool wasSuitableColliderFound = true;
		int startIndex = mActiveColliderIndex;
		while (!spriteName.Equals(mColliderIDStrings[mActiveColliderIndex])) {
			mActiveColliderIndex = (mActiveColliderIndex+1) % mMeshCollidersToSwitch.Length;
			if (mActiveColliderIndex == startIndex) {
				wasSuitableColliderFound = false;
				break;
			}
		}
		if (wasSuitableColliderFound) {
			// disable last active, activate new one
			if (mActiveMeshCollider != null) {
				mActiveMeshCollider.enabled = false;
			}
			
			mActiveMeshCollider = mMeshCollidersToSwitch[mActiveColliderIndex];
			mActiveMeshCollider.enabled = true;
		}
		
		return wasSuitableColliderFound;
	}
}

#endif // UNITY_4_3_AND_LATER
