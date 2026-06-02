using UnityEngine;
using System.Collections.Generic;

// This file provides a set of common parameter- and data classes.

//-------------------------------------------------------------------------
/// <summary>
/// Class to group general (shared for all regions) parameters.
/// </summary>
[System.Serializable]
public class RegionIndependentParametersBase {
	
	[SerializeField] protected bool mLiveUpdate = true;
	[SerializeField] protected float mAlphaOpaqueThreshold = 0.1f;
	[SerializeField] protected float mVertexReductionDistanceTolerance = 0.0f;
	[SerializeField] protected int mDefaultMaxPointCount = 20;
	[SerializeField] protected float mThickness = 1.0f;
	[SerializeField] public bool mFlipHorizontal = false; // needed since it is used as an output parameter - TODO: pass the whole class as object.
	[SerializeField] public bool mFlipVertical = false; // needed since it is used as an output parameter
	[SerializeField] protected bool mConvex = false;
    [SerializeField] protected bool mIsTrigger = false;
    [SerializeField] protected bool mFlipInsideOutside = false;

    /// <summary>Erodes top pixels in a single direction to transperent before processing them. Value is [0..1] normalized to image region, not in pixels.</summary>
    [SerializeField] protected float mLowerTop = 0.0f;
    [SerializeField] protected float mRaiseBottom = 0.0f;
    [SerializeField] protected float mCutLeft = 0.0f;
    [SerializeField] protected float mCutRight = 0.0f;
    /// <summary>Dilates top pixels in a single direction to solid before processing them. Value is [0..1] normalized to image region, not in pixels.</summary>
    [SerializeField] protected float mExpandTop = 0.0f;
    [SerializeField] protected float mExpandBottom = 0.0f;
    [SerializeField] protected float mExpandLeft = 0.0f;
    [SerializeField] protected float mExpandRight = 0.0f;

    [SerializeField] protected float   mCustomRotation = 0.0f;
	[SerializeField] protected Vector2 mCustomScale = Vector2.one;
	[SerializeField] protected Vector3 mCustomOffset = Vector3.zero;
	
	[SerializeField] protected Texture2D mCustomTex;
    [SerializeField] protected bool mSyncToParentSpriteRenderer = false;

    [SerializeField] protected bool mIsCustomAtlasRegionUsed = false;
	[SerializeField] protected string mCustomAtlasFrameTitle = null;
	/// mRegionIndependentParameters.CustomAtlasFramePositionInPixels describes the offset of the top-left corner of the sub-texture from the top-left origin of the currently used texture
	[SerializeField] protected Vector2 mCustomAtlasFramePositionInPixels = Vector2.zero;
	[SerializeField] protected Vector2 mCustomAtlasFrameSizeInPixels = Vector2.zero;
	[SerializeField] protected float mCustomAtlasFrameRotation = 0.0f;
			
	[SerializeField] protected bool mUpdateCalculationNeeded = true;
    [SerializeField] protected bool mSimpleColliderParametersChanged = false;

    [SerializeField] protected float mUpdatedThickness = 1.0f; // workaround for a "changed" flag, since an ObjectField in the inspector GUI cannot target setters and getters, but only variables directly.
	
	// Constructors
	
	//-------------------------------------------------------------------------
	// Default constructor.
	public RegionIndependentParametersBase() {
	}
		
	//-------------------------------------------------------------------------
	// Deep-copy constructor.
	public RegionIndependentParametersBase(RegionIndependentParametersBase src) {
		
		mLiveUpdate = src.mLiveUpdate;
		mAlphaOpaqueThreshold = src.mAlphaOpaqueThreshold;
		mVertexReductionDistanceTolerance = src.mVertexReductionDistanceTolerance;
		mDefaultMaxPointCount = src.mDefaultMaxPointCount;
		mThickness = src.mThickness;
		mFlipHorizontal = src.mFlipHorizontal;
		mFlipVertical = src.mFlipVertical;
		mConvex = src.mConvex;
        mIsTrigger = src.mIsTrigger;
        mFlipInsideOutside = src.mFlipInsideOutside;

        mLowerTop = src.mLowerTop;
        mRaiseBottom = src.mRaiseBottom;
        mCutLeft = src.mCutLeft;
        mCutRight = src.mCutRight;
        mExpandTop = src.mExpandTop;
        mExpandBottom = src.mExpandBottom;
        mExpandLeft = src.mExpandLeft;
        mExpandRight = src.mExpandRight;
        
        mCustomRotation = src.mCustomRotation;
		mCustomScale = src.mCustomScale;
		mCustomOffset = src.mCustomOffset;
				
		mCustomTex = src.mCustomTex;
        mSyncToParentSpriteRenderer = src.mSyncToParentSpriteRenderer;

        mIsCustomAtlasRegionUsed = src.mIsCustomAtlasRegionUsed;
		mCustomAtlasFrameTitle = src.mCustomAtlasFrameTitle;
		
		mCustomAtlasFramePositionInPixels = src.mCustomAtlasFramePositionInPixels;
		mCustomAtlasFrameSizeInPixels = src.mCustomAtlasFrameSizeInPixels;
		mCustomAtlasFrameRotation = src.mCustomAtlasFrameRotation;
						
		mUpdateCalculationNeeded = src.mUpdateCalculationNeeded;
        mSimpleColliderParametersChanged = src.mSimpleColliderParametersChanged;
        mUpdatedThickness = src.mUpdatedThickness;
	}
	
	// Setters and Getters
	public bool LiveUpdate {
		get {
			return mLiveUpdate;
		}
		set {
			if (value != mLiveUpdate) {
				mUpdateCalculationNeeded = true;
			}
			mLiveUpdate = value;
		}
	}
	public float AlphaOpaqueThreshold {
		get {
			return mAlphaOpaqueThreshold;
		}
		set {
			if (value != mAlphaOpaqueThreshold) {
				mUpdateCalculationNeeded = true;
			}
			mAlphaOpaqueThreshold = value;
		}
	}
	public float VertexReductionDistanceTolerance {
		get {
			return mVertexReductionDistanceTolerance;
		}
		set {
			if (value != mVertexReductionDistanceTolerance) {
				mUpdateCalculationNeeded = true;
			}
			mVertexReductionDistanceTolerance = value;
		}
	}
	public int DefaultMaxPointCount {
		get {
			return mDefaultMaxPointCount;
		}
		set {
			if (value != mDefaultMaxPointCount) {
				mUpdateCalculationNeeded = true;
			}
			mDefaultMaxPointCount = value;
		}
	}
	public float Thickness {
		get {
			return mThickness;
		}
		set {
			if (value != mThickness) {
				mUpdateCalculationNeeded = true;
			}
			mThickness = value;
		}
	}
	public bool FlipHorizontal {
		get {
			return mFlipHorizontal;
		}
		set {
			if (value != mFlipHorizontal) {
				mUpdateCalculationNeeded = true;
			}
			mFlipHorizontal = value;
		}
	}
	public bool FlipVertical {
		get {
			return mFlipVertical;
		}
		set {
			if (value != mFlipVertical) {
				mUpdateCalculationNeeded = true;
			}
			mFlipVertical = value;
		}
	}
	public bool Convex {
		get {
			return mConvex;
		}
		set {
			if (value != mConvex) {
				mUpdateCalculationNeeded = true;
                mSimpleColliderParametersChanged = true;
            }
			mConvex = value;
		}
	}
    public bool IsTrigger {
        get {
            return mIsTrigger;
        }
        set {
            if (value != IsTrigger) {
                mSimpleColliderParametersChanged = true;
            }
            mIsTrigger = value;
        }
    }
    public bool FlipInsideOutside {
		get {
			return mFlipInsideOutside;
		}
		set {
			if (value != mFlipInsideOutside) {
				mUpdateCalculationNeeded = true;
			}
			mFlipInsideOutside = value;
		}
	}

    public float LowerTop {
        get {
            return mLowerTop;
        }
        set {
            mLowerTop = value;
        }
    }
    public float RaiseBottom {
        get {
            return mRaiseBottom;
        }
        set {
            mRaiseBottom = value;
        }
    }
    public float CutLeft {
        get {
            return mCutLeft;
        }
        set {
            mCutLeft = value;
        }
    }
    public float CutRight {
        get {
            return mCutRight;
        }
        set {
            mCutRight = value;
        }
    }

    public float ExpandTop {
        get {
            return mExpandTop;
        }
        set {
            mExpandTop = value;
        }
    }
    public float ExpandBottom {
        get {
            return mExpandBottom;
        }
        set {
            mExpandBottom = value;
        }
    }
    public float ExpandLeft {
        get {
            return mExpandLeft;
        }
        set {
            mExpandLeft = value;
        }
    }
    public float ExpandRight {
        get {
            return mExpandRight;
        }
        set {
            mExpandRight = value;
        }
    }

    public float CustomRotation {
		get {
			return mCustomRotation;
		}
		set {
			if (value != mCustomRotation) {
				mUpdateCalculationNeeded = true;
			}
			mCustomRotation = value;
		}
	}
	public Vector2 CustomScale {
		get {
			return mCustomScale;
		}
		set {
			if (value != mCustomScale) {
				mUpdateCalculationNeeded = true;
			}
			mCustomScale = value;
		}
	}
	public Vector3 CustomOffset {
		get {
			return mCustomOffset;
		}
		set {
			if (value != mCustomOffset) {
				mUpdateCalculationNeeded = true;
			}
			mCustomOffset = value;
		}
	}
	public Texture2D CustomTex {
		get {
			return mCustomTex;
		}
		set {
			if (value != mCustomTex) {
				mUpdateCalculationNeeded = true;
			}
			mCustomTex = value;
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

    public bool IsCustomAtlasRegionUsed {
		get {
			return mIsCustomAtlasRegionUsed;
		}
		set {
			if (value != mIsCustomAtlasRegionUsed) {
				mUpdateCalculationNeeded = true;
			}
			mIsCustomAtlasRegionUsed = value;
		}
	}
	public string CustomAtlasFrameTitle {
		get {
			return mCustomAtlasFrameTitle;
		}
		set {
			if (value != mCustomAtlasFrameTitle) {
				mUpdateCalculationNeeded = true;
			}
			mCustomAtlasFrameTitle = value;
		}
	}
	public Vector2 CustomAtlasFramePositionInPixels {
		get {
			return mCustomAtlasFramePositionInPixels;
		}
		set {
			if (value != mCustomAtlasFramePositionInPixels) {
				mUpdateCalculationNeeded = true;
			}
			mCustomAtlasFramePositionInPixels = value;
		}
	}
	public Vector2 CustomAtlasFrameSizeInPixels {
		get {
			return mCustomAtlasFrameSizeInPixels;
		}
		set {
			if (value != mCustomAtlasFrameSizeInPixels) {
				mUpdateCalculationNeeded = true;
			}
			mCustomAtlasFrameSizeInPixels = value;
		}
	}
	public float CustomAtlasFrameRotation {
		get {
			return mCustomAtlasFrameRotation;
		}
		set {
			if (value != mCustomAtlasFrameRotation) {
				mUpdateCalculationNeeded = true;
			}
			mCustomAtlasFrameRotation = value;
		}
	}
	
	public bool UpdateCalculationNeeded {
		get {
			return mUpdateCalculationNeeded;
		}
		set {
			mUpdateCalculationNeeded = value;
		}
	}

    public bool SimpleColliderParametersChanged {
        get {
            return mSimpleColliderParametersChanged;
        }
        set {
            mSimpleColliderParametersChanged = value;
        }
    }

    public bool HasThicknessChanged {
		get {
			return (mUpdatedThickness != mThickness);
		}
		set {
			if (value == false) {
				mUpdatedThickness = mThickness;
			}
		}
	}
}

//-------------------------------------------------------------------------
/// <summary>
/// Class to group region-specific parameters.
/// </summary>
[System.Serializable]
public class ColliderRegionParametersBase {
	[SerializeField] protected bool mEnableRegion = false;
	[SerializeField] protected int mMaxPointCount = 20;
	[SerializeField] protected bool mConvex = false;
	[SerializeField] protected bool mRegionUpdateCalculationNeeded = true;
	
	// Constructors
	//-------------------------------------------------------------------------
	// Default constructor.
	public ColliderRegionParametersBase() {
	}
	
	//-------------------------------------------------------------------------
	// Deep-copy constructor.
	public ColliderRegionParametersBase(ColliderRegionParametersBase src) {
		
		mEnableRegion = src.mEnableRegion;
		mMaxPointCount = src.mMaxPointCount;
		mConvex = src.mConvex;
		mRegionUpdateCalculationNeeded = src.mRegionUpdateCalculationNeeded;
	}

	// Setters and Getters
	public bool EnableRegion {
		get {
			return mEnableRegion;
		}
		set {
			if (value != mEnableRegion) {
				mRegionUpdateCalculationNeeded = true;
			}
			mEnableRegion = value;
		}
	}
	public int MaxPointCount {
		get {
			return mMaxPointCount;
		}
		set {
			if (value != mMaxPointCount) {
				mRegionUpdateCalculationNeeded = true;
			}
			mMaxPointCount = value;
		}
	}
	public bool Convex {
		get {
			return mConvex;
		}
		set {
			if (value != mConvex) {
				mRegionUpdateCalculationNeeded = true;
			}
			mConvex = value;
		}
	}
	public bool RegionUpdateCalculationNeeded {
		get {
			return mRegionUpdateCalculationNeeded;
		}
		set {
			mRegionUpdateCalculationNeeded = value;
		}
	}
}

//-------------------------------------------------------------------------
/// <summary>
/// Class to group result- and intermediate data of a collider.
/// </summary>
[System.Serializable]
public class GeneratedColliderData {
	public PolygonOutlineFromImageFrontend mOutlineAlgorithm;
	[System.NonSerialized] public bool[] mBinaryImage;
    public int mBinaryImageWidth;
    public int mBinaryImageHeight;
    [System.NonSerialized] public int[] mClassificationImage;
    public List<IslandDetector.Region> mIslandsWorkList;
    public List<IslandDetector.Region> mSeaRegionsWorkList;
    public ColliderRegionData[] mColliderRegions;

	public int NumIslandRegions {
		get {
			if (mColliderRegions == null)
				return 0;
			int numIslands = 0;
			for (int index = 0; index < mColliderRegions.Length; ++index) {
				if (mColliderRegions[index].mRegionIsIsland) {
					++numIslands;
				}
			}
			return numIslands;
		}
	}
	public int NumSeaRegions {
		get {
			if (mColliderRegions == null)
				return 0;
			int numSeaRegions = 0;
			for (int index = 0; index < mColliderRegions.Length; ++index) {
				if (!mColliderRegions[index].mRegionIsIsland) {
					++numSeaRegions;
				}
			}
			return numSeaRegions;
		}
	}
}

//-------------------------------------------------------------------------
/// <summary>
/// Class to group region-specific collider result- and intermediate data.
/// </summary>
[System.Serializable]
public class ColliderRegionData {
	
	public IslandDetector.Region mDetectedRegion;
	public bool mRegionIsIsland = true;
	public bool mOutlineVertexOrderIsCCW = true;
    public List<Vector2> mIntermediateOutlineVertices = null; // temporary result, stored to run the algorithm from a common intermediate point instead of from the start.
	public List<Vector2> mReducedOutlineVertices = null;
    public List<int> mUsedIndices = null;
    public Vector3[] mResultVertices = null;
    public int[] mResultTriangleIndices = null;
}
